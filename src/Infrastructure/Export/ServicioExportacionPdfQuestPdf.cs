using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SistemaAranceles.Application.DTOs.Reportes;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Infrastructure.Export.Documentos;

namespace SistemaAranceles.Infrastructure.Export;

public sealed class ServicioExportacionPdfQuestPdf : IServicioExportacionPdf
{
    static ServicioExportacionPdfQuestPdf()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GenerarReporteCes(ReporteCesDatos datos)
        => new ReporteCesDocument(datos).GeneratePdf();

    public byte[] GenerarReporteFinanciero(ReporteFinancieroDatos datos)
        => new ReporteFinancieroDocument(datos).GeneratePdf();

    public byte[] GenerarReporteDemanda(ReporteDemandaDatos datos)
        => new ReporteDemandaDocument(datos).GeneratePdf();

    public byte[] GenerarReporteDireccion(ReporteDireccionDatos datos)
        => new ReporteDireccionDocument(datos).GeneratePdf();
}
