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
                ComponerInversionYCapital(col);
                ComponerFinanciamiento(col);
                ComponerIndicadores(col);
                ComponerMatriz(col, "Proyección de Costos y Gastos",
                    datos.CostosGastos.EtiquetasPeriodos,
                    datos.CostosGastos.ProyeccionCostosGastos.Select(f => (
                        f.Concepto,
                        (IReadOnlyList<string>)f.PeriodosDisplay,
                        f.TotalDisplay,
                        f.EsEncabezadoGrupo,
                        f.EsTotal)).ToList(),
                    datos.CostosGastos.MensajeAdvertencia);
                ComponerMatriz(col, "Flujo de Fondos",
                    datos.FlujoFondos.EtiquetasPeriodos,
                    datos.FlujoFondos.Filas.Select(f => (
                        f.Concepto,
                        (IReadOnlyList<string>)f.PeriodosDisplay,
                        f.TotalDisplay,
                        f.EsSeccion,
                        f.EsTotal || f.EsResultado)).ToList(),
                    datos.FlujoFondos.MensajeAdvertencia);
            });
        });
    }

    private void ComponerInversionYCapital(ColumnDescriptor col)
    {
        EstilosPdf.TituloSeccion(col, "Inversión Inicial y Capital de Trabajo");
        col.Item().Row(row =>
        {
            row.RelativeItem().Table(tabla =>
            {
                tabla.ColumnsDefinition(c => { c.RelativeColumn(2.4f); c.RelativeColumn(1.2f); });
                tabla.Header(h =>
                {
                    h.Cell().CeldaHeader().Text("Inversión Inicial").Bold();
                    h.Cell().CeldaHeader().AlignRight().Text("Valor").Bold();
                });
                Fila(tabla, "Activos Diferidos", datos.Inversion.ActivosDiferidosDisplay);
                foreach (var cat in datos.Inversion.CategoriasActivosFijos)
                    Fila(tabla, cat.CategoriaNombre, cat.ValorDisplay);
                Fila(tabla, "Subtotal Activos Fijos", datos.Inversion.SubtotalActivosFijosDisplay, negrita: true);
                Fila(tabla, $"Capital de Trabajo ({datos.Inversion.MesesCapitalTrabajoDisplay} meses)", datos.Inversion.CapitalTrabajoDisplay);
                Fila(tabla, $"Imprevistos ({datos.Inversion.PorcentajeImprevistosInversionDisplay})", datos.Inversion.ImprevistosDisplay);
                Fila(tabla, "TOTAL INVERSIÓN", datos.Inversion.TotalInversionDisplay, negrita: true);
            });
            row.Spacing(12);
            row.RelativeItem().Table(tabla =>
            {
                tabla.ColumnsDefinition(c => { c.RelativeColumn(2.4f); c.RelativeColumn(1.2f); });
                tabla.Header(h =>
                {
                    h.Cell().CeldaHeader().Text("Capital de Trabajo (detalle)").Bold();
                    h.Cell().CeldaHeader().AlignRight().Text("Valor").Bold();
                });
                Fila(tabla, "Sueldos y cargos", datos.CapitalTrabajo.SubtotalCargosDisplay);
                Fila(tabla, "Materiales de oficina", datos.CapitalTrabajo.SubtotalMaterialesDisplay);
                Fila(tabla, "Aseo y limpieza", datos.CapitalTrabajo.SubtotalAseoDisplay);
                Fila(tabla, "Accesorios", datos.CapitalTrabajo.SubtotalAccesoriosDisplay);
                Fila(tabla, "Total mensual", datos.CapitalTrabajo.TotalMensualDisplay, negrita: true);
                Fila(tabla, $"Total ({datos.CapitalTrabajo.MesesCapitalTrabajo} meses)", datos.CapitalTrabajo.TotalCapitalTrabajoDisplay, negrita: true);
            });
        });
    }

    private void ComponerFinanciamiento(ColumnDescriptor col)
    {
        EstilosPdf.TituloSeccion(col, "Financiamiento de la Inversión (3 fuentes)");
        col.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2f);
                c.RelativeColumn(0.8f);
                c.RelativeColumn(1.2f);
                c.RelativeColumn(2f);
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Fuente").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("%").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Monto").Bold();
                h.Cell().CeldaHeader().Text("Entidad").Bold();
            });
            FilaFin(tabla, "Recursos Propios", datos.Financiamiento.PorcentajePropioDisplay, datos.Financiamiento.MontoPropioDisplay, string.Empty);
            FilaFin(tabla, "Préstamo Bancario", datos.Financiamiento.PorcentajePrestamoDisplay, datos.Financiamiento.MontoPrestamoDisplay, datos.Financiamiento.NombreEntidadPrestamo ?? string.Empty);
            FilaFin(tabla, "Convenio Institucional", datos.Financiamiento.PorcentajeConvenioDisplay, datos.Financiamiento.MontoConvenioDisplay, datos.Financiamiento.NombreEntidadConvenio ?? string.Empty);
            FilaFin(tabla, "TOTAL", "100%", datos.Financiamiento.TotalInversionDisplay, string.Empty, negrita: true);
        });
        col.Item().PaddingTop(4).Text(
            $"Préstamo: tasa {datos.Financiamiento.TasaInteresAnualDisplay} anual, plazo {datos.Financiamiento.PlazoMeses} meses, " +
            $"cuota mensual {datos.Financiamiento.CuotaMensualDisplay}, intereses totales {datos.Financiamiento.TotalInteresesDisplay}.")
            .FontSize(8).FontColor(EstilosPdf.ColorGris);
    }

    private void ComponerIndicadores(ColumnDescriptor col)
    {
        EstilosPdf.TituloSeccion(col, "Indicadores Financieros");
        col.Item().Row(row =>
        {
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("TMR (tasa mínima de rendimiento)").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.Indicadores.TmrDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("VAN").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.Indicadores.VanDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("TIR").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.Indicadores.TirDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Estado de viabilidad").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(datos.Indicadores.EstadoViabilidad).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
        });
    }

    private static void ComponerMatriz(
        ColumnDescriptor col,
        string titulo,
        IReadOnlyList<string> etiquetas,
        IReadOnlyList<(string Concepto, IReadOnlyList<string> Periodos, string Total, bool EsSeccion, bool EsTotal)> filas,
        string? advertencia)
    {
        EstilosPdf.TituloSeccion(col, titulo);
        if (filas.Count == 0 || etiquetas.Count == 0)
        {
            col.Item().Text("Sin datos para esta sección.").FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            EstilosPdf.Advertencia(col, advertencia);
            return;
        }

        col.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.6f);
                foreach (var _ in etiquetas)
                    c.RelativeColumn();
                c.RelativeColumn(1.1f);
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Concepto").Bold();
                foreach (var etiqueta in etiquetas)
                    h.Cell().CeldaHeader().AlignRight().Text(etiqueta).Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Total").Bold();
            });
            foreach (var fila in filas)
            {
                if (fila.EsSeccion)
                {
                    tabla.Cell().ColumnSpan((uint)(etiquetas.Count + 2)).CeldaSeccion().Text(fila.Concepto).Bold();
                    continue;
                }
                tabla.Cell().Celda().Text(t => { var s = t.Span(fila.Concepto); if (fila.EsTotal) s.Bold(); });
                for (var i = 0; i < etiquetas.Count; i++)
                {
                    var valor = i < fila.Periodos.Count ? fila.Periodos[i] : string.Empty;
                    tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(valor); if (fila.EsTotal) s.Bold(); });
                }
                tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.Total); if (fila.EsTotal) s.Bold(); });
            }
        });
        EstilosPdf.Advertencia(col, advertencia);
    }

    private static void Fila(TableDescriptor tabla, string concepto, string valor, bool negrita = false)
    {
        tabla.Cell().Celda().Text(t => { var s = t.Span(concepto); if (negrita) s.Bold(); });
        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(valor); if (negrita) s.Bold(); });
    }

    private static void FilaFin(TableDescriptor tabla, string fuente, string porcentaje, string monto, string entidad, bool negrita = false)
    {
        tabla.Cell().Celda().Text(t => { var s = t.Span(fuente); if (negrita) s.Bold(); });
        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(porcentaje); if (negrita) s.Bold(); });
        tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(monto); if (negrita) s.Bold(); });
        tabla.Cell().Celda().Text(entidad);
    }
}
