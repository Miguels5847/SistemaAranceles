using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// KAN-34: Calcula materiales por periodo a partir de ratios + proyección estudiantes.
///   cantidad = estudiantes × ratio × (meses_operativos si unidad='por_estudiante_mes', sino 1)
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
        var anioBase = datos?.AnioBaseProyeccion;

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
            advertencias.Add("No hay proyección de estudiantes para la carrera/escenario.");
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

        var ratios = await repositorioRatios.ListarPorCarreraAsync(carreraId, incluirGlobales: true, ct);
        if (ratios.Count == 0)
        {
            advertencias.Add("No hay ratios configurados para esta carrera (ni globales).");
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

        // Factor inflación por año (acumulado desde anio_base hasta el año destino)
        var aniosDestino = estudiantesPorPeriodo.Values.Select(v => v.Anio).Distinct().ToList();
        var factoresPorAnio = await CalcularFactoresInflacionAsync(anioBase, aniosDestino, ct);

        var cantidades = new List<MaterialCantidadCeldaDto>();
        var monetarios = new List<MaterialMonetarioCeldaDto>();

        foreach (var ratio in ratios)
        {
            var multiplicadorUnidad = ratio.UnidadRatio == "por_estudiante_mes"
                ? ratio.MesesOperativos
                : 1;

            foreach (var (periodoId, info) in estudiantesPorPeriodo.OrderBy(p => p.Value.Anio).ThenBy(p => p.Value.NumeroPeriodo))
            {
                var cantidad = decimal.Round(info.Total * ratio.RatioConsumo * multiplicadorUnidad, 4);

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
                var factor = ratio.AplicaInflacion && factoresPorAnio.TryGetValue(info.Anio, out var f) ? f : 1m;
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

        if (anioBase is null)
            advertencias.Add("Año base de proyección no configurado en Datos Institucionales — factor de inflación usa 1.");

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

    private async Task<Dictionary<int, decimal>> CalcularFactoresInflacionAsync(
        int? anioBase,
        IReadOnlyList<int> aniosDestino,
        CancellationToken ct)
    {
        var resultado = new Dictionary<int, decimal>();
        if (anioBase is null || aniosDestino.Count == 0)
        {
            foreach (var a in aniosDestino) resultado[a] = 1m;
            return resultado;
        }

        var anioMaximo = aniosDestino.Max();
        var anioMinimo = Math.Min(anioBase.Value, aniosDestino.Min());
        var lista = await repositorioInflacion.ListarPorRangoAsync(anioMinimo, anioMaximo, ct);
        var porAnio = lista.ToDictionary(x => x.Anio, x => x.PorcentajeInflacion);

        foreach (var anioDestino in aniosDestino.Distinct())
        {
            var factor = 1m;
            if (anioDestino > anioBase.Value)
            {
                for (var a = anioBase.Value + 1; a <= anioDestino; a++)
                {
                    if (porAnio.TryGetValue(a, out var inf))
                        factor *= 1m + inf / 100m;
                }
            }
            resultado[anioDestino] = decimal.Round(factor, 6);
        }
        return resultado;
    }

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
