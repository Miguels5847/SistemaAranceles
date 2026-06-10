using SistemaAranceles.Application.DTOs.Reportes;

namespace SistemaAranceles.Application.Interfaces.Servicios;

/// <summary>
/// Genera los reportes PDF (KAN-46). Devuelve bytes: la capa de presentación
/// decide dónde guardar el archivo.
/// </summary>
public interface IServicioExportacionPdf
{
    byte[] GenerarReporteCes(ReporteCesDatos datos);
    byte[] GenerarReporteFinanciero(ReporteFinancieroDatos datos);
    byte[] GenerarReporteDemanda(ReporteDemandaDatos datos);
    byte[] GenerarReporteDireccion(ReporteDireccionDatos datos);
}
