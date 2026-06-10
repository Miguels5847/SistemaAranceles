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
            page.Content().Column(col =>
            {
                EstilosPdf.TituloSeccion(col, "INF CES — Presupuesto de la primera cohorte por función sustantiva");
                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3.2f);
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn();
                        c.RelativeColumn(1.2f);
                        c.RelativeColumn(0.8f);
                    });
                    tabla.Header(h =>
                    {
                        h.Cell().CeldaHeader().Text("Concepto").Bold();
                        h.Cell().CeldaHeader().AlignRight().Text("Provisión").Bold();
                        h.Cell().CeldaHeader().AlignRight().Text("Fomento").Bold();
                        h.Cell().CeldaHeader().AlignRight().Text("Vinculación").Bold();
                        h.Cell().CeldaHeader().AlignRight().Text("Otros").Bold();
                        h.Cell().CeldaHeader().AlignRight().Text("Total").Bold();
                        h.Cell().CeldaHeader().AlignRight().Text("%").Bold();
                    });
                    foreach (var fila in datos.Ces.InfCes)
                    {
                        var resaltada = fila.EsSeccion || fila.EsTotal || fila.EsResultado;
                        if (fila.EsSeccion)
                        {
                            tabla.Cell().ColumnSpan(7).CeldaSeccion().Text(fila.Concepto).Bold();
                            continue;
                        }
                        tabla.Cell().Celda().Text(t => { var s = t.Span(fila.Concepto); if (resaltada) s.Bold(); });
                        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.ProvisionDisplay); if (resaltada) s.Bold(); });
                        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.FomentoDisplay); if (resaltada) s.Bold(); });
                        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.VinculacionDisplay); if (resaltada) s.Bold(); });
                        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.OtrosDisplay); if (resaltada) s.Bold(); });
                        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.TotalDisplay); if (resaltada) s.Bold(); });
                        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.PorcentajeDisplay); if (resaltada) s.Bold(); });
                    }
                });

                col.Item().PaddingTop(8).Row(row =>
                {
                    row.RelativeItem().CeldaSeccion().Column(c =>
                    {
                        c.Item().Text("Arancel por semestre").FontSize(8).FontColor(EstilosPdf.ColorGris);
                        c.Item().Text(datos.Ces.ArancelPorSemestreDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
                    });
                    row.Spacing(6);
                    row.RelativeItem().CeldaSeccion().Column(c =>
                    {
                        c.Item().Text("Matrícula").FontSize(8).FontColor(EstilosPdf.ColorGris);
                        c.Item().Text(datos.Ces.MatriculaDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
                    });
                    row.RelativeItem().CeldaSeccion().Column(c =>
                    {
                        c.Item().Text("Total por semestre").FontSize(8).FontColor(EstilosPdf.ColorGris);
                        c.Item().Text(datos.Ces.TotalPorSemestreDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
                    });
                });

                EstilosPdf.TituloSeccion(col, "Parámetros de justificación del arancel");
                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2f);
                        c.RelativeColumn(3f);
                        c.RelativeColumn(1.2f);
                    });
                    tabla.Header(h =>
                    {
                        h.Cell().CeldaHeader().Text("Parámetro").Bold();
                        h.Cell().CeldaHeader().Text("Criterio").Bold();
                        h.Cell().CeldaHeader().AlignRight().Text("Valor").Bold();
                    });
                    foreach (var fila in datos.Ces.Parametros)
                    {
                        tabla.Cell().Celda().Text(t => { var s = t.Span(fila.Parametro); if (fila.EsResultado) s.Bold(); });
                        tabla.Cell().Celda().Text(t => { var s = t.Span(fila.Criterio); if (fila.EsResultado) s.Bold(); });
                        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.ValorDisplay); if (fila.EsResultado) s.Bold(); });
                    }
                });

                EstilosPdf.TituloSeccion(col, "Distribución referencial del costo de la carrera");
                col.Item().Table(tabla =>
                {
                    tabla.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3f);
                        c.RelativeColumn(1.4f);
                        c.RelativeColumn(1f);
                    });
                    tabla.Header(h =>
                    {
                        h.Cell().CeldaHeader().Text("Categoría").Bold();
                        h.Cell().CeldaHeader().AlignRight().Text("Monto").Bold();
                        h.Cell().CeldaHeader().AlignRight().Text("%").Bold();
                    });
                    foreach (var fila in datos.Ces.Distribucion)
                    {
                        var resaltada = fila.EsTotal || fila.EsReferencial;
                        tabla.Cell().Celda().Text(t => { var s = t.Span(fila.Categoria); if (resaltada) s.Bold(); });
                        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.MontoDisplay); if (resaltada) s.Bold(); });
                        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.PorcentajeDisplay); if (resaltada) s.Bold(); });
                    }
                });

                EstilosPdf.Advertencia(col, datos.Ces.MensajeAdvertencia);
            });
        });
    }
}
