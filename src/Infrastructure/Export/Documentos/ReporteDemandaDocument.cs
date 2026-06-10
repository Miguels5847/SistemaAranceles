using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaAranceles.Application.DTOs.Reportes;

namespace SistemaAranceles.Infrastructure.Export.Documentos;

internal sealed class ReporteDemandaDocument(ReporteDemandaDatos datos) : IDocument
{
    public DocumentMetadata GetMetadata() => new() { Title = "Reporte de Demanda y Matrícula" };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(26);
            page.DefaultTextStyle(s => s.FontSize(8f));
            page.Header().Element(h => EstilosPdf.EncabezadoReporte(
                h, "Reporte de Demanda y Matrícula", datos.CarreraNombre, datos.EscenarioNombre, datos.GeneradoEn));
            page.Footer().Element(EstilosPdf.PiePagina);
            page.Content().Column(col =>
            {
                SeccionesPdf.Arancel(col, datos.ArancelEfectivo);
                SeccionesPdf.DemandaTabla(col, datos.Demanda);
                if (datos.Demanda.TieneDatos)
                    SeccionesPdf.GraficoMatricula(col, datos.Demanda);
                SeccionesPdf.DocentesTabla(col, datos.Demanda);
                if (datos.Demanda.TieneDocentes)
                    SeccionesPdf.GraficoDocentes(col, datos.Demanda);
                SeccionesPdf.Ingresos(col, datos.Ingresos);
            });
        });
    }
}
