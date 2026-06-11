using ClosedXML.Excel;
using SistemaAranceles.Application.Comun;
using SistemaAranceles.Application.DTOs.Reportes;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Infrastructure.Export;

/// <summary>
/// Exportación XLSX del reporte por dirección (KAN-48) con ClosedXML: una hoja
/// por sección presente, espejo de las hojas del Excel original. Reutiliza los
/// mismos DTOs del PDF; las secciones sin datos simplemente no generan hoja.
/// </summary>
public sealed class ServicioExportacionXlsxClosedXml : IServicioExportacionXlsx
{
    private const string FormatoMoneda = "$ #,##0.00";
    private const string FormatoNumero = "#,##0.00";

    public byte[] GenerarReporteDireccion(ReporteDireccionDatos datos)
    {
        using var libro = new XLWorkbook();
        ComponerPortada(libro, datos);

        foreach (var seccion in SeccionesReporte.ParaDireccion(datos.Direccion))
            ComponerSeccion(libro, datos, seccion);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static void ComponerSeccion(XLWorkbook libro, ReporteDireccionDatos datos, SeccionReporte seccion)
    {
        switch (seccion)
        {
            case SeccionReporte.ArancelMatricula when datos.Arancel is not null:
                HojaPares(libro, "Arancel",
                [
                    ("Arancel efectivo", datos.Arancel.ArancelDisplay),
                    ("Matrícula efectiva", datos.Arancel.MatriculaDisplay),
                    ("Total por semestre (arancel + matrícula)",
                        $"$ {(datos.Arancel.ArancelEfectivo ?? 0m) + datos.Arancel.MatriculaEfectiva:N2}"),
                    ("Fuente del arancel", datos.Arancel.FuenteCalculo)
                ]);
                break;

            case SeccionReporte.DemandaTabla when datos.Demanda is { TieneDatos: true }:
                HojaMatriz(libro, "Demanda", "Ciclo", datos.Demanda.EtiquetasPeriodos,
                    datos.Demanda.Filas
                        .Select(f => (f.CicloDisplay, f.Periodos, (decimal?)f.Total, false))
                        .Append(("TOTAL", datos.Demanda.TotalesPorPeriodo, datos.Demanda.TotalGeneral, true))
                        .ToList(),
                    FormatoNumero);
                break;

            case SeccionReporte.DocentesTabla when datos.Demanda is { TieneDocentes: true }:
                HojaMatriz(libro, "Docentes", "Tipo", datos.Demanda.EtiquetasPeriodos,
                    datos.Demanda.DocentesPorPeriodo
                        .Select(f => (f.Tipo, f.Periodos, (decimal?)f.Total, false))
                        .ToList(),
                    FormatoNumero);
                break;

            case SeccionReporte.Ingresos when datos.Ingresos is not null:
                ComponerIngresos(libro, datos.Ingresos);
                break;

            case SeccionReporte.MaterialesUnidades when datos.Materiales is { Cantidades.Count: > 0 }:
                ComponerMateriales(libro, "Materiales (unidades)",
                    datos.Materiales.Cantidades.Select(c => (c.Categoria, c.Concepto, c.Anio, c.NumeroPeriodo, c.EtiquetaPeriodo, c.Cantidad)).ToList(),
                    FormatoNumero);
                break;

            case SeccionReporte.MaterialesMonetario when datos.Materiales is { Monetarios.Count: > 0 }:
                ComponerMateriales(libro, "Materiales (valores)",
                    datos.Materiales.Monetarios.Select(c => (c.Categoria, c.Concepto, c.Anio, c.NumeroPeriodo, c.EtiquetaPeriodo, c.Costo)).ToList(),
                    FormatoMoneda);
                break;

            case SeccionReporte.ActivosFijos when datos.ActivosFijos is { Count: > 0 }:
                ComponerActivos(libro, datos);
                break;

            case SeccionReporte.InversionInicial when datos.Inversion is not null:
                HojaPares(libro, "Inversión Inicial",
                    new List<(string, object)>
                    {
                        ("Activos Diferidos", datos.Inversion.ActivosDiferidos),
                    }
                    .Concat(datos.Inversion.CategoriasActivosFijos.Select(c => (c.CategoriaNombre, (object)c.Valor)))
                    .Concat(
                    [
                        ("Subtotal Activos Fijos", (object)datos.Inversion.SubtotalActivosFijos),
                        ($"Capital de Trabajo ({datos.Inversion.MesesCapitalTrabajo} meses)", datos.Inversion.CapitalTrabajoDosM),
                        ("Imprevistos", datos.Inversion.Imprevistos),
                        ("TOTAL INVERSIÓN", datos.Inversion.TotalInversion)
                    ])
                    .ToList());
                break;

            case SeccionReporte.CapitalTrabajo when datos.CapitalTrabajo is not null:
                HojaPares(libro, "Capital de Trabajo",
                [
                    ("Sueldos y cargos", datos.CapitalTrabajo.SubtotalCargos),
                    ("Materiales de oficina", datos.CapitalTrabajo.SubtotalMateriales),
                    ("Aseo y limpieza", datos.CapitalTrabajo.SubtotalAseo),
                    ("Accesorios", datos.CapitalTrabajo.SubtotalAccesorios),
                    ("Total mensual", datos.CapitalTrabajo.TotalMensual),
                    ($"Total ({datos.CapitalTrabajo.MesesCapitalTrabajo} meses)", datos.CapitalTrabajo.TotalCapitalTrabajo)
                ]);
                break;

            case SeccionReporte.Depreciacion when datos.Depreciacion is { TotalesPorPeriodo.Count: > 0 }:
                ComponerDepreciacion(libro, datos);
                break;

            case SeccionReporte.Sueldos when datos.Sueldos is { Filas.Count: > 0 }:
                HojaMatriz(libro, "Sueldos", "Cargo",
                    datos.Sueldos.Periodos.Select(p => p.EtiquetaPeriodo).ToList(),
                    datos.Sueldos.Filas
                        .Select(f => (f.NombreCargo, (IReadOnlyList<decimal>)f.ValoresPorPeriodo, (decimal?)f.TotalFila, false))
                        .Append(("TOTAL", datos.Sueldos.TotalesPorPeriodo, datos.Sueldos.GranTotal, true))
                        .ToList(),
                    FormatoMoneda);
                break;

            case SeccionReporte.Mantenimiento when datos.Mantenimiento is not null:
                ComponerMantenimiento(libro, datos);
                break;

            case SeccionReporte.PlantaCentral when datos.PlantaCentral is { Periodos.Count: > 0 }:
                ComponerPlantaCentral(libro, datos);
                break;

            case SeccionReporte.InvVinBecas when datos.InvVinBecas is not null:
                HojaMatriz(libro, "Inv Vin Becas", "Concepto", datos.InvVinBecas.EtiquetasPeriodos,
                    datos.InvVinBecas.Filas
                        .Select(f => (f.Concepto, f.Periodos, (decimal?)f.Total, f.EsTotal || f.EsEncabezadoGrupo))
                        .ToList(),
                    FormatoMoneda);
                break;

            case SeccionReporte.CostosGastos when datos.CostosGastos is not null:
                HojaMatriz(libro, "Costos y Gastos", "Concepto", datos.CostosGastos.EtiquetasPeriodos,
                    datos.CostosGastos.ProyeccionCostosGastos
                        .Select(f => (f.Concepto, f.Periodos, (decimal?)f.Total, f.EsTotal || f.EsEncabezadoGrupo))
                        .ToList(),
                    FormatoMoneda);
                break;

            case SeccionReporte.FinanciamientoAmortizacion when datos.Financiamiento is not null:
                ComponerFinanciamiento(libro, datos);
                break;

            case SeccionReporte.Indicadores when datos.Indicadores is not null:
                HojaPares(libro, "Indicadores",
                [
                    ("TMR (tasa mínima de rendimiento)", datos.Indicadores.TmrDisplay),
                    ("VAN", datos.Indicadores.VanDisplay),
                    ("TIR", datos.Indicadores.TirDisplay),
                    ("Estado de viabilidad", datos.Indicadores.EstadoViabilidad)
                ]);
                break;

            case SeccionReporte.PuntoEquilibrio when datos.PuntoEquilibrio is { TieneResumenExcel: true }:
                ComponerPuntoEquilibrio(libro, datos);
                break;

            case SeccionReporte.FlujoFondos when datos.FlujoFondos is not null:
                HojaMatriz(libro, "Flujo de Fondos", "Concepto", datos.FlujoFondos.EtiquetasPeriodos,
                    datos.FlujoFondos.Filas
                        .Select(f => (f.Concepto, f.Periodos, (decimal?)f.Total, f.EsTotal || f.EsSeccion))
                        .ToList(),
                    FormatoMoneda);
                break;

            case SeccionReporte.EstadoResultados when datos.EstadoResultados is not null:
                HojaMatriz(libro, "Pérdidas y Ganancias", "Concepto", datos.EstadoResultados.EtiquetasPeriodos,
                    datos.EstadoResultados.Filas
                        .Select(f => (f.Concepto, f.Periodos, (decimal?)f.Total, f.EsTotal || f.EsSeccion || f.EsResultado))
                        .ToList(),
                    FormatoMoneda);
                break;

            case SeccionReporte.BalanceProyectado when datos.BalanceProyectado is { TieneDatos: true }:
                HojaMatriz(libro, "Balance", "Concepto", datos.BalanceProyectado.EtiquetasPeriodos,
                    datos.BalanceProyectado.Filas
                        .Select(f => (f.Concepto, f.Periodos, (decimal?)null, f.EsTotal || f.EsSeccion || f.EsResultado))
                        .ToList(),
                    FormatoMoneda);
                break;

            case SeccionReporte.Ces when datos.Ces is not null:
                ComponerCes(libro, datos);
                break;

            default:
                // Sección sin datos: no genera hoja (el PDF es el formato que anota la ausencia).
                break;
        }
    }

    // ============ hojas genéricas ============

    private static void ComponerPortada(XLWorkbook libro, ReporteDireccionDatos datos)
    {
        var hoja = libro.AddWorksheet("Reporte");
        hoja.Cell(1, 1).Value = $"Reporte — {SeccionesReporte.Titulo(datos.Direccion)}";
        hoja.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14);
        hoja.Cell(2, 1).Value = $"Carrera: {datos.CarreraNombre}";
        hoja.Cell(3, 1).Value = $"Escenario: {datos.EscenarioNombre}";
        hoja.Cell(4, 1).Value = $"Generado: {datos.GeneradoEn:dd/MM/yyyy HH:mm} — Sistema de Aranceles Universitarios";
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static IXLWorksheet NuevaHoja(XLWorkbook libro, string nombre)
    {
        var limpio = nombre.Length > 31 ? nombre[..31] : nombre;
        var unico = limpio;
        var sufijo = 2;
        while (libro.Worksheets.Any(w => string.Equals(w.Name, unico, StringComparison.OrdinalIgnoreCase)))
            unico = $"{(limpio.Length > 28 ? limpio[..28] : limpio)} {sufijo++}";
        return libro.AddWorksheet(unico);
    }

    private static void HojaPares(XLWorkbook libro, string nombre, IReadOnlyList<(string Concepto, object Valor)> pares)
    {
        var hoja = NuevaHoja(libro, nombre);
        hoja.Cell(1, 1).Value = "Concepto";
        hoja.Cell(1, 2).Value = "Valor";
        EstiloHeader(hoja.Range(1, 1, 1, 2));

        for (var i = 0; i < pares.Count; i++)
        {
            hoja.Cell(i + 2, 1).Value = pares[i].Concepto;
            var celda = hoja.Cell(i + 2, 2);
            if (pares[i].Valor is decimal valor)
            {
                celda.Value = valor;
                celda.Style.NumberFormat.Format = FormatoMoneda;
            }
            else
            {
                celda.Value = pares[i].Valor?.ToString() ?? string.Empty;
            }
        }
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static void HojaMatriz(
        XLWorkbook libro,
        string nombre,
        string tituloConcepto,
        IReadOnlyList<string> etiquetas,
        IReadOnlyList<(string Concepto, IReadOnlyList<decimal> Periodos, decimal? Total, bool Resaltada)> filas,
        string formato)
    {
        var hoja = NuevaHoja(libro, nombre);
        var tieneTotal = filas.Any(f => f.Total is not null);

        hoja.Cell(1, 1).Value = tituloConcepto;
        for (var c = 0; c < etiquetas.Count; c++)
            hoja.Cell(1, c + 2).Value = etiquetas[c];
        if (tieneTotal)
            hoja.Cell(1, etiquetas.Count + 2).Value = "Total";
        EstiloHeader(hoja.Range(1, 1, 1, etiquetas.Count + (tieneTotal ? 2 : 1)));

        for (var f = 0; f < filas.Count; f++)
        {
            var fila = filas[f];
            var filaXl = f + 2;
            hoja.Cell(filaXl, 1).Value = fila.Concepto;
            // Sin períodos = fila-banda (encabezado de grupo): las celdas quedan vacías.
            for (var c = 0; c < fila.Periodos.Count && c < etiquetas.Count; c++)
            {
                var celda = hoja.Cell(filaXl, c + 2);
                celda.Value = fila.Periodos[c];
                celda.Style.NumberFormat.Format = formato;
            }
            if (fila.Total is decimal total)
            {
                var celdaTotal = hoja.Cell(filaXl, etiquetas.Count + 2);
                celdaTotal.Value = total;
                celdaTotal.Style.NumberFormat.Format = formato;
            }
            if (fila.Resaltada)
                hoja.Row(filaXl).Style.Font.SetBold();
        }
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static void EstiloHeader(IXLRange rango)
    {
        rango.Style.Font.SetBold();
        rango.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E8EAF6"));
    }

    // ============ hojas específicas ============

    private static void ComponerIngresos(XLWorkbook libro, Application.DTOs.DemandaIngresos.IngresosProyectadosDto ingresos)
    {
        var hoja = NuevaHoja(libro, "Ingresos");
        hoja.Cell(1, 1).Value = "Ciclo";
        hoja.Cell(1, 2).Value = "Arancel base";
        hoja.Cell(1, 3).Value = "% descuento";
        hoja.Cell(1, 4).Value = "Arancel a cobrar";
        hoja.Cell(1, 5).Value = "Estudiantes (suma períodos)";
        hoja.Cell(1, 6).Value = "Ingreso bruto";
        hoja.Cell(1, 7).Value = "Becas";
        hoja.Cell(1, 8).Value = "Ingreso neto";
        EstiloHeader(hoja.Range(1, 1, 1, 8));

        var filaXl = 2;
        foreach (var fila in ingresos.Filas)
        {
            var primera = fila.Periodos.FirstOrDefault();
            hoja.Cell(filaXl, 1).Value = fila.CicloDisplay;
            hoja.Cell(filaXl, 2).Value = primera?.ArancelBase ?? 0m;
            hoja.Cell(filaXl, 3).Value = (primera?.PorcentajeDescuentoCiclo ?? 0m) / 100m;
            hoja.Cell(filaXl, 3).Style.NumberFormat.Format = "0.00%";
            hoja.Cell(filaXl, 4).Value = primera?.ArancelCiclo ?? 0m;
            hoja.Cell(filaXl, 5).Value = fila.TotalEstudiantes;
            hoja.Cell(filaXl, 6).Value = fila.TotalBruto;
            hoja.Cell(filaXl, 7).Value = fila.TotalBecas;
            hoja.Cell(filaXl, 8).Value = fila.TotalNeto;
            foreach (var c in new[] { 2, 4, 6, 7, 8 })
                hoja.Cell(filaXl, c).Style.NumberFormat.Format = FormatoMoneda;
            hoja.Cell(filaXl, 5).Style.NumberFormat.Format = FormatoNumero;
            filaXl++;
        }

        hoja.Cell(filaXl, 1).Value = "TOTAL";
        hoja.Cell(filaXl, 6).Value = ingresos.TotalGeneralBruto;
        hoja.Cell(filaXl, 7).Value = ingresos.TotalGeneralBecas;
        hoja.Cell(filaXl, 8).Value = ingresos.TotalGeneralNeto;
        foreach (var c in new[] { 6, 7, 8 })
            hoja.Cell(filaXl, c).Style.NumberFormat.Format = FormatoMoneda;
        hoja.Row(filaXl).Style.Font.SetBold();
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static void ComponerMateriales(
        XLWorkbook libro,
        string nombre,
        IReadOnlyList<(string Categoria, string Concepto, int Anio, int NumeroPeriodo, string Etiqueta, decimal Valor)> celdas,
        string formato)
    {
        var etiquetas = celdas
            .Select(c => (c.Anio, c.NumeroPeriodo, c.Etiqueta))
            .Distinct()
            .OrderBy(x => x.Anio).ThenBy(x => x.NumeroPeriodo)
            .ToList();

        // Agrupado como en pantalla: fila resaltada por categoría legible + sus conceptos (KAN-49).
        var filas = new List<(string Concepto, IReadOnlyList<decimal> Periodos, decimal? Total, bool Resaltada)>();
        foreach (var categoria in celdas
            .GroupBy(c => c.Categoria)
            .OrderBy(g => CategoriaMaterialDisplay.Orden(g.Key)).ThenBy(g => g.Key))
        {
            filas.Add((CategoriaMaterialDisplay.Formatear(categoria.Key), [], null, true));

            foreach (var concepto in categoria.GroupBy(c => c.Concepto).OrderBy(g => g.Key))
            {
                var porPeriodo = concepto.ToDictionary(x => (x.Anio, x.NumeroPeriodo), x => x.Valor);
                var valores = etiquetas
                    .Select(e => porPeriodo.GetValueOrDefault((e.Anio, e.NumeroPeriodo)))
                    .ToList();
                filas.Add((concepto.Key, valores, concepto.Sum(x => x.Valor), false));
            }
        }

        HojaMatriz(libro, nombre, "Concepto", etiquetas.Select(e => e.Etiqueta).ToList(), filas, formato);
    }

    private static void ComponerActivos(XLWorkbook libro, ReporteDireccionDatos datos)
    {
        var hoja = NuevaHoja(libro, "Activos Fijos");
        string[] headers = ["Descripción", "Categoría", "Cantidad", "Unidad", "V. unitario", "V. total"];
        for (var c = 0; c < headers.Length; c++)
            hoja.Cell(1, c + 1).Value = headers[c];
        EstiloHeader(hoja.Range(1, 1, 1, headers.Length));

        var filaXl = 2;
        foreach (var activo in datos.ActivosFijos!)
        {
            hoja.Cell(filaXl, 1).Value = activo.Descripcion;
            hoja.Cell(filaXl, 2).Value = activo.CategoriaNombre;
            hoja.Cell(filaXl, 3).Value = activo.Cantidad;
            hoja.Cell(filaXl, 4).Value = activo.UnidadMedida;
            hoja.Cell(filaXl, 5).Value = activo.ValorUnitario;
            hoja.Cell(filaXl, 6).Value = activo.ValorTotal;
            hoja.Cell(filaXl, 3).Style.NumberFormat.Format = FormatoNumero;
            hoja.Cell(filaXl, 5).Style.NumberFormat.Format = FormatoMoneda;
            hoja.Cell(filaXl, 6).Style.NumberFormat.Format = FormatoMoneda;
            filaXl++;
        }

        if (datos.TotalesActivos is not null)
        {
            filaXl++;
            foreach (var cat in datos.TotalesActivos.PorCategoria)
            {
                hoja.Cell(filaXl, 1).Value = cat.CategoriaNombre;
                hoja.Cell(filaXl, 6).Value = cat.SubtotalValorTotal;
                hoja.Cell(filaXl, 6).Style.NumberFormat.Format = FormatoMoneda;
                filaXl++;
            }
            hoja.Cell(filaXl, 1).Value = "TOTAL GENERAL";
            hoja.Cell(filaXl, 6).Value = datos.TotalesActivos.TotalGeneral;
            hoja.Cell(filaXl, 6).Style.NumberFormat.Format = FormatoMoneda;
            hoja.Row(filaXl).Style.Font.SetBold();
        }
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static void ComponerDepreciacion(XLWorkbook libro, ReporteDireccionDatos datos)
    {
        var hoja = NuevaHoja(libro, "Depreciación");
        string[] headers = ["Período", "Depreciación del período", "Depreciación acumulada"];
        for (var c = 0; c < headers.Length; c++)
            hoja.Cell(1, c + 1).Value = headers[c];
        EstiloHeader(hoja.Range(1, 1, 1, headers.Length));

        var filaXl = 2;
        foreach (var total in datos.Depreciacion!.TotalesPorPeriodo)
        {
            hoja.Cell(filaXl, 1).Value = total.Etiqueta;
            hoja.Cell(filaXl, 2).Value = total.DepreciacionPeriodo;
            hoja.Cell(filaXl, 3).Value = total.DepreciacionAcumulada;
            hoja.Cell(filaXl, 2).Style.NumberFormat.Format = FormatoMoneda;
            hoja.Cell(filaXl, 3).Style.NumberFormat.Format = FormatoMoneda;
            filaXl++;
        }
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static void ComponerMantenimiento(XLWorkbook libro, ReporteDireccionDatos datos)
    {
        var hoja = NuevaHoja(libro, "Mantenimiento");
        string[] headers = ["Período", "Demanda", "Factor inflación", "Servicios básicos", "Mantenimiento", "Total"];
        for (var c = 0; c < headers.Length; c++)
            hoja.Cell(1, c + 1).Value = headers[c];
        EstiloHeader(hoja.Range(1, 1, 1, headers.Length));

        var filaXl = 2;
        foreach (var p in datos.Mantenimiento!.Proyeccion)
        {
            hoja.Cell(filaXl, 1).Value = p.Etiqueta;
            hoja.Cell(filaXl, 2).Value = p.DemandaPeriodo;
            hoja.Cell(filaXl, 3).Value = p.FactorInflacion;
            hoja.Cell(filaXl, 4).Value = p.CostoServiciosBasicos;
            hoja.Cell(filaXl, 5).Value = p.CostoMantenimiento;
            hoja.Cell(filaXl, 6).Value = p.CostoTotal;
            foreach (var c in new[] { 4, 5, 6 })
                hoja.Cell(filaXl, c).Style.NumberFormat.Format = FormatoMoneda;
            filaXl++;
        }
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static void ComponerPlantaCentral(XLWorkbook libro, ReporteDireccionDatos datos)
    {
        var hoja = NuevaHoja(libro, "Planta Central");
        string[] headers = ["Período", "Alumnos carrera", "Aporte semestral", "% sobre total anual"];
        for (var c = 0; c < headers.Length; c++)
            hoja.Cell(1, c + 1).Value = headers[c];
        EstiloHeader(hoja.Range(1, 1, 1, headers.Length));

        var filaXl = 2;
        foreach (var p in datos.PlantaCentral!.Periodos)
        {
            hoja.Cell(filaXl, 1).Value = p.Etiqueta;
            hoja.Cell(filaXl, 2).Value = p.AlumnosCarrera;
            hoja.Cell(filaXl, 3).Value = p.AporteSemestral;
            hoja.Cell(filaXl, 3).Style.NumberFormat.Format = FormatoMoneda;
            hoja.Cell(filaXl, 4).Value = p.PorcentajeSobreTotalAnual;
            hoja.Cell(filaXl, 4).Style.NumberFormat.Format = "0.00%";
            filaXl++;
        }
        hoja.Cell(filaXl, 1).Value = "Aporte acumulado";
        hoja.Cell(filaXl, 3).Value = datos.PlantaCentral.AporteAcumulado;
        hoja.Cell(filaXl, 3).Style.NumberFormat.Format = FormatoMoneda;
        hoja.Row(filaXl).Style.Font.SetBold();
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static void ComponerFinanciamiento(XLWorkbook libro, ReporteDireccionDatos datos)
    {
        var financiamiento = datos.Financiamiento!;
        HojaPares(libro, "Financiamiento",
        [
            ("Recursos Propios", financiamiento.MontoPropio),
            ($"Préstamo Bancario ({financiamiento.NombreEntidadPrestamo})", financiamiento.MontoPrestamo),
            ($"Convenio Institucional ({financiamiento.NombreEntidadConvenio})", financiamiento.MontoConvenio),
            ("TOTAL INVERSIÓN", financiamiento.TotalInversion),
            ("Tasa de interés anual", financiamiento.TasaInteresAnualDisplay),
            ("Plazo (meses)", financiamiento.PlazoMeses.ToString()),
            ("Cuota mensual", financiamiento.CuotaMensual),
            ("Total intereses", financiamiento.TotalIntereses)
        ]);

        if (financiamiento.TablaAmortizacion.Count == 0)
            return;

        var hoja = NuevaHoja(libro, "Amort. préstamo");
        string[] headers = ["N° pago", "Saldo capital", "Interés", "Amortización capital", "Cuota", "Interés acumulado"];
        for (var c = 0; c < headers.Length; c++)
            hoja.Cell(1, c + 1).Value = headers[c];
        EstiloHeader(hoja.Range(1, 1, 1, headers.Length));

        var filaXl = 2;
        foreach (var pago in financiamiento.TablaAmortizacion)
        {
            hoja.Cell(filaXl, 1).Value = pago.NumeroPago;
            hoja.Cell(filaXl, 2).Value = pago.SaldoCapital;
            hoja.Cell(filaXl, 3).Value = pago.Interes;
            hoja.Cell(filaXl, 4).Value = pago.AmortizacionCapital;
            hoja.Cell(filaXl, 5).Value = pago.Cuota;
            hoja.Cell(filaXl, 6).Value = pago.InteresAcumulado;
            for (var c = 2; c <= 6; c++)
                hoja.Cell(filaXl, c).Style.NumberFormat.Format = FormatoMoneda;
            filaXl++;
        }
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static void ComponerPuntoEquilibrio(XLWorkbook libro, ReporteDireccionDatos datos)
    {
        var pe = datos.PuntoEquilibrio!;
        var hoja = NuevaHoja(libro, "Punto de Equilibrio");

        hoja.Cell(1, 1).Value = "Proyección de Resultados y Punto de Equilibrio";
        hoja.Cell(1, 1).Style.Font.SetBold();
        hoja.Cell(2, 1).Value = "Concepto";
        hoja.Cell(2, 2).Value = "Valor anual";
        hoja.Cell(2, 3).Value = "Valor mensual";
        hoja.Cell(2, 4).Value = "%";
        EstiloHeader(hoja.Range(2, 1, 2, 4));

        var filaXl = 3;
        foreach (var fila in pe.ProyeccionResultados)
        {
            hoja.Cell(filaXl, 1).Value = fila.Concepto;
            hoja.Cell(filaXl, 2).Value = fila.ValorAnualDisplay;
            hoja.Cell(filaXl, 3).Value = fila.ValorMensualDisplay;
            hoja.Cell(filaXl, 4).Value = fila.PorcentajeDisplay;
            if (fila.EsTotal || fila.EsResultado)
                hoja.Row(filaXl).Style.Font.SetBold();
            filaXl++;
        }

        filaXl++;
        hoja.Cell(filaXl, 1).Value = "Análisis del Punto de Equilibrio";
        hoja.Cell(filaXl, 1).Style.Font.SetBold();
        filaXl++;
        hoja.Cell(filaXl, 1).Value = "Concepto";
        hoja.Cell(filaXl, 2).Value = "PE Anual";
        hoja.Cell(filaXl, 3).Value = "PE Mensual";
        EstiloHeader(hoja.Range(filaXl, 1, filaXl, 3));
        filaXl++;
        foreach (var fila in pe.AnalisisPuntoEquilibrio)
        {
            hoja.Cell(filaXl, 1).Value = fila.Concepto;
            hoja.Cell(filaXl, 2).Value = fila.PeAnualDisplay;
            hoja.Cell(filaXl, 3).Value = fila.PeMensualDisplay;
            if (fila.EsTotal || fila.EsResultado)
                hoja.Row(filaXl).Style.Font.SetBold();
            filaXl++;
        }
        hoja.ColumnsUsed().AdjustToContents();
    }

    private static void ComponerCes(XLWorkbook libro, ReporteDireccionDatos datos)
    {
        var ces = datos.Ces!;
        var hoja = NuevaHoja(libro, "INF CES");
        string[] headers = ["Concepto", "Provisión", "Fomento", "Vinculación", "Otros", "Total", "%"];
        for (var c = 0; c < headers.Length; c++)
            hoja.Cell(1, c + 1).Value = headers[c];
        EstiloHeader(hoja.Range(1, 1, 1, headers.Length));

        var filaXl = 2;
        foreach (var fila in ces.InfCes)
        {
            hoja.Cell(filaXl, 1).Value = fila.Concepto;
            hoja.Cell(filaXl, 2).Value = fila.ProvisionDisplay;
            hoja.Cell(filaXl, 3).Value = fila.FomentoDisplay;
            hoja.Cell(filaXl, 4).Value = fila.VinculacionDisplay;
            hoja.Cell(filaXl, 5).Value = fila.OtrosDisplay;
            hoja.Cell(filaXl, 6).Value = fila.TotalDisplay;
            hoja.Cell(filaXl, 7).Value = fila.PorcentajeDisplay;
            if (fila.EsSeccion || fila.EsTotal || fila.EsResultado)
                hoja.Row(filaXl).Style.Font.SetBold();
            filaXl++;
        }

        filaXl++;
        hoja.Cell(filaXl, 1).Value = "Arancel por semestre";
        hoja.Cell(filaXl, 2).Value = ces.ArancelPorSemestreDisplay;
        filaXl++;
        hoja.Cell(filaXl, 1).Value = "Matrícula";
        hoja.Cell(filaXl, 2).Value = ces.MatriculaDisplay;
        filaXl++;
        hoja.Cell(filaXl, 1).Value = "Total por semestre";
        hoja.Cell(filaXl, 2).Value = ces.TotalPorSemestreDisplay;
        hoja.ColumnsUsed().AdjustToContents();

        var hojaParametros = NuevaHoja(libro, "CES Parámetros");
        hojaParametros.Cell(1, 1).Value = "Parámetro";
        hojaParametros.Cell(1, 2).Value = "Criterio";
        hojaParametros.Cell(1, 3).Value = "Valor";
        EstiloHeader(hojaParametros.Range(1, 1, 1, 3));
        var filaP = 2;
        foreach (var fila in ces.Parametros)
        {
            hojaParametros.Cell(filaP, 1).Value = fila.Parametro;
            hojaParametros.Cell(filaP, 2).Value = fila.Criterio;
            hojaParametros.Cell(filaP, 3).Value = fila.ValorDisplay;
            if (fila.EsResultado)
                hojaParametros.Row(filaP).Style.Font.SetBold();
            filaP++;
        }
        filaP++;
        hojaParametros.Cell(filaP, 1).Value = "Distribución referencial del costo de la carrera";
        hojaParametros.Cell(filaP, 1).Style.Font.SetBold();
        filaP++;
        foreach (var fila in ces.Distribucion)
        {
            hojaParametros.Cell(filaP, 1).Value = fila.Categoria;
            hojaParametros.Cell(filaP, 2).Value = fila.MontoDisplay;
            hojaParametros.Cell(filaP, 3).Value = fila.PorcentajeDisplay;
            if (fila.EsTotal || fila.EsReferencial)
                hojaParametros.Row(filaP).Style.Font.SetBold();
            filaP++;
        }
        hojaParametros.ColumnsUsed().AdjustToContents();
    }
}
