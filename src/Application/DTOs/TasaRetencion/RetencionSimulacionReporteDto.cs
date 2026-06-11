namespace SistemaAranceles.Application.DTOs.TasaRetencion;

/// <summary>
/// Sección "Retención y simulación por ciclos" del reporte por dirección (KAN-49):
/// tasas y parámetros de la configuración del escenario + la misma tabla semestral
/// de estudiantes por ciclo que muestra la pantalla de Tasa de Retención.
/// </summary>
public sealed class RetencionSimulacionReporteDto
{
    public bool TieneDatos { get; init; }

    public decimal TasaRetencionPorcentaje { get; init; }
    public decimal TasaGraduacionPorcentaje { get; init; }
    public decimal? MetaRetencionPorcentaje { get; init; }
    public decimal? MetaGraduacionPorcentaje { get; init; }

    /// <summary>Tasa que realmente usan la proyección y los cálculos financieros (meta si existe, si no la histórica).</summary>
    public decimal TasaRetencionAplicada { get; init; }
    public decimal TasaGraduacionAplicada { get; init; }

    public int ParalelosPeriodo1 { get; init; }
    public int ParalelosPeriodo2 { get; init; }
    public decimal EstudiantesPeriodo1 { get; init; }
    public decimal EstudiantesPeriodo2 { get; init; }
    public int TotalCiclos { get; init; }

    /// <summary>Estudiantes del grupo abril-agosto al llegar a cada ciclo (1..TotalCiclos).</summary>
    public IReadOnlyList<decimal> AlumnosPeriodo1PorCiclo { get; init; } = [];

    /// <summary>Estudiantes del grupo septiembre-febrero por ciclo (el último ciclo no tiene segundo período).</summary>
    public IReadOnlyList<decimal> AlumnosPeriodo2PorCiclo { get; init; } = [];
}
