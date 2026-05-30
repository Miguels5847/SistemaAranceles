namespace SistemaAranceles.Application.Services.Financieros;

public sealed class ParametrosTmr
{
    public decimal TasaInteresFinancieraPorcentaje { get; init; }
    public decimal InflacionPromedioPorcentaje { get; init; }
    public decimal PremioRiesgoPorcentaje { get; init; }
    public bool UsarTmrManual { get; init; }
    public decimal TmrManualPorcentaje { get; init; }
}

public sealed class ResultadoTmr
{
    public bool EsManual { get; init; }
    /// <summary>TMR en porcentaje (16.5 = 16.5%).</summary>
    public decimal TmrPorcentaje { get; init; }
    /// <summary>TMR como fracción (0.165) lista para descontar el VAN.</summary>
    public decimal TmrTasa => decimal.Round(TmrPorcentaje / 100m, 6);

    public decimal TasaInteresFinancieraPorcentaje { get; init; }
    public decimal InflacionPromedioPorcentaje { get; init; }
    public decimal PremioRiesgoPorcentaje { get; init; }
}

/// <summary>
/// Tasa Mínima de Rendimiento (TMR). Encapsulada para poder cambiar la fórmula
/// si el docente confirma otra: sólo se ajusta <see cref="CalcularFormula"/>.
///
/// Si hay TMR manual activa se usa ese valor; si no, se calcula con
/// tasa de interés + inflación promedio + premio al riesgo.
/// La inflación promedio proviene del módulo Inflación, nunca se escribe aquí.
/// </summary>
public static class CalculadoraTMR
{
    public static ResultadoTmr Calcular(ParametrosTmr parametros)
    {
        if (parametros.UsarTmrManual)
        {
            return new ResultadoTmr
            {
                EsManual = true,
                TmrPorcentaje = decimal.Round(parametros.TmrManualPorcentaje, 4),
                TasaInteresFinancieraPorcentaje = parametros.TasaInteresFinancieraPorcentaje,
                InflacionPromedioPorcentaje = parametros.InflacionPromedioPorcentaje,
                PremioRiesgoPorcentaje = parametros.PremioRiesgoPorcentaje
            };
        }

        var tmr = CalcularFormula(
            parametros.TasaInteresFinancieraPorcentaje,
            parametros.InflacionPromedioPorcentaje,
            parametros.PremioRiesgoPorcentaje);

        return new ResultadoTmr
        {
            EsManual = false,
            TmrPorcentaje = tmr,
            TasaInteresFinancieraPorcentaje = parametros.TasaInteresFinancieraPorcentaje,
            InflacionPromedioPorcentaje = parametros.InflacionPromedioPorcentaje,
            PremioRiesgoPorcentaje = parametros.PremioRiesgoPorcentaje
        };
    }

    /// <summary>
    /// Fórmula inicial: TMR = tasaInterés + inflaciónPromedio + premioRiesgo (todos en %).
    /// Punto único de cambio si se confirma otra fórmula.
    /// </summary>
    public static decimal CalcularFormula(
        decimal tasaInteresPorcentaje,
        decimal inflacionPromedioPorcentaje,
        decimal premioRiesgoPorcentaje)
        => decimal.Round(tasaInteresPorcentaje + inflacionPromedioPorcentaje + premioRiesgoPorcentaje, 4);
}
