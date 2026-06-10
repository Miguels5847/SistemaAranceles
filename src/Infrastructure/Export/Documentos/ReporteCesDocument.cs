using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaAranceles.Application.DTOs.Reportes;

namespace SistemaAranceles.Infrastructure.Export.Documentos;

internal sealed class ReporteCesDocument(ReporteCesDatos datos) : IDocument
{
    public DocumentMetadata GetMetadata() => new() { Title = "Reporte CES / INF CES" };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.DefaultTextStyle(s => s.FontSize(8.5f));
            page.Header().Element(h => EstilosPdf.EncabezadoReporte(
                h, "Reporte CES / INF CES", datos.CarreraNombre, datos.EscenarioNombre, datos.GeneradoEn));
            page.Footer().Element(EstilosPdf.PiePagina);
            page.Content().Column(col => SeccionesPdf.CuadrosCes(col, datos.Ces));
        });
    }
}
