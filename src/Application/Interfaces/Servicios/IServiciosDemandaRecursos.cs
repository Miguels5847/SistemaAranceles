namespace SistemaAranceles.Application.Interfaces.Servicios;

/// <summary>
/// Dependencias cross-modulo del modulo Recursos y Depreciacion (KAN-24).
/// Los modulos Estudiantes/Demanda/Sueldos aun no exponen estos totales por semestre;
/// se consumen via interfaz para no bloquear la epica (ver implementaciones stub).
/// </summary>
public interface IServicioEstudiantesTotales
{
    // KAN-25: consume la proyeccion real de estudiantes por carrera + escenario.
    Task<decimal> ObtenerTotalEstudiantesPorSemestreAsync(
        int carreraId,
        int escenarioProyeccionId,
        int anio,
        int numeroPeriodo,
        CancellationToken ct = default);
}

public interface IServicioDocentesTotales
{
    // KAN-25: consume el total real de docentes requeridos por carrera + escenario.
    Task<decimal> ObtenerTotalDocentesPorSemestreAsync(
        int carreraId,
        int escenarioProyeccionId,
        int anio,
        int numeroPeriodo,
        CancellationToken ct = default);

    // Devuelve los docentes requeridos por número de período relativo (1..N) en una sola
    // lectura, para evitar reconstruir el consolidado por cada celda/período.
    Task<IReadOnlyDictionary<int, decimal>> ObtenerTotalesDocentesPorPeriodoAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken ct = default);
}
