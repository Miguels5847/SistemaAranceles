using SistemaAranceles.Application.DTOs.Reportes;

namespace SistemaAranceles.Application.Interfaces.Servicios;

/// <summary>
/// Genera el reporte por dirección en formato XLSX (KAN-48): una hoja por sección,
/// espejo del Excel original. Devuelve bytes; Presentation decide dónde guardar.
/// </summary>
public interface IServicioExportacionXlsx
{
    byte[] GenerarReporteDireccion(ReporteDireccionDatos datos);
}
