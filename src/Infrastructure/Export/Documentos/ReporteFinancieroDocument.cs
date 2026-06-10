using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaAranceles.Application.DTOs.Reportes;

namespace SistemaAranceles.Infrastructure.Export.Documentos;

internal sealed class ReporteFinancieroDocument(ReporteFinancieroDatos datos) : IDocument
{
    public DocumentMetadata GetMetadata() => new() { Title = "Reporte Financiero" };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(26);
            page.DefaultTextStyle(s => s.FontSize(8f));
            page.Header().Element(h => EstilosPdf.EncabezadoReporte(
                h, "Reporte Financiero", datos.CarreraNombre, datos.EscenarioNombre, datos.GeneradoEn));
            page.Footer().Element(EstilosPdf.PiePagina);
            page.Content().Column(col =>
            {
                SeccionesPdf.InversionYCapital(col, datos.Inversion, datos.CapitalTrabajo);
                SeccionesPdf.Financiamiento(col, datos.Financiamiento);
                SeccionesPdf.Indicadores(col, datos.Indicadores);
                SeccionesPdf.Matriz(col, "Proyección de Costos y Gastos",
                    datos.CostosGastos.EtiquetasPeriodos,
                    datos.CostosGastos.ProyeccionCostosGastos.Select(f => (
                        f.Concepto,
                        f.PeriodosDisplay,
                        f.TotalDisplay,
                        f.EsEncabezadoGrupo,
                        f.EsTotal)).ToList(),
                    datos.CostosGastos.MensajeAdvertencia);
                SeccionesPdf.Matriz(col, "Flujo de Fondos",
                    datos.FlujoFondos.EtiquetasPeriodos,
                    datos.FlujoFondos.Filas.Select(f => (
                        f.Concepto,
                        f.PeriodosDisplay,
                        f.TotalDisplay,
                        f.EsSeccion,
                        f.EsTotal || f.EsResultado)).ToList(),
                    datos.FlujoFondos.MensajeAdvertencia);
            });
        });
    }
}
