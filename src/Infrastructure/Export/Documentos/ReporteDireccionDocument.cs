using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SistemaAranceles.Application.DTOs.Reportes;

namespace SistemaAranceles.Infrastructure.Export.Documentos;

/// <summary>
/// KAN-47: reporte por dirección/destinatario. Compone solo las secciones de la
/// dirección seleccionada (Completo = todas, en el orden del informe original).
/// Las secciones sin datos imprimen una nota y no rompen el PDF.
/// </summary>
internal sealed class ReporteDireccionDocument(ReporteDireccionDatos datos) : IDocument
{
    public DocumentMetadata GetMetadata() => new() { Title = SeccionesReporte.Titulo(datos.Direccion) };

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(26);
            page.DefaultTextStyle(s => s.FontSize(8f));
            page.Header().Element(h => EstilosPdf.EncabezadoReporte(
                h, $"Reporte — {SeccionesReporte.Titulo(datos.Direccion)}",
                datos.CarreraNombre, datos.EscenarioNombre, datos.GeneradoEn));
            page.Footer().Element(EstilosPdf.PiePagina);
            page.Content().Column(col =>
            {
                foreach (var seccion in SeccionesReporte.ParaDireccion(datos.Direccion))
                    ComponerSeccion(col, seccion);
            });
        });
    }

    private void ComponerSeccion(ColumnDescriptor col, SeccionReporte seccion)
    {
        switch (seccion)
        {
            case SeccionReporte.ArancelMatricula:
                Opcional(col, datos.Arancel, "Arancel y Matrícula vigentes", SeccionesPdf.Arancel);
                break;
            case SeccionReporte.DemandaTabla:
                Opcional(col, datos.Demanda, "Demanda proyectada por ciclo", SeccionesPdf.DemandaTabla);
                break;
            case SeccionReporte.GraficoMatricula:
                Opcional(col, datos.Demanda?.TieneDatos == true ? datos.Demanda : null,
                    "Proyección de la matrícula (gráfico)", SeccionesPdf.GraficoMatricula);
                break;
            case SeccionReporte.DocentesTabla:
                Opcional(col, datos.Demanda, "Docentes requeridos por período", SeccionesPdf.DocentesTabla);
                break;
            case SeccionReporte.GraficoDocentes:
                Opcional(col, datos.Demanda?.TieneDocentes == true ? datos.Demanda : null,
                    "Docentes requeridos por período (gráfico)", SeccionesPdf.GraficoDocentes);
                break;
            case SeccionReporte.Ingresos:
                Opcional(col, datos.Ingresos, "Ingresos proyectados por ciclo", SeccionesPdf.Ingresos);
                break;
            case SeccionReporte.MaterialesUnidades:
                Opcional(col, datos.Materiales, "Materiales y suministros (unidades)", SeccionesPdf.MaterialesUnidades);
                break;
            case SeccionReporte.MaterialesMonetario:
                Opcional(col, datos.Materiales, "Materiales y suministros (valores monetarios)", SeccionesPdf.MaterialesMonetario);
                break;
            case SeccionReporte.ActivosFijos:
                Opcional(col, datos.ActivosFijos is { Count: > 0 } ? datos.ActivosFijos : null,
                    "Activos fijos — desglose de la situación inicial",
                    (c, activos) => SeccionesPdf.ActivosFijos(c, activos, datos.TotalesActivos));
                break;
            case SeccionReporte.InversionInicial:
                Opcional(col, datos.Inversion, "Inversión Inicial", SeccionesPdf.Inversion);
                break;
            case SeccionReporte.CapitalTrabajo:
                Opcional(col, datos.CapitalTrabajo, "Capital de Trabajo", SeccionesPdf.CapitalTrabajo);
                break;
            case SeccionReporte.Depreciacion:
                Opcional(col, datos.Depreciacion, "Depreciación de activos fijos", SeccionesPdf.Depreciacion);
                break;
            case SeccionReporte.Sueldos:
                Opcional(col, datos.Sueldos, "Sueldos del personal docente y administrativo", SeccionesPdf.Sueldos);
                break;
            case SeccionReporte.Mantenimiento:
                Opcional(col, datos.Mantenimiento, "Mantenimiento de edificio y servicios básicos", SeccionesPdf.Mantenimiento);
                break;
            case SeccionReporte.PlantaCentral:
                Opcional(col, datos.PlantaCentral, "Gasto administrativo / aporte a Planta Central", SeccionesPdf.PlantaCentral);
                break;
            case SeccionReporte.InvVinBecas:
                Opcional(col, datos.InvVinBecas, "Investigación y Vinculación con la Sociedad",
                    (c, dto) => SeccionesPdf.Matriz(c, "Investigación y Vinculación con la Sociedad",
                        dto.EtiquetasPeriodos,
                        dto.Filas.Select(f => (f.Concepto, f.PeriodosDisplay, f.TotalDisplay, f.EsEncabezadoGrupo, f.EsTotal)).ToList(),
                        dto.MensajeAdvertencia));
                break;
            case SeccionReporte.CostosGastos:
                Opcional(col, datos.CostosGastos, "Proyección de Costos y Gastos de la carrera",
                    (c, dto) => SeccionesPdf.Matriz(c, "Proyección de Costos y Gastos de la carrera",
                        dto.EtiquetasPeriodos,
                        dto.ProyeccionCostosGastos.Select(f => (f.Concepto, f.PeriodosDisplay, f.TotalDisplay, f.EsEncabezadoGrupo, f.EsTotal)).ToList(),
                        dto.MensajeAdvertencia));
                break;
            case SeccionReporte.FinanciamientoAmortizacion:
                Opcional(col, datos.Financiamiento, "Financiamiento y amortización",
                    (c, dto) => SeccionesPdf.Financiamiento(c, dto, "Financiamiento y amortización"));
                break;
            case SeccionReporte.Indicadores:
                Opcional(col, datos.Indicadores, "Indicadores Financieros", SeccionesPdf.Indicadores);
                break;
            case SeccionReporte.PuntoEquilibrio:
                Opcional(col, datos.PuntoEquilibrio, "Punto de Equilibrio", SeccionesPdf.PuntoEquilibrio);
                break;
            case SeccionReporte.FlujoFondos:
                Opcional(col, datos.FlujoFondos, "Flujo de Fondos",
                    (c, dto) => SeccionesPdf.Matriz(c, "Flujo de Fondos",
                        dto.EtiquetasPeriodos,
                        dto.Filas.Select(f => (f.Concepto, f.PeriodosDisplay, f.TotalDisplay, f.EsSeccion, f.EsTotal || f.EsResultado)).ToList(),
                        dto.MensajeAdvertencia));
                break;
            case SeccionReporte.EstadoResultados:
                Opcional(col, datos.EstadoResultados, "Estado de Pérdidas y Ganancias",
                    (c, dto) => SeccionesPdf.Matriz(c, "Estado de Pérdidas y Ganancias",
                        dto.EtiquetasPeriodos,
                        dto.Filas.Select(f => (f.Concepto, f.PeriodosDisplay, f.TotalDisplay, f.EsSeccion, f.EsTotal || f.EsResultado)).ToList(),
                        dto.MensajeAdvertencia));
                break;
            case SeccionReporte.BalanceProyectado:
                Opcional(col, datos.BalanceProyectado, "Balance Proyectado", SeccionesPdf.BalanceProyectado);
                break;
            case SeccionReporte.Ces:
                Opcional(col, datos.Ces, "INF CES / Presupuesto general de la carrera", SeccionesPdf.CuadrosCes);
                break;
        }
    }

    private static void Opcional<T>(ColumnDescriptor col, T? dto, string tituloSinDatos, Action<ColumnDescriptor, T> render)
        where T : class
    {
        if (dto is null)
            SeccionesPdf.SinDatos(col, tituloSinDatos);
        else
            render(col, dto);
    }
}
