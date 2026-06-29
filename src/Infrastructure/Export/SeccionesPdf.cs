using QuestPDF.Fluent;
using SistemaAranceles.Application.Comun;
using SistemaAranceles.Application.DTOs.Amortizacion;
using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.DTOs.InversionInicial;
using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.DTOs.Reportes;
using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Application.DTOs.TasaRetencion;

namespace SistemaAranceles.Infrastructure.Export;

/// <summary>
/// Renderers de sección compartidos por los reportes PDF (KAN-47): los documentos
/// existentes (CES/Financiero/Demanda) y el reporte por dirección componen con
/// estos mismos bloques para no duplicar tablas ni formatos.
/// </summary>
internal static class SeccionesPdf
{
    public static void SinDatos(ColumnDescriptor col, string titulo)
    {
        EstilosPdf.TituloSeccion(col, titulo);
        col.Item().Text("No existen datos suficientes para generar esta sección.")
            .FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
    }

    // ============ Resumen de Indicadores Clave (KAN) ============

    /// <summary>
    /// Arma las filas del "Resumen de Indicadores Clave" desde los DTOs ya cargados
    /// del reporte. Solo agrega un valor si su sección trae datos, así el resumen se
    /// adapta a la dirección. <c>EsGrupo = true</c> marca una fila de subtítulo.
    /// Compartido por el PDF y el XLSX para no duplicar la lista de indicadores.
    /// </summary>
    public static IReadOnlyList<(string Etiqueta, string Valor, bool EsGrupo)> ResumenIndicadoresFilas(
        ReporteDireccionDatos d)
    {
        var filas = new List<(string, string, bool)>();
        void Grupo(string t) => filas.Add((t, string.Empty, true));
        void Val(string et, string? v) { if (!string.IsNullOrWhiteSpace(v)) filas.Add((et, v!, false)); }

        if (d.RetencionSimulacion is { TieneDatos: true } r)
        {
            Grupo("Estudiantes y retención");
            Val("Estudiantes que ingresan (ciclo 1)", r.EstudiantesPeriodo1.ToString("N0"));
            Val("Tasa de retención aplicada", $"{r.TasaRetencionAplicada:N2} %");
            Val("Tasa de graduación aplicada", $"{r.TasaGraduacionAplicada:N2} %");
            if (r.AlumnosPeriodo1PorCiclo.Count > 0 && r.TotalCiclos > 0)
                Val($"Estudiantes al {r.TotalCiclos}.º ciclo (retención acumulada)",
                    r.AlumnosPeriodo1PorCiclo[^1].ToString("N0"));
        }

        if (d.Demanda is { TieneDatos: true } dem)
        {
            if (d.RetencionSimulacion is not { TieneDatos: true }) Grupo("Estudiantes");
            Val("Total estudiantes (acumulado del período)", dem.TotalGeneralDisplay);
            if (dem.TotalesPorPeriodo.Count > 0)
                Val("Estudiantes en el último período", dem.TotalesPorPeriodo[^1].ToString("N0"));
        }

        if (d.Ces is { TieneDatos: true } ces)
        {
            Grupo("Costo y arancel por semestre");
            Val("Costo por estudiante", ces.CostoPorEstudianteDisplay);
            Val("Costo de la carrera (por estudiante)", ces.CostoDeLaCarreraDisplay);
            Val("Costo por semestre (referencial)", ces.CostoPorSemestreDisplay);
            Val("Arancel por semestre (óptimo VAN≈0)", ces.ArancelPorSemestreDisplay);
            Val("Matrícula", ces.MatriculaDisplay);
            Val("Total por semestre (arancel + matrícula)", ces.TotalPorSemestreDisplay);
        }

        if (d.Indicadores is { TieneDatos: true } ind)
        {
            Grupo("Indicadores financieros");
            Val("TMR (tasa mínima de rendimiento)", ind.TmrDisplay);
            Val("VAN", ind.VanDisplay);
            Val("TIR", ind.TirDisplay);
        }

        if (d.PuntoEquilibrio is { TieneDatos: true } pe)
        {
            if (d.Indicadores is not { TieneDatos: true }) Grupo("Indicadores financieros");
            // Valor monetario del período base (último proyectado), igual que la pantalla de
            // Punto de Equilibrio; el conteo de estudiantes no aplica como "punto de equilibrio anual".
            var periodoBase = pe.Periodos.FirstOrDefault(x => x.EtiquetaPeriodo == pe.PeriodoBaseEtiqueta)
                ?? pe.Periodos.LastOrDefault(x => x.EsCalculable);
            Val("Punto de equilibrio (anual)", periodoBase?.PuntoEquilibrioMonetarioDisplay);
        }

        return filas;
    }

    public static void ResumenIndicadores(ColumnDescriptor col, ReporteDireccionDatos datos)
    {
        EstilosPdf.TituloSeccion(col, "Resumen de Indicadores Clave");
        var filas = ResumenIndicadoresFilas(datos);
        if (filas.Count == 0)
        {
            col.Item().Text("No existen datos suficientes para generar esta sección.")
                .FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            return;
        }

        col.Item().Text("Valores clave del caso para lectura rápida y para la planilla de validación.")
            .FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
        col.Item().PaddingTop(3).Table(tabla =>
        {
            tabla.ColumnsDefinition(c => { c.RelativeColumn(3f); c.RelativeColumn(2f); });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Indicador").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Valor").Bold();
            });
            foreach (var (etiqueta, valor, esGrupo) in filas)
            {
                if (esGrupo)
                {
                    tabla.Cell().ColumnSpan(2).Celda()
                        .Text(t => t.Span(etiqueta).Bold().FontColor(EstilosPdf.ColorPrimario));
                }
                else
                {
                    tabla.Cell().Celda().Text(etiqueta);
                    tabla.Cell().Celda().AlignRight().Text(t => t.Span(valor).Bold());
                }
            }
        });
    }

    // ============ Demanda / Estrategia Comercial ============

    public static void Arancel(ColumnDescriptor col, ArancelEfectivoDto arancel)
    {
        EstilosPdf.TituloSeccion(col, "Arancel y Matrícula vigentes");
        col.Item().Row(row =>
        {
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Arancel efectivo").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(arancel.ArancelDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Matrícula efectiva").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(arancel.MatriculaDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Total por semestre (arancel + matrícula)").FontSize(8).FontColor(EstilosPdf.ColorGris);
                var total = (arancel.ArancelEfectivo ?? 0m) + arancel.MatriculaEfectiva;
                c.Item().Text($"$ {total:N2}").Bold().FontColor(EstilosPdf.ColorPrimario);
            });
        });
        EstilosPdf.Advertencia(col, arancel.MensajeAdvertencia);
    }

    public static void DemandaTabla(ColumnDescriptor col, DemandaProyectadaDto demanda)
    {
        EstilosPdf.TituloSeccion(col, $"Demanda considerada por la carrera de {demanda.CarreraNombre} — proyección por ciclo (estudiantes)");
        var etiquetas = demanda.EtiquetasPeriodos;
        if (!demanda.TieneDatos)
        {
            col.Item().Text("Sin datos de demanda proyectada.").FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            EstilosPdf.Advertencia(col, demanda.MensajeAdvertencia);
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
            foreach (var fila in demanda.Filas)
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
                var valor = i < demanda.TotalesPorPeriodo.Count
                    ? demanda.TotalesPorPeriodo[i].ToString("N0")
                    : string.Empty;
                tabla.Cell().Celda().AlignRight().Text(valor).Bold();
            }
            tabla.Cell().Celda().AlignRight().Text(demanda.TotalGeneralDisplay).Bold();
        });
        EstilosPdf.Advertencia(col, demanda.MensajeAdvertencia);
    }

    public static void RetencionSimulacion(ColumnDescriptor col, RetencionSimulacionReporteDto retencion)
    {
        var titulo = "Retención y graduación — resultado de simulación por ciclos (estructura semestral)";
        if (!retencion.TieneDatos)
        {
            SinDatos(col, titulo);
            return;
        }

        EstilosPdf.TituloSeccion(col, titulo);

        // Parámetros del escenario (la tabla usa las tasas configuradas, igual que la pantalla).
        col.Item().Row(row =>
        {
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Tasa de retención").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text($"{retencion.TasaRetencionPorcentaje:N1}%").Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Tasa de graduación").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text($"{retencion.TasaGraduacionPorcentaje:N1}%").Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Paralelos abr-ago / sep-feb").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text($"{retencion.ParalelosPeriodo1} / {retencion.ParalelosPeriodo2}").Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Estudiantes de ingreso abr / sep").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text($"{retencion.EstudiantesPeriodo1:N0} / {retencion.EstudiantesPeriodo2:N0}").Bold().FontColor(EstilosPdf.ColorPrimario);
            });
        });

        if (retencion.MetaRetencionPorcentaje is not null || retencion.MetaGraduacionPorcentaje is not null)
        {
            col.Item().PaddingTop(4).Text(
                    $"Metas de referencia aplicadas a la proyección y a los cálculos financieros: retención {retencion.TasaRetencionAplicada:N1}%, graduación {retencion.TasaGraduacionAplicada:N1}%.")
                .FontSize(7.5f).Italic().FontColor(EstilosPdf.ColorGris);
        }

        col.Item().PaddingTop(6).Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.2f);
                for (var i = 0; i < retencion.TotalCiclos; i++)
                    c.RelativeColumn();
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Grupo de ingreso").Bold();
                for (var ciclo = 1; ciclo <= retencion.TotalCiclos; ciclo++)
                    h.Cell().CeldaHeader().AlignRight().Text($"{ciclo}°").Bold();
            });

            tabla.Cell().Celda().Text("Abril - agosto");
            for (var i = 0; i < retencion.TotalCiclos; i++)
            {
                var valor = i < retencion.AlumnosPeriodo1PorCiclo.Count
                    ? retencion.AlumnosPeriodo1PorCiclo[i].ToString("N0")
                    : string.Empty;
                tabla.Cell().Celda().AlignRight().Text(valor);
            }

            tabla.Cell().Celda().Text("Septiembre - febrero");
            for (var i = 0; i < retencion.TotalCiclos; i++)
            {
                var valor = i < retencion.AlumnosPeriodo2PorCiclo.Count
                    ? retencion.AlumnosPeriodo2PorCiclo[i].ToString("N0")
                    : string.Empty;
                tabla.Cell().Celda().AlignRight().Text(valor);
            }
        });
        col.Item().PaddingTop(2).Text(
                "Estudiantes del grupo de ingreso que llegan a cada ciclo: la primera mitad de la malla decae con la tasa de retención y la segunda con la de graduación.")
            .FontSize(7.5f).Italic().FontColor(EstilosPdf.ColorGris);
    }

    public static void DocentesTabla(ColumnDescriptor col, DemandaProyectadaDto demanda)
    {
        EstilosPdf.TituloSeccion(col, "Docentes requeridos por la carrera, por período");
        var etiquetas = demanda.EtiquetasPeriodos;
        if (!demanda.TieneDocentes)
        {
            col.Item().Text("Sin datos de docentes requeridos.").FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            EstilosPdf.Advertencia(col, demanda.MensajeAdvertenciaDocentes);
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
                h.Cell().CeldaHeader().Text("Tipo").Bold();
                foreach (var etiqueta in etiquetas)
                    h.Cell().CeldaHeader().AlignRight().Text(etiqueta).Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Total").Bold();
            });
            foreach (var fila in demanda.DocentesPorPeriodo)
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
        EstilosPdf.Advertencia(col, demanda.MensajeAdvertenciaDocentes);
    }

    public static void GraficoMatricula(ColumnDescriptor col, DemandaProyectadaDto demanda)
        => GraficosPdf.GraficoBarras(col, "Proyección de la matrícula (estudiantes por período)",
            demanda.EtiquetasPeriodos,
            [("Matrícula", demanda.TotalesPorPeriodo, GraficosPdf.Paleta[0])]);

    public static void GraficoDocentes(ColumnDescriptor col, DemandaProyectadaDto demanda)
        => GraficosPdf.GraficoBarras(col, "Docentes requeridos por período",
            demanda.EtiquetasPeriodos,
            demanda.DocentesPorPeriodo
                // En el gráfico solo va el desglose por tipo: las filas de "Horas asignadas ..."
                // mezclan escalas (horas vs. personas) y "Docentes Requeridos" es el total
                // que duplica visualmente a sus componentes.
                .Where(fila => !fila.Tipo.Contains("Horas", StringComparison.OrdinalIgnoreCase)
                            && !string.Equals(fila.Tipo, "Docentes Requeridos", StringComparison.OrdinalIgnoreCase))
                .Select((fila, i) => (fila.Tipo, fila.Periodos, GraficosPdf.Paleta[i % GraficosPdf.Paleta.Length]))
                .ToList());

    public static void Ingresos(ColumnDescriptor col, IngresosProyectadosDto ingresos)
    {
        EstilosPdf.TituloSeccion(col, "Ingresos proyectados por ciclo");
        col.Item().Row(row =>
        {
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Ingreso bruto").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(ingresos.TotalBrutoDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Becas (reducen ingreso)").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(ingresos.TotalBecasDisplay).Bold().FontColor("#C62828");
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Ingreso neto").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(ingresos.TotalNetoDisplay).Bold().FontColor("#2E7D32");
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("% becas aplicado").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(ingresos.PorcentajeBecasDisplay).Bold();
            });
        });

        DescuentosPorCiclo(col, ingresos);

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
                h.Cell().CeldaHeader().AlignRight().Text("Estudiantes (suma de períodos)").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Ingreso bruto").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Becas").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Ingreso neto").Bold();
            });
            foreach (var fila in ingresos.Filas)
            {
                tabla.Cell().Celda().Text(fila.CicloDisplay);
                tabla.Cell().Celda().AlignRight().Text(fila.TotalEstudiantesDisplay);
                tabla.Cell().Celda().AlignRight().Text(fila.TotalBrutoDisplay);
                tabla.Cell().Celda().AlignRight().Text(fila.TotalBecasDisplay);
                tabla.Cell().Celda().AlignRight().Text(fila.TotalNetoDisplay);
            }
            tabla.Cell().Celda().Text("TOTAL").Bold();
            tabla.Cell().Celda().Text(string.Empty);
            tabla.Cell().Celda().AlignRight().Text(ingresos.TotalBrutoDisplay).Bold();
            tabla.Cell().Celda().AlignRight().Text(ingresos.TotalBecasDisplay).Bold();
            tabla.Cell().Celda().AlignRight().Text(ingresos.TotalNetoDisplay).Bold();
        });

        EstilosPdf.Advertencia(col, ingresos.MensajeAdvertencia);

        col.Item().PaddingTop(2).Text(
            "Nota: \"Estudiantes (suma de períodos)\" acumula los estudiantes del ciclo a lo largo de todos los períodos proyectados; no es la matrícula simultánea de un solo período.")
            .FontSize(7).Italic().FontColor(EstilosPdf.ColorGris);
    }

    private static void DescuentosPorCiclo(ColumnDescriptor col, IngresosProyectadosDto ingresos)
    {
        var filas = ingresos.Filas
            .Select(f => f.Periodos.FirstOrDefault())
            .Where(celda => celda is not null)
            .Cast<IngresoPeriodoCeldaDto>()
            .ToList();
        if (filas.Count == 0)
            return;

        col.Item().PaddingTop(6).Text("Descuento por ciclo y arancel a cobrar")
            .FontSize(9).Bold().FontColor(EstilosPdf.ColorPrimario);
        col.Item().PaddingTop(3).Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.2f);
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn();
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Ciclo").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Arancel base").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("% descuento").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Arancel a cobrar").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Matrícula").Bold();
            });
            foreach (var celda in filas)
            {
                tabla.Cell().Celda().Text(celda.CicloDisplay);
                tabla.Cell().Celda().AlignRight().Text(celda.ArancelBaseDisplay);
                tabla.Cell().Celda().AlignRight().Text(celda.DescuentoCicloDisplay);
                tabla.Cell().Celda().AlignRight().Text(celda.ArancelCicloDisplay);
                tabla.Cell().Celda().AlignRight().Text(celda.MatriculaCicloDisplay);
            }
        });
    }

    // ============ Financiero ============

    public static void InversionYCapital(ColumnDescriptor col, InversionInicialTotalDto inversion, ResumenCapitalTrabajoDto capital)
    {
        EstilosPdf.TituloSeccion(col, "Inversión Inicial y Capital de Trabajo");
        col.Item().Row(row =>
        {
            row.RelativeItem().Table(tabla => TablaInversion(tabla, inversion));
            row.Spacing(12);
            row.RelativeItem().Table(tabla => TablaCapital(tabla, capital));
        });
    }

    public static void Inversion(ColumnDescriptor col, InversionInicialTotalDto inversion)
    {
        EstilosPdf.TituloSeccion(col, "Inversión Inicial");
        col.Item().Table(tabla => TablaInversion(tabla, inversion));
    }

    public static void CapitalTrabajo(ColumnDescriptor col, ResumenCapitalTrabajoDto capital)
    {
        EstilosPdf.TituloSeccion(col, "Capital de Trabajo");
        col.Item().Table(tabla => TablaCapital(tabla, capital));
    }

    private static void TablaInversion(TableDescriptor tabla, InversionInicialTotalDto inversion)
    {
        tabla.ColumnsDefinition(c => { c.RelativeColumn(2.4f); c.RelativeColumn(1.2f); });
        tabla.Header(h =>
        {
            h.Cell().CeldaHeader().Text("Inversión Inicial").Bold();
            h.Cell().CeldaHeader().AlignRight().Text("Valor").Bold();
        });
        Fila(tabla, "Activos Diferidos", inversion.ActivosDiferidosDisplay);
        foreach (var cat in inversion.CategoriasActivosFijos)
            Fila(tabla, cat.CategoriaNombre, cat.ValorDisplay);
        Fila(tabla, "Subtotal Activos Fijos", inversion.SubtotalActivosFijosDisplay, negrita: true);
        Fila(tabla, $"Capital de Trabajo ({inversion.MesesCapitalTrabajoDisplay} meses)", inversion.CapitalTrabajoDisplay);
        Fila(tabla, $"Imprevistos ({inversion.PorcentajeImprevistosInversionDisplay})", inversion.ImprevistosDisplay);
        Fila(tabla, "TOTAL INVERSIÓN", inversion.TotalInversionDisplay, negrita: true);
    }

    private static void TablaCapital(TableDescriptor tabla, ResumenCapitalTrabajoDto capital)
    {
        tabla.ColumnsDefinition(c => { c.RelativeColumn(2.4f); c.RelativeColumn(1.2f); });
        tabla.Header(h =>
        {
            h.Cell().CeldaHeader().Text("Capital de Trabajo (detalle)").Bold();
            h.Cell().CeldaHeader().AlignRight().Text("Valor").Bold();
        });
        Fila(tabla, "Sueldos y cargos", capital.SubtotalCargosDisplay);
        Fila(tabla, "Materiales de oficina", capital.SubtotalMaterialesDisplay);
        Fila(tabla, "Aseo y limpieza", capital.SubtotalAseoDisplay);
        Fila(tabla, "Accesorios", capital.SubtotalAccesoriosDisplay);
        Fila(tabla, "Total mensual", capital.TotalMensualDisplay, negrita: true);
        Fila(tabla, $"Total ({capital.MesesCapitalTrabajo} meses)", capital.TotalCapitalTrabajoDisplay, negrita: true);
    }

    public static void Financiamiento(ColumnDescriptor col, ResumenFinanciamientoDto financiamiento,
        string titulo = "Financiamiento de la Inversión (3 fuentes)")
    {
        EstilosPdf.TituloSeccion(col, titulo);
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
            FilaFin(tabla, "Recursos Propios", financiamiento.PorcentajePropioDisplay, financiamiento.MontoPropioDisplay, string.Empty);
            FilaFin(tabla, "Préstamo Bancario", financiamiento.PorcentajePrestamoDisplay, financiamiento.MontoPrestamoDisplay, financiamiento.NombreEntidadPrestamo ?? string.Empty);
            FilaFin(tabla, "Convenio Institucional", financiamiento.PorcentajeConvenioDisplay, financiamiento.MontoConvenioDisplay, financiamiento.NombreEntidadConvenio ?? string.Empty);
            FilaFin(tabla, "TOTAL", "100%", financiamiento.TotalInversionDisplay, string.Empty, negrita: true);
        });
        col.Item().PaddingTop(4).Text(
            $"Préstamo: tasa {financiamiento.TasaInteresAnualDisplay} anual, plazo {financiamiento.PlazoMeses} meses, " +
            $"cuota mensual {financiamiento.CuotaMensualDisplay}, intereses totales {financiamiento.TotalInteresesDisplay}.")
            .FontSize(8).FontColor(EstilosPdf.ColorGris);
    }

    public static void Indicadores(ColumnDescriptor col, IndicadoresFinancierosDto indicadores)
    {
        EstilosPdf.TituloSeccion(col, "Indicadores Financieros");
        col.Item().Row(row =>
        {
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("TMR (tasa mínima de rendimiento)").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(indicadores.TmrDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("VAN").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(indicadores.VanDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("TIR").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(indicadores.TirDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Estado de viabilidad").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(indicadores.EstadoViabilidad).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
        });
    }

    public static void PuntoEquilibrio(ColumnDescriptor col, PuntoEquilibrioDto pe)
    {
        EstilosPdf.TituloSeccion(col, "Punto de Equilibrio");
        // Mismos cuadros que la pantalla de Análisis Financiero (resumen estilo Excel);
        // pe.Periodos puede venir vacío aunque el resumen sí tenga datos.
        if (!pe.TieneResumenExcel)
        {
            col.Item().Text("No existen datos suficientes para generar esta sección.")
                .FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            EstilosPdf.Advertencia(col, pe.MensajeAdvertencia);
            return;
        }

        if (!string.IsNullOrWhiteSpace(pe.PeriodoBaseEtiqueta))
            col.Item().Text($"Período del punto de equilibrio (período base): {pe.PeriodoBaseEtiqueta}")
                .FontSize(8).FontColor(EstilosPdf.ColorGris);

        if (pe.ProyeccionResultados.Count > 0)
        {
            col.Item().PaddingTop(4).Text("Proyección de Resultados y Punto de Equilibrio")
                .FontSize(9).Bold().FontColor(EstilosPdf.ColorPrimario);
            col.Item().PaddingTop(3).Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.4f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(0.8f);
                });
                tabla.Header(h =>
                {
                    h.Cell().CeldaHeader().Text("Concepto").Bold();
                    h.Cell().CeldaHeader().AlignRight().Text("Valor anual").Bold();
                    h.Cell().CeldaHeader().AlignRight().Text("Valor mensual").Bold();
                    h.Cell().CeldaHeader().AlignRight().Text("%").Bold();
                });
                foreach (var fila in pe.ProyeccionResultados)
                {
                    var resaltada = fila.EsTotal || fila.EsResultado;
                    tabla.Cell().Celda().Text(t => { var s = t.Span(fila.Concepto); if (resaltada) s.Bold(); });
                    tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.ValorAnualDisplay); if (resaltada) s.Bold(); });
                    tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.ValorMensualDisplay); if (resaltada) s.Bold(); });
                    tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.PorcentajeDisplay); if (resaltada) s.Bold(); });
                }
            });
        }

        if (pe.AnalisisPuntoEquilibrio.Count > 0)
        {
            col.Item().PaddingTop(6).Text("Análisis del Punto de Equilibrio")
                .FontSize(9).Bold().FontColor(EstilosPdf.ColorPrimario);
            col.Item().PaddingTop(3).Table(tabla =>
            {
                tabla.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(2.8f);
                    c.RelativeColumn(1.3f);
                    c.RelativeColumn(1.3f);
                });
                tabla.Header(h =>
                {
                    h.Cell().CeldaHeader().Text("Concepto").Bold();
                    h.Cell().CeldaHeader().AlignRight().Text("PE Anual").Bold();
                    h.Cell().CeldaHeader().AlignRight().Text("PE Mensual").Bold();
                });
                foreach (var fila in pe.AnalisisPuntoEquilibrio)
                {
                    var resaltada = fila.EsTotal || fila.EsResultado;
                    tabla.Cell().Celda().Text(t => { var s = t.Span(fila.Concepto); if (resaltada) s.Bold(); });
                    tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.PeAnualDisplay); if (resaltada) s.Bold(); });
                    tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.PeMensualDisplay); if (resaltada) s.Bold(); });
                }
            });
        }

        EstilosPdf.Advertencia(col, pe.MensajeAdvertencia);
    }

    public static void BalanceProyectado(ColumnDescriptor col, BalanceProyectadoDto balance)
    {
        EstilosPdf.TituloSeccion(col, "Balance Proyectado");
        if (!balance.TieneDatos)
        {
            col.Item().Text("No existen datos suficientes para generar esta sección.")
                .FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            EstilosPdf.Advertencia(col, balance.MensajeAdvertencia);
            return;
        }

        var etiquetas = balance.EtiquetasPeriodos;
        col.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.6f);
                foreach (var _ in etiquetas)
                    c.RelativeColumn();
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Concepto").Bold();
                foreach (var etiqueta in etiquetas)
                    h.Cell().CeldaHeader().AlignRight().Text(etiqueta).Bold();
            });
            foreach (var fila in balance.Filas)
            {
                if (fila.EsSeccion)
                {
                    tabla.Cell().ColumnSpan((uint)(etiquetas.Count + 1)).CeldaSeccion().Text(fila.Concepto).Bold();
                    continue;
                }
                var resaltada = fila.EsTotal || fila.EsResultado;
                tabla.Cell().Celda().Text(t => { var s = t.Span(fila.Concepto); if (resaltada) s.Bold(); });
                var displays = fila.PeriodosDisplay;
                for (var i = 0; i < etiquetas.Count; i++)
                {
                    var valor = i < displays.Count ? displays[i] : string.Empty;
                    tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(valor); if (resaltada) s.Bold(); });
                }
            }
        });
        EstilosPdf.Advertencia(col, balance.MensajeAdvertencia);
    }

    public static void Matriz(
        ColumnDescriptor col,
        string titulo,
        IReadOnlyList<string> etiquetas,
        IReadOnlyList<(string Concepto, IReadOnlyList<string> Periodos, string Total, bool EsSeccion, bool EsTotal)> filas,
        string? advertencia)
    {
        if (!string.IsNullOrEmpty(titulo))
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

    // ============ Administrativa ============

    public static void MaterialesUnidades(ColumnDescriptor col, MaterialesProyectadosDto materiales)
    {
        var celdas = materiales.Cantidades;
        var titulo = "Materiales de oficina, suministros de aseo y limpieza — proyección de necesidades (unidades)";
        if (celdas.Count == 0)
        {
            SinDatos(col, titulo);
            return;
        }

        var etiquetas = EtiquetasOrdenadas(celdas.Select(x => (x.Anio, x.NumeroPeriodo, x.EtiquetaPeriodo)));
        var filas = FilasMaterialesAgrupadas(
            celdas.Select(x => (x.Categoria, x.Concepto, x.Anio, x.NumeroPeriodo, x.Cantidad)),
            etiquetas,
            v => v.ToString("N2"));

        Matriz(col, titulo, etiquetas.Select(e => e.Etiqueta).ToList(), filas, materiales.MensajeAdvertencia);
    }

    public static void MaterialesMonetario(ColumnDescriptor col, MaterialesProyectadosDto materiales)
    {
        var celdas = materiales.Monetarios;
        var titulo = "Materiales de oficina, suministros de aseo y limpieza — proyección de necesidades (valores monetarios)";
        if (celdas.Count == 0)
        {
            SinDatos(col, titulo);
            return;
        }

        var etiquetas = EtiquetasOrdenadas(celdas.Select(x => (x.Anio, x.NumeroPeriodo, x.EtiquetaPeriodo)));
        var filas = FilasMaterialesAgrupadas(
            celdas.Select(x => (x.Categoria, x.Concepto, x.Anio, x.NumeroPeriodo, x.Costo)),
            etiquetas,
            v => $"$ {v:N2}");

        var totalesPorPeriodo = etiquetas
            .Select(e => celdas.Where(x => x.Anio == e.Anio && x.NumeroPeriodo == e.Numero).Sum(x => x.Costo))
            .Select(v => $"$ {v:N2}").ToList();
        filas.Add(("TOTAL", totalesPorPeriodo, materiales.TotalCostoDisplay, false, true));

        Matriz(col, titulo, etiquetas.Select(e => e.Etiqueta).ToList(), filas, materiales.MensajeAdvertencia);
    }

    private static List<(int Anio, int Numero, string Etiqueta)> EtiquetasOrdenadas(
        IEnumerable<(int Anio, int Numero, string Etiqueta)> celdas)
        => celdas.Distinct().OrderBy(x => x.Anio).ThenBy(x => x.Numero).ToList();

    /// <summary>
    /// Filas de materiales agrupadas como en pantalla: una banda por categoría (nombre legible)
    /// y debajo sus conceptos, en vez del crudo "CATEGORIA_X — Concepto" por fila (KAN-49).
    /// </summary>
    private static List<(string Concepto, IReadOnlyList<string> Periodos, string Total, bool EsSeccion, bool EsTotal)> FilasMaterialesAgrupadas(
        IEnumerable<(string Categoria, string Concepto, int Anio, int NumeroPeriodo, decimal Valor)> celdas,
        List<(int Anio, int Numero, string Etiqueta)> etiquetas,
        Func<decimal, string> formato)
    {
        var filas = new List<(string, IReadOnlyList<string>, string, bool, bool)>();
        var bandaVacia = etiquetas.Select(_ => string.Empty).ToList();

        foreach (var categoria in celdas
            .GroupBy(x => x.Categoria)
            .OrderBy(g => CategoriaMaterialDisplay.Orden(g.Key)).ThenBy(g => g.Key))
        {
            filas.Add((CategoriaMaterialDisplay.Formatear(categoria.Key), bandaVacia, string.Empty, true, false));

            foreach (var concepto in categoria.GroupBy(x => x.Concepto).OrderBy(g => g.Key))
            {
                var porPeriodo = concepto.ToDictionary(x => (x.Anio, x.NumeroPeriodo), x => x.Valor);
                var valores = etiquetas
                    .Select(e => formato(porPeriodo.GetValueOrDefault((e.Anio, e.Numero))))
                    .ToList();
                filas.Add((concepto.Key, valores, formato(concepto.Sum(x => x.Valor)), false, false));
            }
        }

        return filas;
    }

    public static void ActivosFijos(ColumnDescriptor col, IReadOnlyList<ActivoFijoDto> activos, TotalesActivosFijosDto? totales)
    {
        EstilosPdf.TituloSeccion(col, "Activos fijos — desglose de la situación inicial");
        col.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.6f);
                c.RelativeColumn(1.4f);
                c.RelativeColumn(0.7f);
                c.RelativeColumn(0.7f);
                c.RelativeColumn(1f);
                c.RelativeColumn(1.1f);
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Descripción").Bold();
                h.Cell().CeldaHeader().Text("Categoría").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Cantidad").Bold();
                h.Cell().CeldaHeader().Text("Unidad").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("V. unitario").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("V. total").Bold();
            });
            foreach (var activo in activos)
            {
                tabla.Cell().Celda().Text(activo.Descripcion);
                tabla.Cell().Celda().Text(activo.CategoriaNombre);
                tabla.Cell().Celda().AlignRight().Text(activo.Cantidad.ToString("N2"));
                tabla.Cell().Celda().Text(activo.UnidadMedida);
                tabla.Cell().Celda().AlignRight().Text(activo.ValorUnitarioDisplay);
                tabla.Cell().Celda().AlignRight().Text(activo.ValorTotalDisplay);
            }
        });

        if (totales is null)
            return;

        col.Item().PaddingTop(6).Table(tabla =>
        {
            tabla.ColumnsDefinition(c => { c.RelativeColumn(2.4f); c.RelativeColumn(0.8f); c.RelativeColumn(1.2f); });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Total por categoría").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Ítems").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Subtotal").Bold();
            });
            foreach (var cat in totales.PorCategoria)
            {
                tabla.Cell().Celda().Text(cat.CategoriaNombre);
                tabla.Cell().Celda().AlignRight().Text(cat.CantidadItems.ToString("N0"));
                tabla.Cell().Celda().AlignRight().Text(cat.SubtotalValorTotal.ToString("C2"));
            }
            tabla.Cell().Celda().Text("TOTAL GENERAL").Bold();
            tabla.Cell().Celda().Text(string.Empty);
            tabla.Cell().Celda().AlignRight().Text(totales.TotalGeneral.ToString("C2")).Bold();
        });
    }

    public static void Depreciacion(ColumnDescriptor col, MatrizDepreciacionDto depreciacion)
    {
        EstilosPdf.TituloSeccion(col, "Depreciación de activos fijos");
        if (depreciacion.Filas.Count == 0)
        {
            col.Item().Text("No existen datos suficientes para generar esta sección.")
                .FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            return;
        }

        col.Item().Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(2.6f);
                c.RelativeColumn(1.4f);
                c.RelativeColumn(1.1f);
                c.RelativeColumn(1.1f);
                c.RelativeColumn(0.9f);
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Activo").Bold();
                h.Cell().CeldaHeader().Text("Categoría").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("V. inicial").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("V. residual").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Vida útil").Bold();
            });
            foreach (var fila in depreciacion.Filas)
            {
                tabla.Cell().Celda().Text(fila.Descripcion);
                tabla.Cell().Celda().Text(fila.CategoriaNombre);
                tabla.Cell().Celda().AlignRight().Text(fila.ValorInicialDisplay);
                tabla.Cell().Celda().AlignRight().Text(fila.ValorResidualDisplay);
                tabla.Cell().Celda().AlignRight().Text($"{fila.VidaUtilAnios} años");
            }
        });

        if (depreciacion.TotalesPorPeriodo.Count == 0)
            return;

        col.Item().PaddingTop(6).Table(tabla =>
        {
            tabla.ColumnsDefinition(c => { c.RelativeColumn(1.6f); c.RelativeColumn(1.2f); c.RelativeColumn(1.2f); });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Período").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Depreciación del período").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Depreciación acumulada").Bold();
            });
            foreach (var total in depreciacion.TotalesPorPeriodo)
            {
                tabla.Cell().Celda().Text(total.Etiqueta);
                tabla.Cell().Celda().AlignRight().Text(total.DepreciacionPeriodoDisplay);
                tabla.Cell().Celda().AlignRight().Text(total.DepreciacionAcumuladaDisplay);
            }
        });
    }

    // ============ Talento Humano ============

    public static void Sueldos(ColumnDescriptor col, ResumenSueldosVistaDto sueldos)
    {
        EstilosPdf.TituloSeccion(col, "Sueldos del personal docente y administrativo (por período)");
        if (sueldos.Filas.Count == 0 || sueldos.Periodos.Count == 0)
        {
            col.Item().Text("No existen datos suficientes para generar esta sección.")
                .FontSize(8).Italic().FontColor(EstilosPdf.ColorGris);
            return;
        }

        var etiquetas = sueldos.Periodos.Select(p => p.EtiquetaPeriodo).ToList();
        var filas = sueldos.Filas
            .Select(f => (
                Concepto: f.NombreCargo,
                Periodos: (IReadOnlyList<string>)f.ValoresPorPeriodo.Select(v => v.ToString("C2")).ToList(),
                Total: f.TotalFila.ToString("C2"),
                EsSeccion: false,
                EsTotal: false))
            .ToList();
        filas.Add((
            "TOTAL",
            sueldos.TotalesPorPeriodo.Select(v => v.ToString("C2")).ToList(),
            sueldos.GranTotal.ToString("C2"),
            false,
            true));

        Matriz(col, string.Empty, etiquetas, filas, advertencia: null);

        if (sueldos.PlantaCentralDistribucion is not null)
        {
            col.Item().PaddingTop(4).Text(
                $"Total sueldos + aporte a Planta Central: {sueldos.TotalSueldosMasPlantaCentral:C2}")
                .FontSize(8).Bold().FontColor(EstilosPdf.ColorPrimario);
        }
    }

    public static void PlantaCentral(ColumnDescriptor col, AportePlantaCentralCarreraDto aporte)
    {
        EstilosPdf.TituloSeccion(col, "Gasto administrativo / aporte a Planta Central");
        col.Item().Row(row =>
        {
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Aporte acumulado").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(aporte.AporteAcumuladoDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Aporte promedio anual").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(aporte.AportePromedioAnualDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("% promedio sobre total anual").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(aporte.PorcentajePromedioDisplay).Bold();
            });
        });

        if (aporte.Periodos.Count == 0)
            return;

        col.Item().PaddingTop(6).Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.6f);
                c.RelativeColumn();
                c.RelativeColumn(1.2f);
                c.RelativeColumn();
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Período").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Alumnos carrera").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Aporte semestral").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("% sobre total anual").Bold();
            });
            foreach (var periodo in aporte.Periodos)
            {
                tabla.Cell().Celda().Text(periodo.Etiqueta);
                tabla.Cell().Celda().AlignRight().Text(periodo.AlumnosCarrera.ToString("N0"));
                tabla.Cell().Celda().AlignRight().Text(periodo.AporteSemestralDisplay);
                tabla.Cell().Celda().AlignRight().Text(periodo.PorcentajeDisplay);
            }
        });
    }

    // ============ Mantenimiento ============

    public static void Mantenimiento(ColumnDescriptor col, ResumenMantenimientoDto mantenimiento)
    {
        EstilosPdf.TituloSeccion(col, "Mantenimiento de edificio y servicios básicos");
        col.Item().Row(row =>
        {
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Servicios básicos (anual)").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(mantenimiento.TotalServiciosDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Mantenimiento (anual)").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(mantenimiento.TotalMantenimientoDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Total general (anual)").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(mantenimiento.TotalGeneralDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
        });

        if (mantenimiento.Proyeccion.Count == 0)
            return;

        col.Item().PaddingTop(6).Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.RelativeColumn(1.4f);
                c.RelativeColumn();
                c.RelativeColumn();
                c.RelativeColumn(1.1f);
                c.RelativeColumn(1.1f);
                c.RelativeColumn(1.1f);
            });
            tabla.Header(h =>
            {
                h.Cell().CeldaHeader().Text("Período").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Demanda").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("F. inflación").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Serv. básicos").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Mantenimiento").Bold();
                h.Cell().CeldaHeader().AlignRight().Text("Total").Bold();
            });
            foreach (var periodo in mantenimiento.Proyeccion)
            {
                tabla.Cell().Celda().Text(periodo.Etiqueta);
                tabla.Cell().Celda().AlignRight().Text(periodo.DemandaDisplay);
                tabla.Cell().Celda().AlignRight().Text(periodo.FactorInflacionDisplay);
                tabla.Cell().Celda().AlignRight().Text(periodo.CostoServiciosBasicosDisplay);
                tabla.Cell().Celda().AlignRight().Text(periodo.CostoMantenimientoDisplay);
                tabla.Cell().Celda().AlignRight().Text(periodo.CostoTotalDisplay);
            }
        });
    }

    // ============ CES ============

    public static void CuadrosCes(ColumnDescriptor col, CesDto ces)
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
            foreach (var fila in ces.InfCes)
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
                c.Item().Text("Arancel por semestre (óptimo VAN=0)").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(ces.ArancelPorSemestreDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Costo por Semestre").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(ces.CostoPorSemestreDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.Spacing(6);
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Matrícula").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(ces.MatriculaDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
            });
            row.RelativeItem().CeldaSeccion().Column(c =>
            {
                c.Item().Text("Total por semestre").FontSize(8).FontColor(EstilosPdf.ColorGris);
                c.Item().Text(ces.TotalPorSemestreDisplay).Bold().FontColor(EstilosPdf.ColorPrimario);
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
            foreach (var fila in ces.Parametros)
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
            foreach (var fila in ces.Distribucion)
            {
                var resaltada = fila.EsTotal || fila.EsReferencial;
                tabla.Cell().Celda().Text(t => { var s = t.Span(fila.Categoria); if (resaltada) s.Bold(); });
                tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.MontoDisplay); if (resaltada) s.Bold(); });
                tabla.Cell().Celda().AlignRight().Text(t => { var s = t.Span(fila.PorcentajeDisplay); if (resaltada) s.Bold(); });
            }
        });

        EstilosPdf.Advertencia(col, ces.MensajeAdvertencia);
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
