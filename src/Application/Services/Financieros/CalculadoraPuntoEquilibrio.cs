namespace SistemaAranceles.Application.Services.Financieros;

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
