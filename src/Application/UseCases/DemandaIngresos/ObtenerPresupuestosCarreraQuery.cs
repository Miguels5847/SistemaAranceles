using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Inflacion;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// KAN-35: Calcula el resumen de presupuestos institucionales asignados a la carrera.
/// Proporcion = (estudiantes promedio carrera) / (estudiantes universidad).
/// Por semestre = anual / semestres_por_anio.
/// </summary>
public sealed class ObtenerPresupuestosCarreraQuery(
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioInflacionAnual repositorioInflacion)
{
    private sealed record PeriodoPresupuestoInfo(int Anio, int NumeroPeriodo);

    public async Task<PresupuestosCarreraDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var carreraNombre = carrera?.Nombre ?? string.Empty;

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        if (datos is null)
        {
            return new PresupuestosCarreraDto
            {
                CarreraId = carreraId,
                CarreraNombre = carreraNombre,
                SemestresPorAnio = 2,
                MensajeAdvertencia = "No hay Datos Institucionales vigentes."
            };
        }

        var estudiantesUniv = datos.NumeroEstudiantesUniversidad;
        var (estudiantesCarrera, periodos) = await ObtenerInfoProyeccionAsync(
            carreraId, escenarioProyeccionId, ct);

        var proporcion = estudiantesUniv > 0 && estudiantesCarrera > 0m
            ? estudiantesCarrera / estudiantesUniv
            : 0m;

        var advertencias = new List<string>();
        var factoresInflacion = await CalcularFactoresInflacionAsync(
            periodos,
            datos.SemestresPorAnio,
            advertencias,
            ct);

        var presupuestos = new List<PresupuestoDemandaDto>
        {
            Construir("Capacitación", datos.PresupuestoAnualCapacitacion, proporcion, datos.SemestresPorAnio),
            Construir("Internacionalización", datos.PresupuestoAnualInternacionalizacion, proporcion, datos.SemestresPorAnio),
            Construir("Marketing", datos.PresupuestoAnualMarketing, proporcion, datos.SemestresPorAnio),
            Construir("Seguro Estudiantil", datos.PolizaSeguroEstudiantilAnual, proporcion, datos.SemestresPorAnio),
        };

        if (estudiantesUniv <= 0)
            advertencias.Add("Datos Institucionales: numero de estudiantes universidad debe ser > 0.");
        if (datos.NumeroDocentesUniversidad <= 0)
            advertencias.Add("Datos Institucionales: numero de docentes universidad debe ser > 0.");
        if (estudiantesCarrera <= 0m)
            advertencias.Add("No hay proyeccion de estudiantes para esta carrera/escenario. Genere la proyeccion para calcular la asignacion a la carrera.");

        return new PresupuestosCarreraDto
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EstudiantesUniversidad = estudiantesUniv,
            DocentesUniversidad = datos.NumeroDocentesUniversidad,
            EstudiantesCarreraPromedio = decimal.Round(estudiantesCarrera, 2),
            SemestresPorAnio = datos.SemestresPorAnio,
            PorcentajeBecasInstitucionales = datos.PorcentajeBecasInstitucionales,
            PresupuestoAnualCapacitacion = datos.PresupuestoAnualCapacitacion,
            PresupuestoAnualInternacionalizacion = datos.PresupuestoAnualInternacionalizacion,
            PresupuestoAnualMarketing = datos.PresupuestoAnualMarketing,
            PolizaSeguroEstudiantilAnual = datos.PolizaSeguroEstudiantilAnual,
            AnioBaseInflacion = periodos.Count > 0 ? periodos.Min(p => p.Anio) : null,
            FactoresInflacionPeriodos = factoresInflacion,
            Presupuestos = presupuestos,
            MensajeAdvertencia = advertencias.Count > 0 ? string.Join(" ", advertencias) : null
        };
    }

    private async Task<(decimal EstudiantesPromedio, IReadOnlyList<PeriodoPresupuestoInfo> Periodos)> ObtenerInfoProyeccionAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct)
    {
        if (escenarioProyeccionId is null or <= 0)
            return (0m, []);

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId, escenarioProyeccionId.Value, ct);
        if (proyeccionId is null or <= 0)
            return (0m, []);

        var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return (0m, []);

        var promedio = proyeccion.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .Select(g => g.Sum(d => d.TotalEstudiantes))
            .Average();

        var periodos = proyeccion.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .Select(g =>
            {
                var primero = g.First();
                return new PeriodoPresupuestoInfo(primero.Anio, primero.NumeroPeriodo);
            })
            .OrderBy(p => p.Anio)
            .ThenBy(p => p.NumeroPeriodo)
            .ToList();

        return (promedio, periodos);
    }

    private async Task<IReadOnlyList<decimal>> CalcularFactoresInflacionAsync(
        IReadOnlyList<PeriodoPresupuestoInfo> periodos,
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
        var aniosNecesarios = ObtenerAniosInflacionNecesarios(periodos, anioBase, semestresPorAnio);
        var aniosSinInflacion = aniosNecesarios
            .Where(anio => !aniosConInflacion.Contains(anio))
            .Distinct()
            .OrderBy(anio => anio)
            .ToList();

        if (aniosSinInflacion.Count > 0)
            advertencias.Add($"No hay inflación registrada para el año {string.Join(", ", aniosSinInflacion)}; se usó factor 1.");

        return periodos
            .Select(p => CalculoInflacionAplicada.CalcularFactorPeriodo(
                registros,
                anioBase,
                p.Anio,
                NumeroPeriodoEnAnio(p.NumeroPeriodo, semestresPorAnio)))
            .ToList();
    }

    private static IReadOnlyList<int> ObtenerAniosInflacionNecesarios(
        IReadOnlyList<PeriodoPresupuestoInfo> periodos,
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

    private static PresupuestoDemandaDto Construir(
        string tipo, decimal montoAnualInstitucional, decimal proporcion, int semestres)
    {
        var asignado = decimal.Round(montoAnualInstitucional * proporcion, 2);
        var porSemestre = semestres > 0 ? decimal.Round(asignado / semestres, 2) : 0m;
        return new PresupuestoDemandaDto
        {
            TipoPresupuesto = tipo,
            MontoAnualInstitucional = montoAnualInstitucional,
            MontoAnualProrrateado = asignado,
            MontoPorSemestre = porSemestre,
            ProporcionEstudiantes = proporcion
        };
    }
}
