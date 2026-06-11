namespace SistemaAranceles.Presentation.Mensajes;

/// <summary>
/// Atajo 4 → 5 del flujo (KAN-49): desde Análisis Financiero se salta al módulo Reportes
/// con la carrera y el escenario ya preseleccionados, listos para exportar.
/// </summary>
public sealed record AbrirReportesMensaje(int CarreraId, int EscenarioId);
