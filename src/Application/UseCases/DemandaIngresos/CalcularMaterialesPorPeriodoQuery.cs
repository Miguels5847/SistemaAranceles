using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// KAN-34: Calcula materiales por periodo a partir de ratios + proyección estudiantes.
///   cantidad = estudiantes x ratio, estudiantes x ratio x meses, o ratio fijo por periodo.
///   precio   = item_material_insumo.precio_unitario (si vinculado) o 0
///   factor_inflacion = (1+inf₁)·(1+inf₂)... desde anio_base hasta año del periodo (si aplica_inflacion)
///   costo    = cantidad × precio × factor
/// </summary>
public sealed class CalcularMaterialesPorPeriodoQuery(
    IRepositorioRatioMaterialDemanda repositorioRatios,
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioInflacionAnual repositorioInflacion)
{
    public async Task<MaterialesProyectadosDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        int? anioBase = null;

        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
        {
            advertencias.Add("Selecciona un escenario.");
            return Vacio(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId,
                escenario?.Nombre ?? "Global", anioBase, advertencias);
        }

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId, escenarioProyeccionId.Value, ct);
        if (proyeccionId is null or <= 0)
        {
            advertencias.Add("No hay proyeccion de estudiantes para esta carrera/escenario.");
            return Vacio(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId,
                escenario?.Nombre ?? "Global", anioBase, advertencias);
        }

        var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
        {
            advertencias.Add("Proyección sin detalles.");
            return Vacio(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId,
                escenario?.Nombre ?? "Global", anioBase, advertencias);
        }

        // KAN-34 (revisión): el año base se infiere del primer período de la proyección,
        // NO se edita manualmente en Datos Institucionales.
        anioBase = proyeccion.Detalles.Min(d => d.Anio);
        _ = datos; // se mantiene la lectura por compatibilidad pero ya no se usa AnioBaseProyeccion

        var ratios = await repositorioRatios.ListarPorCarreraAsync(carreraId, incluirGlobales: true, ct);
        if (ratios.Count == 0)
        {
            advertencias.Add("No hay ratios configurados.");
            return Vacio(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId,
                escenario?.Nombre ?? "Global", anioBase, advertencias);
        }

        // Estudiantes promedio por periodo (sumar todos los ciclos)
        var estudiantesPorPeriodo = proyeccion.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .ToDictionary(g => g.Key, g => new
            {
                Total = g.Sum(d => d.TotalEstudiantes),
                g.First().Anio,
                g.First().NumeroPeriodo,
                g.First().EtiquetaPeriodo
            });

        // Factor de inflacion por periodo, inferido desde el primer periodo de la proyeccion.
        var periodosInflacion = estudiantesPorPeriodo.Values
            .Select(v => (v.Anio, v.NumeroPeriodo))
            .Distinct()
            .OrderBy(p => p.Anio)
            .ThenBy(p => p.NumeroPeriodo)
            .ToList();
        var (factoresPorPeriodo, aniosSinInflacion) = await CalcularFactoresInflacionPorPeriodoAsync(
            anioBase,
            periodosInflacion,
            ct);

        var cantidades = new List<MaterialCantidadCeldaDto>();
        var monetarios = new List<MaterialMonetarioCeldaDto>();

        foreach (var ratio in ratios)
        {
            foreach (var (periodoId, info) in estudiantesPorPeriodo.OrderBy(p => p.Value.Anio).ThenBy(p => p.Value.NumeroPeriodo))
            {
                var cantidad = ratio.UnidadRatio switch
                {
                    UnidadRatioMaterialExtensiones.PorEstudianteMesText =>
                        info.Total * ratio.RatioConsumo * ratio.MesesOperativos,
                    UnidadRatioMaterialExtensiones.FijoPeriodoText =>
                        ratio.RatioConsumo,
                    _ =>
                        info.Total * ratio.RatioConsumo
                };
                cantidad = decimal.Round(cantidad, 4);

                cantidades.Add(new MaterialCantidadCeldaDto
                {
                    RatioId = ratio.Id,
                    Categoria = ratio.Categoria,
                    Concepto = ratio.Concepto,
                    PeriodoAcademicoId = periodoId,
                    Anio = info.Anio,
                    NumeroPeriodo = info.NumeroPeriodo,
                    EtiquetaPeriodo = info.EtiquetaPeriodo,
                    Estudiantes = info.Total,
                    Cantidad = cantidad
                });

                var precio = ratio.PrecioUnitarioReferencia;
                var factor = ratio.AplicaInflacion && factoresPorPeriodo.TryGetValue((info.Anio, info.NumeroPeriodo), out var f) ? f : 1m;
                var costo = decimal.Round(cantidad * precio * factor, 2);

                monetarios.Add(new MaterialMonetarioCeldaDto
                {
                    RatioId = ratio.Id,
                    Categoria = ratio.Categoria,
                    Concepto = ratio.Concepto,
                    PeriodoAcademicoId = periodoId,
                    Anio = info.Anio,
                    NumeroPeriodo = info.NumeroPeriodo,
                    EtiquetaPeriodo = info.EtiquetaPeriodo,
                    Cantidad = cantidad,
                    PrecioUnitario = precio,
                    FactorInflacion = factor,
                    Costo = costo
                });
            }
        }

        if (ratios.Any(r => r.ItemMaterialInsumoId is null))
            advertencias.Add("Hay ratios sin item de Capital de Trabajo vinculado; Materiales Monetarios mostrara costo 0.");
        if (ratios.Any(r => r.ItemMaterialInsumoId is not null && r.PrecioUnitarioReferencia <= 0m))
            advertencias.Add("Hay ratios con item vinculado sin precio unitario; Materiales Monetarios mostrara costo 0.");
        if (aniosSinInflacion.Count > 0)
            advertencias.Add($"No hay inflación registrada para el año {string.Join(", ", aniosSinInflacion)}; se usó factor 1.");

        return new MaterialesProyectadosDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? string.Empty,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? "Global",
            AnioBaseInflacion = anioBase,
            Cantidades = cantidades,
            Monetarios = monetarios,
            MensajeAdvertencia = advertencias.Count > 0 ? string.Join(" ", advertencias) : null
        };
    }

    private async Task<(Dictionary<(int Anio, int NumeroPeriodo), decimal> Factores, IReadOnlyList<int> AniosSinInflacion)> CalcularFactoresInflacionPorPeriodoAsync(
        int? anioBase,
        IReadOnlyList<(int Anio, int NumeroPeriodo)> periodos,
        CancellationToken ct)
    {
        var resultado = new Dictionary<(int Anio, int NumeroPeriodo), decimal>();
        if (anioBase is null || periodos.Count == 0)
        {
            foreach (var periodo in periodos) resultado[periodo] = 1m;
            return (resultado, []);
        }

        var anioMaximo = periodos.Max(p => p.Anio);
        var lista = await repositorioInflacion.ListarPorRangoAsync(anioBase.Value, anioMaximo, ct);
        var aniosConInflacion = lista.Select(x => x.Anio).ToHashSet();

        var aniosSinInflacion = ObtenerAniosInflacionNecesarios(periodos, anioBase.Value)
            .Where(anio => !aniosConInflacion.Contains(anio))
            .ToList();

        foreach (var periodo in periodos.Distinct())
        {
            resultado[periodo] = CalculoInflacionAplicada.CalcularFactorPeriodo(
                lista,
                anioBase.Value,
                periodo.Anio,
                NumeroPeriodoEnAnio(periodo.NumeroPeriodo));
        }

        return (resultado, aniosSinInflacion);
    }

    private static IReadOnlyList<int> ObtenerAniosInflacionNecesarios(
        IReadOnlyList<(int Anio, int NumeroPeriodo)> periodos,
        int anioBase)
    {
        var resultado = new SortedSet<int>();
        foreach (var periodo in periodos)
        {
            var numeroPeriodoEnAnio = NumeroPeriodoEnAnio(periodo.NumeroPeriodo);
            for (var anio = anioBase; anio <= periodo.Anio; anio++)
            {
                if (anio < periodo.Anio || numeroPeriodoEnAnio >= 2)
                    resultado.Add(anio);
            }
        }

        return resultado.ToList();
    }

    private static int NumeroPeriodoEnAnio(int numeroPeriodo)
        => numeroPeriodo <= 0 ? 1 : ((numeroPeriodo - 1) % 2) + 1;

    private static MaterialesProyectadosDto Vacio(
        int carreraId, string carreraNombre,
        int? escenarioId, string escenarioNombre,
        int? anioBase,
        List<string> advertencias) => new()
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EscenarioProyeccionId = escenarioId,
            EscenarioNombre = escenarioNombre,
            AnioBaseInflacion = anioBase,
            MensajeAdvertencia = advertencias.Count > 0 ? string.Join(" ", advertencias) : null
        };
}
