using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaAranceles.Application.DTOs.Reportes;

namespace SistemaAranceles.Infrastructure.Export.Documentos;

internal sealed class ReporteDemandaDocument(ReporteDemandaDatos datos) : IDocument
{
    public DocumentMetadata GetMetadata() => new() { Title = "Reporte de Demanda" };

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
                ComponerArancel(col);
                ComponerDemanda(col);
                ComponerIngresos(col);
            });
        });
    }

    private void ComponerArancel(ColumnDescriptor col)
    {
        EstilosPdf.TituloSeccion(col, "Arancel y Matrícula vigentes");
        col.Item().Row(row =>
        {
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Arancel efectivo").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.ArancelEfectivo.ArancelDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Matrícula efectiva").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.ArancelEfectivo.MatriculaDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem(2).CeldaSeccion().Column(c =>
            {
                c.Item().Text("Fuente del arancel").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.ArancelEfectivo.FuenteCalculo).Bold();
            });
        });
        EstilosPdf.Advertencia(col, datos.ArancelEfectivo.MensajeAdvertencia);
    }

    private void ComponerDemanda(ColumnDescriptor col)
    {
        EstilosPdf.TituloSeccion(col, "Demanda proyectada por ciclo (estudiantes)");
        var etiquetas = datos.Demanda.EtiquetasPeriodos;
        if (!datos.Demanda.TieneDatos)
        {
            col.Item().Text("Sin datos de demanda proyectada.").FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            EstilosPdf.Advertencia(col, datos.Demanda.MensajeAdvertencia);
            return;
        }

        col.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.6f);
                foreach (var _ in etiquetas)
                    c.RelativeColumn();
                c.RelativeColumn(1.1f);
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Ciclo").Bold();
                foreach (var etiqueta in etiquetas)
                    h.Cell().CeldaHeader().AlignRight().Text(etiqueta).Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Total").Bold();
            });
            foreach (var fila in datos.Demanda.Filas)
            {
                tabla.Cell().Celda().Text(fila.CicloDisplay);
                for (var i = 0; i < etiquetas.Count; i++)
                {
                    var valor = i < fila.Periodos.Count ? fila.Periodos[i].ToString("N0") : string.Empty;
                    tabla.Cell().Celda().AlignRight().Text(valor);
                }
                tabla.Cell().Celda().AlignRight().Text(fila.TotalDisplay);
            }
            tabla.Cell().Celda().Text("TOTAL").Bold();
            for (var i = 0; i < etiquetas.Count; i++)
            {
                var valor = i < datos.Demanda.TotalesPorPeriodo.Count
                    ? datos.Demanda.TotalesPorPeriodo[i].ToString("N0")
                    : string.Empty;
                tabla.Cell().Celda().AlignRight().Text(valor).Bold();
            }
            tabla.Cell().Celda().AlignRight().Text(datos.Demanda.TotalGeneralDisplay).Bold();
        });

        if (datos.Demanda.TieneDocentes)
        {
            EstilosPdf.TituloSeccion(col, "Docentes requeridos por período");
            col.Item().Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(1.6f);
                    foreach (var _ in etiquetas)
                        c.RelativeColumn();
                    c.RelativeColumn(1.1f);
                });
                tabla.Header(h =>
                {
                    h.Cell().CeldaHeader().Text("Tipo").Bold();
                    foreach (var etiqueta in etiquetas)
                        h.Cell().CeldaHeader().AlignRight().Text(etiqueta).Bold();
                    h.Cell().CeldaHeader().AlignRight().Text("Total").Bold();
                });
                foreach (var fila in datos.Demanda.DocentesPorPeriodo)
                {
                    tabla.Cell().Celda().Text(fila.Tipo);
                    for (var i = 0; i < etiquetas.Count; i++)
                    {
                        var valor = i < fila.Periodos.Count ? fila.Periodos[i].ToString("N0") : string.Empty;
                        tabla.Cell().Celda().AlignRight().Text(valor);
                    }
                    tabla.Cell().Celda().AlignRight().Text(fila.TotalDisplay);
                }
            });
        }

        EstilosPdf.Advertencia(col, datos.Demanda.MensajeAdvertencia);
    }

    private void ComponerIngresos(ColumnDescriptor col)
    {
        EstilosPdf.TituloSeccion(col, "Ingresos proyectados por ciclo");
        col.Item().Row(row =>
        {
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Ingreso bruto").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.Ingresos.TotalBrutoDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Becas (reducen ingreso)").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.Ingresos.TotalBecasDisplay).Bold().FontColor("#C62828");
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Ingreso neto").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.Ingresos.TotalNetoDisplay).Bold().FontColor("#2E7D32");
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("% becas aplicado").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.Ingresos.PorcentajeBecasDisplay).Bold();
            });
        });

        col.Item().PaddingTop(6).Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.4f);
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Ciclo").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Estudiantes").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Ingreso bruto").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Becas").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Ingreso neto").Bold();
            });
            foreach (var fila in datos.Ingresos.Filas)
            {
                tabla.Cell().Celda().Text(fila.CicloDisplay);
                tabla.Cell().Celda().AlignRight().Text(fila.TotalEstudiantesDisplay);
                tabla.Cell().Celda().AlignRight().Text(fila.TotalBrutoDisplay);
                tabla.Cell().Celda().AlignRight().Text(fila.TotalBecasDisplay);
                tabla.Cell().Celda().AlignRight().Text(fila.TotalNetoDisplay);
            }
            tabla.Cell().Celda().Text("TOTAL").Bold();
            tabla.Cell().Celda().Text(string.Empty);
            tabla.Cell().Celda().AlignRight().Text(datos.Ingresos.TotalBrutoDisplay).Bold();
            tabla.Cell().Celda().AlignRight().Text(datos.Ingresos.TotalBecasDisplay).Bold();
            tabla.Cell().Celda().AlignRight().Text(datos.Ingresos.TotalNetoDisplay).Bold();
        });

        EstilosPdf.Advertencia(col, datos.Ingresos.MensajeAdvertencia);
    }
}
