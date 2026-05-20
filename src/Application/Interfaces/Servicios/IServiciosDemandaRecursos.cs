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
}

public interface IServicioInflacionFactor
{
    // TODO(KAN-25): consumir factor acumulado real desde Sueldos/Configuracion.
    Task<decimal> ObtenerFactorAcumuladoAsync(
        int anio, int numeroPeriodo, CancellationToken ct = default);
}
