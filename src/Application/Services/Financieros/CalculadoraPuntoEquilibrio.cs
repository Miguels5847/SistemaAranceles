using SistemaAranceles.Application.DTOs.CostosGastos;

namespace SistemaAranceles.Application.Services.Financieros;

public sealed class ResumenPuntoEquilibrio
{
    // Bloque A: Proyeccion de Resultados (ultimo periodo, equivalencias hoja "15 Punto de equilibrio")
    public decimal Ingresos { get; init; }
    public decimal CostoServicio { get; init; }
    public decimal MargenBruto { get; init; }
    public decimal GastosPersonal { get; init; }
    public decimal GastosAdminVentas { get; init; }
    public decimal DepreciacionAmortizacion { get; init; }
    public decimal Intereses { get; init; }
    public decimal TotalGastos { get; init; }
    public decimal Beneficio { get; init; }

    // Bloque B: Analisis del Punto de Equilibrio
    public decimal CostoVariable { get; init; }
    public decimal CostoFijo { get; init; }
    public decimal IngresoPromedio { get; init; }
    public decimal CostoVariablePorEstudiante { get; init; }
    public decimal MargenContribucion { get; init; }
    public decimal PuntoEquilibrioEstudiantes { get; init; }
    public int Ciclos { get; init; }
    public decimal EstudiantesPorCiclo { get; init; }
    public decimal EstudiantesPorCicloDesercion35 { get; init; }
    public decimal EstudiantesPorCicloDesercion175 { get; init; }
    public bool EsCalculable { get; init; }
}

public sealed class EntradaPuntoEquilibrioPeriodo
{
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal Estudiantes { get; init; }
    public decimal Ingresos { get; init; }
    public decimal CostosVariables { get; init; }
    public decimal CostosFijos { get; init; }
}

public sealed class ResultadoPuntoEquilibrioPeriodo
{
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal Estudiantes { get; init; }
    public decimal Ingresos { get; init; }
    public decimal CostosVariables { get; init; }
    public decimal CostosFijos { get; init; }
    public decimal IngresoPromedioEstudiante { get; init; }
    public decimal CostoVariablePorEstudiante { get; init; }
    public decimal MargenContribucion { get; init; }
    public decimal PuntoEquilibrioEstudiantes { get; init; }
    public decimal PuntoEquilibrioMonetario { get; init; }
    public bool EsCalculable { get; init; }
    public string Estado { get; init; } = "Sin datos";
}

public static class CalculadoraPuntoEquilibrio
{
    public static ResultadoPuntoEquilibrioPeriodo CalcularPeriodo(EntradaPuntoEquilibrioPeriodo entrada)
    {
        var estudiantes = decimal.Round(entrada.Estudiantes, 2);
        var ingresos = decimal.Round(entrada.Ingresos, 2);
        var costosVariables = decimal.Round(entrada.CostosVariables, 2);
        var costosFijos = decimal.Round(Math.Max(entrada.CostosFijos, 0m), 2);

        if (estudiantes <= 0m)
            return CrearNoCalculable(entrada, estudiantes, ingresos, costosVariables, costosFijos, "Sin estudiantes");

        var ingresoPromedio = decimal.Round(ingresos / estudiantes, 4);
        var costoVariableEstudiante = decimal.Round(costosVariables / estudiantes, 4);
        var margen = decimal.Round(ingresoPromedio - costoVariableEstudiante, 4);

        if (margen <= 0m)
        {
            return new ResultadoPuntoEquilibrioPeriodo
            {
                PeriodoAcademicoId = entrada.PeriodoAcademicoId,
                Anio = entrada.Anio,
                NumeroPeriodo = entrada.NumeroPeriodo,
                EtiquetaPeriodo = entrada.EtiquetaPeriodo,
                Estudiantes = estudiantes,
                Ingresos = ingresos,
                CostosVariables = costosVariables,
                CostosFijos = costosFijos,
                IngresoPromedioEstudiante = ingresoPromedio,
                CostoVariablePorEstudiante = costoVariableEstudiante,
                MargenContribucion = margen,
                Estado = "Punto de equilibrio no calculable"
            };
        }

        var puntoEstudiantes = decimal.Round(costosFijos / margen, 2);
        var puntoMonetario = decimal.Round(puntoEstudiantes * ingresoPromedio, 2);

        return new ResultadoPuntoEquilibrioPeriodo
        {
            PeriodoAcademicoId = entrada.PeriodoAcademicoId,
            Anio = entrada.Anio,
            NumeroPeriodo = entrada.NumeroPeriodo,
            EtiquetaPeriodo = entrada.EtiquetaPeriodo,
            Estudiantes = estudiantes,
            Ingresos = ingresos,
            CostosVariables = costosVariables,
            CostosFijos = costosFijos,
            IngresoPromedioEstudiante = ingresoPromedio,
            CostoVariablePorEstudiante = costoVariableEstudiante,
            MargenContribucion = margen,
            PuntoEquilibrioEstudiantes = puntoEstudiantes,
            PuntoEquilibrioMonetario = puntoMonetario,
            EsCalculable = true,
            Estado = "Calculado"
        };
    }

    public static IReadOnlyList<ResultadoPuntoEquilibrioPeriodo> Calcular(
        IReadOnlyList<EntradaPuntoEquilibrioPeriodo> entradas)
        => entradas.Select(CalcularPeriodo).ToList();

    // Resumen estilo Excel "15 Punto de equilibrio" sobre el ultimo periodo proyectado.
    public static ResumenPuntoEquilibrio CalcularResumen(
        CostoGastoPeriodoDto costo,
        decimal ingresos,
        decimal estudiantes,
        int ciclos)
    {
        ingresos = decimal.Round(ingresos, 2);

        // Costo por servicio = 9 rubros del Excel (B6).
        var costoServicio = decimal.Round(
            costo.MantenimientoEdificio
            + costo.CapacitacionDocente
            + costo.Internacionalizacion
            + costo.CostoSeguroEstudiantil
            + costo.BecasInstitucionales
            + costo.Investigacion
            + costo.Vinculacion
            + costo.MaterialesSuministros
            + costo.ServiciosBasicos,
            2);

        var margenBruto = decimal.Round(ingresos - costoServicio, 2);

        // Gastos de Personal = rubros docentes (B10).
        var gastosPersonal = decimal.Round(
            costo.TiempoCompletoPhd
            + costo.TiempoCompletoMgs
            + costo.MedioTiempo
            + costo.TiempoParcial
            + costo.OcasionalTipo2TecnicoDocente,
            2);

        var gastosAdminVentas = decimal.Round(costo.GastosAdministracion + costo.GastosVentas, 2);
        var depreciacionAmortizacion = decimal.Round(costo.Depreciacion + costo.Amortizacion, 2);
        var intereses = decimal.Round(costo.Interes, 2);

        // Total Gastos (B15) = SUM(B10:B14), con Depreciacion y Amortizacion en negativo.
        var totalGastos = decimal.Round(gastosPersonal + gastosAdminVentas - depreciacionAmortizacion + intereses, 2);
        var beneficio = decimal.Round(margenBruto - totalGastos, 2);

        var costoVariable = costoServicio;
        var costoFijo = totalGastos;
        var ciclosBase = ciclos > 0 ? ciclos : 0;

        if (estudiantes <= 0m)
        {
            return new ResumenPuntoEquilibrio
            {
                Ingresos = ingresos,
                CostoServicio = costoServicio,
                MargenBruto = margenBruto,
                GastosPersonal = gastosPersonal,
                GastosAdminVentas = gastosAdminVentas,
                DepreciacionAmortizacion = depreciacionAmortizacion,
                Intereses = intereses,
                TotalGastos = totalGastos,
                Beneficio = beneficio,
                CostoVariable = costoVariable,
                CostoFijo = costoFijo,
                Ciclos = ciclosBase,
                EsCalculable = false
            };
        }

        var ingresoPromedio = decimal.Round(ingresos / estudiantes, 2);
        var costoVariablePorEstudiante = decimal.Round(costoVariable / estudiantes, 2);
        var margenContribucion = decimal.Round(ingresoPromedio - costoVariablePorEstudiante, 2);

        var esCalculable = margenContribucion > 0m;
        var puntoEquilibrioEstudiantes = esCalculable
            ? decimal.Round(costoFijo / margenContribucion, 2)
            : 0m;
        var estudiantesPorCiclo = esCalculable && ciclosBase > 0
            ? decimal.Round(puntoEquilibrioEstudiantes / ciclosBase, 2)
            : 0m;

        return new ResumenPuntoEquilibrio
        {
            Ingresos = ingresos,
            CostoServicio = costoServicio,
            MargenBruto = margenBruto,
            GastosPersonal = gastosPersonal,
            GastosAdminVentas = gastosAdminVentas,
            DepreciacionAmortizacion = depreciacionAmortizacion,
            Intereses = intereses,
            TotalGastos = totalGastos,
            Beneficio = beneficio,
            CostoVariable = costoVariable,
            CostoFijo = costoFijo,
            IngresoPromedio = ingresoPromedio,
            CostoVariablePorEstudiante = costoVariablePorEstudiante,
            MargenContribucion = margenContribucion,
            PuntoEquilibrioEstudiantes = puntoEquilibrioEstudiantes,
            Ciclos = ciclosBase,
            EstudiantesPorCiclo = estudiantesPorCiclo,
            EstudiantesPorCicloDesercion35 = decimal.Round(estudiantesPorCiclo * 1.35m, 2),
            EstudiantesPorCicloDesercion175 = decimal.Round(estudiantesPorCiclo * 1.175m, 2),
            EsCalculable = esCalculable
        };
    }

    private static ResultadoPuntoEquilibrioPeriodo CrearNoCalculable(
        EntradaPuntoEquilibrioPeriodo entrada,
        decimal estudiantes,
        decimal ingresos,
        decimal costosVariables,
        decimal costosFijos,
        string estado)
        => new()
        {
            PeriodoAcademicoId = entrada.PeriodoAcademicoId,
            Anio = entrada.Anio,
            NumeroPeriodo = entrada.NumeroPeriodo,
            EtiquetaPeriodo = entrada.EtiquetaPeriodo,
            Estudiantes = estudiantes,
            Ingresos = ingresos,
            CostosVariables = costosVariables,
            CostosFijos = costosFijos,
            Estado = estado
        };
}
