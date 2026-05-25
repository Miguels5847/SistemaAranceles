using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// KAN-35: Prorratea los presupuestos institucionales por carrera/escenario.
/// Proporción = (estudiantes promedio carrera) / (estudiantes universidad).
/// Por semestre = anual / semestres_por_anio.
/// </summary>
public sealed class ObtenerPresupuestosCarreraQuery(
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioProyeccionEstudiantes repositorioProyeccion)
{
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
        var estudiantesCarrera = await EstimarEstudiantesPromedioCarreraAsync(
            carreraId, escenarioProyeccionId, ct);

        var proporcion = estudiantesUniv > 0 && estudiantesCarrera > 0m
            ? estudiantesCarrera / estudiantesUniv
            : 0m;

        var presupuestos = new List<PresupuestoDemandaDto>
        {
            Construir("Capacitación", datos.PresupuestoAnualCapacitacion, proporcion, datos.SemestresPorAnio),
            Construir("Internacionalización", datos.PresupuestoAnualInternacionalizacion, proporcion, datos.SemestresPorAnio),
            Construir("Marketing", datos.PresupuestoAnualMarketing, proporcion, datos.SemestresPorAnio),
            Construir("Seguro Estudiantil", datos.PolizaSeguroEstudiantilAnual, proporcion, datos.SemestresPorAnio),
        };

        var advertencias = new List<string>();
        if (estudiantesUniv <= 0)
            advertencias.Add("Datos Institucionales: número de estudiantes universidad debe ser > 0.");
        if (estudiantesCarrera <= 0m)
            advertencias.Add("No hay proyección de estudiantes para la carrera/escenario. Prorrateo en 0.");

        return new PresupuestosCarreraDto
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EstudiantesUniversidad = estudiantesUniv,
            EstudiantesCarreraPromedio = decimal.Round(estudiantesCarrera, 2),
            SemestresPorAnio = datos.SemestresPorAnio,
            Presupuestos = presupuestos,
            MensajeAdvertencia = advertencias.Count > 0 ? string.Join(" ", advertencias) : null
        };
    }

    private async Task<decimal> EstimarEstudiantesPromedioCarreraAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct)
    {
        if (escenarioProyeccionId is null or <= 0)
            return 0m;

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId, escenarioProyeccionId.Value, ct);
        if (proyeccionId is null or <= 0)
            return 0m;

        var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return 0m;

        return proyeccion.Detalles.Average(d => d.TotalEstudiantes);
    }

    private static PresupuestoDemandaDto Construir(
        string tipo, decimal montoAnualInstitucional, decimal proporcion, int semestres)
    {
        var prorrateado = decimal.Round(montoAnualInstitucional * proporcion, 2);
        var porSemestre = semestres > 0 ? decimal.Round(prorrateado / semestres, 2) : 0m;
        return new PresupuestoDemandaDto
        {
            TipoPresupuesto = tipo,
            MontoAnualInstitucional = montoAnualInstitucional,
            MontoAnualProrrateado = prorrateado,
            MontoPorSemestre = porSemestre,
            ProporcionEstudiantes = proporcion
        };
    }
}
