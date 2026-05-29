using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CostosGastos;

public sealed class ObtenerMatrizInvVinBecasQuery(
    ObtenerDemandaProyectadaQuery obtenerDemandaProyectadaQuery,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioInflacionAnual repositorioInflacion)
{
    public async Task<MatrizInvVinBecasDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;
        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
            return Vacia(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId, escenario?.Nombre ?? string.Empty, "Selecciona un escenario para calcular Inv. Vin. Becas.");

        var demanda = await obtenerDemandaProyectadaQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertenciaDocentes);

        if (!demanda.TieneDatos || demanda.EtiquetasPeriodos.Count == 0)
            return Vacia(carreraId, carrera?.Nombre ?? demanda.CarreraNombre, escenarioProyeccionId, demanda.EscenarioNombre, ConstruirMensaje(advertencias, "No hay demanda proyectada para consolidar Inv. Vin. Becas."));

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        if (datos is null)
        {
            advertencias.Add("No hay Datos Institucionales vigentes. Se usaron parametros de Costos y Gastos en cero/default.");
            datos = CrearDatosInstitucionalesFallback();
        }

        var periodos = ConstruirPeriodos(demanda);
        var factores = await CalcularFactoresInflacionAsync(periodos, datos.SemestresPorAnio, advertencias, ct);
        var estudiantes = CompletarValores(demanda.TotalesPorPeriodo, periodos.Count);
        var docentes = ObtenerDocentesRequeridosPorPeriodo(demanda, periodos.Count);

        if (datos.NumeroEstudiantesUniversidad <= 0)
            advertencias.Add("Datos Institucionales: numero de estudiantes universidad debe ser > 0.");
        if (datos.NumeroDocentesUniversidad <= 0)
            advertencias.Add("Datos Institucionales: numero de docentes universidad debe ser > 0.");
        if (datos.PresupuestoBaseUniversidad <= 0m)
            advertencias.Add("Presupuesto base universidad esta en 0; Investigacion y Vinculacion quedan en 0.");
        if (datos.PresupuestoGobiernoBecas <= 0m)
            advertencias.Add("Presupuesto gobierno becas esta en 0; Becas Gobierno quedan en 0.");

        var valores = new List<InvVinBecasPeriodoDto>();
        for (var i = 0; i < periodos.Count; i++)
        {
            var presupuestoUniversidad = decimal.Round(datos.PresupuestoBaseUniversidad * factores[i], 2);
            var investigacion = datos.NumeroEstudiantesUniversidad > 0
                ? decimal.Round(presupuestoUniversidad * datos.PorcentajeInvestigacion / 100m / datos.NumeroEstudiantesUniversidad * estudiantes[i], 2)
                : 0m;
            var vinculacion = datos.NumeroEstudiantesUniversidad > 0
                ? decimal.Round(presupuestoUniversidad * datos.PorcentajeVinculacion / 100m / datos.NumeroEstudiantesUniversidad * estudiantes[i], 2)
                : 0m;
            var becasEstudiantes = datos.NumeroEstudiantesUniversidad > 0 && datos.PresupuestoGobiernoBecas > 0m
                ? decimal.Round(datos.PresupuestoGobiernoBecas * datos.PorcentajeBecasEstudiantes / 100m / datos.NumeroEstudiantesUniversidad * estudiantes[i], 2)
                : 0m;
            var becasDocentes = datos.NumeroDocentesUniversidad > 0 && datos.PresupuestoGobiernoBecas > 0m
                ? decimal.Round(datos.PresupuestoGobiernoBecas * datos.PorcentajeBecasDocentes / 100m / datos.NumeroDocentesUniversidad * docentes[i], 2)
                : 0m;

            valores.Add(new InvVinBecasPeriodoDto
            {
                PeriodoAcademicoId = periodos[i].PeriodoAcademicoId,
                Anio = periodos[i].Anio,
                NumeroPeriodo = periodos[i].NumeroPeriodo,
                EtiquetaPeriodo = periodos[i].Etiqueta,
                EstudiantesCarrera = estudiantes[i],
                DocentesCarrera = docentes[i],
                FactorInflacion = factores[i],
                PresupuestoUniversidad = presupuestoUniversidad,
                Investigacion = investigacion,
                Vinculacion = vinculacion,
                BecasEstudiantes = becasEstudiantes,
                BecasDocentes = becasDocentes
            });
        }

        return new MatrizInvVinBecasDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? demanda.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? demanda.EscenarioNombre,
            Periodos = periodos,
            ValoresPorPeriodo = valores,
            Filas = ConstruirFilas(valores),
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

    private async Task<IReadOnlyList<decimal>> CalcularFactoresInflacionAsync(
        IReadOnlyList<PeriodoCostoGastoDto> periodos,
        int semestresPorAnio,
        List<string> advertencias,
        CancellationToken ct)
    {
        if (periodos.Count == 0)
            return [];

        var anioBase = periodos.Min(p => p.Anio);
        var anioMaximo = periodos.Max(p => p.Anio);
        var registros = await repositorioInflacion.ListarPorRangoAsync(anioBase, anioMaximo, ct);
        var aniosConInflacion = registros.Select(r => r.Anio).ToHashSet();
        var aniosSinInflacion = ObtenerAniosInflacionNecesarios(periodos, anioBase, semestresPorAnio)
            .Where(anio => !aniosConInflacion.Contains(anio))
            .Distinct()
            .OrderBy(anio => anio)
            .ToList();

        if (aniosSinInflacion.Count > 0)
            advertencias.Add($"No hay inflacion registrada para el anio {string.Join(", ", aniosSinInflacion)}; se uso factor 1.");

        return periodos
            .Select(p => CalculoInflacionAplicada.CalcularFactorPeriodo(
                registros,
                anioBase,
                p.Anio,
                NumeroPeriodoEnAnio(p.NumeroPeriodo, semestresPorAnio)))
            .ToList();
    }

    public static IReadOnlyList<PeriodoCostoGastoDto> ConstruirPeriodos(DemandaProyectadaDto demanda)
    {
        var total = demanda.EtiquetasPeriodos.Count;
        var periodos = new List<PeriodoCostoGastoDto>(total);
        for (var i = 0; i < total; i++)
        {
            periodos.Add(new PeriodoCostoGastoDto
            {
                PeriodoAcademicoId = i < demanda.PeriodoAcademicoIds.Count ? demanda.PeriodoAcademicoIds[i] : 0,
                Etiqueta = demanda.EtiquetasPeriodos[i],
                Anio = i < demanda.AniosPeriodos.Count ? demanda.AniosPeriodos[i] : 0,
                NumeroPeriodo = i < demanda.NumerosPeriodos.Count ? demanda.NumerosPeriodos[i] : i + 1
            });
        }

        return periodos;
    }

    public static IReadOnlyList<decimal> ObtenerDocentesRequeridosPorPeriodo(DemandaProyectadaDto demanda, int cantidadPeriodos)
    {
        var fila = demanda.DocentesPorPeriodo.FirstOrDefault(f =>
            string.Equals(f.Tipo, "Docentes Requeridos", StringComparison.OrdinalIgnoreCase));

        return CompletarValores(fila?.Periodos ?? [], cantidadPeriodos);
    }

    public static IReadOnlyList<decimal> CompletarValores(IReadOnlyList<decimal> valoresOrigen, int cantidadPeriodos)
    {
        var valores = new List<decimal>(cantidadPeriodos);
        for (var i = 0; i < cantidadPeriodos; i++)
            valores.Add(i < valoresOrigen.Count ? valoresOrigen[i] : 0m);

        return valores;
    }

    private static IReadOnlyList<CostoGastoRubroDto> ConstruirFilas(IReadOnlyList<InvVinBecasPeriodoDto> valores)
    {
        return
        [
            CrearFila("Inv. Vin. Becas", "N. estudiantes carrera", valores.Select(v => v.EstudiantesCarrera).ToList(), FormatoMatrizCostosGastos.Entero),
            CrearFila("Inv. Vin. Becas", "N. docentes carrera", valores.Select(v => v.DocentesCarrera).ToList(), FormatoMatrizCostosGastos.Decimal),
            CrearFila("Inv. Vin. Becas", "Presupuesto universidad", valores.Select(v => v.PresupuestoUniversidad).ToList()),
            CrearFila("Inv. Vin. Becas", "Investigacion 5%", valores.Select(v => v.Investigacion).ToList()),
            CrearFila("Inv. Vin. Becas", "Vinculacion 1%", valores.Select(v => v.Vinculacion).ToList()),
            CrearFila("Inv. Vin. Becas", "Becas estudiantes 90%", valores.Select(v => v.BecasEstudiantes).ToList()),
            CrearFila("Inv. Vin. Becas", "Becas docentes 10%", valores.Select(v => v.BecasDocentes).ToList()),
            CrearFila("Inv. Vin. Becas", "Total Inv. Vin. Becas", valores.Select(v => v.TotalInvVinBecas).ToList(), esTotal: true)
        ];
    }

    private static CostoGastoRubroDto CrearFila(
        string grupo,
        string concepto,
        IReadOnlyList<decimal> periodos,
        string formato = FormatoMatrizCostosGastos.Moneda,
        bool esTotal = false)
        => new()
        {
            Grupo = grupo,
            Concepto = concepto,
            Periodos = periodos.Select(v => decimal.Round(v, 2)).ToList(),
            Total = decimal.Round(periodos.Sum(), 2),
            FormatoValor = formato,
            EsTotal = esTotal
        };

    private static IReadOnlyList<int> ObtenerAniosInflacionNecesarios(
        IReadOnlyList<PeriodoCostoGastoDto> periodos,
        int anioBase,
        int semestresPorAnio)
    {
        var resultado = new SortedSet<int>();
        foreach (var periodo in periodos)
        {
            var numeroPeriodoEnAnio = NumeroPeriodoEnAnio(periodo.NumeroPeriodo, semestresPorAnio);
            for (var anio = anioBase; anio <= periodo.Anio; anio++)
            {
                if (anio < periodo.Anio || numeroPeriodoEnAnio >= 2)
                    resultado.Add(anio);
            }
        }

        return resultado.ToList();
    }

    private static int NumeroPeriodoEnAnio(int numeroPeriodo, int semestresPorAnio)
    {
        var periodosPorAnio = semestresPorAnio <= 0 ? 2 : semestresPorAnio;
        return numeroPeriodo <= 0 ? 1 : ((numeroPeriodo - 1) % periodosPorAnio) + 1;
    }

    private static DatosInstitucionales CrearDatosInstitucionalesFallback()
    {
        var datos = new DatosInstitucionales(
            DateTime.Now.Year.ToString(),
            1,
            1,
            0,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            1,
            null);
        datos.CambiarParametrosCostosGastos(
            DatosInstitucionales.PresupuestoBaseUniversidadPorDefecto,
            DatosInstitucionales.PresupuestoGobiernoBecasPorDefecto,
            DatosInstitucionales.PorcentajeInvestigacionPorDefecto,
            DatosInstitucionales.PorcentajeVinculacionPorDefecto,
            DatosInstitucionales.PorcentajeBecasEstudiantesPorDefecto,
            DatosInstitucionales.PorcentajeBecasDocentesPorDefecto);
        return datos;
    }

    private static void AgregarAdvertencia(List<string> advertencias, string? mensaje)
    {
        if (!string.IsNullOrWhiteSpace(mensaje))
            advertencias.Add(mensaje.Trim());
    }

    private static string? ConstruirMensaje(List<string> advertencias, string? adicional = null)
    {
        if (!string.IsNullOrWhiteSpace(adicional))
            advertencias.Add(adicional);

        return advertencias.Count == 0
            ? null
            : string.Join(" ", advertencias.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static MatrizInvVinBecasDto Vacia(
        int carreraId,
        string carreraNombre,
        int? escenarioId,
        string escenarioNombre,
        string? mensaje)
        => new()
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EscenarioProyeccionId = escenarioId,
            EscenarioNombre = escenarioNombre,
            MensajeAdvertencia = mensaje
        };
}
