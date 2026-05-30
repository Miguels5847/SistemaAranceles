namespace SistemaAranceles.Application.Services.Financieros;

/// <summary>
/// Valor Actual Neto (VAN) de una serie de flujos por período.
/// El período 0 no se descuenta; el período t se descuenta por (1 + tasa)^t.
/// </summary>
public static class CalculadoraVAN
{
    /// <summary>VAN redondeado a 2 decimales. <paramref name="tasaDescuento"/> es fracción (0.15 = 15%).</summary>
    public static decimal Calcular(IReadOnlyList<decimal> flujos, decimal tasaDescuento)
        => decimal.Round((decimal)ValorActualNeto(flujos, (double)tasaDescuento), 2);

    /// <summary>Núcleo en double para iteración robusta (lo usa <see cref="CalculadoraTIR"/>).</summary>
    internal static double ValorActualNeto(IReadOnlyList<decimal> flujos, double tasa)
    {
        if (flujos is null || flujos.Count == 0)
            return 0d;

        // Protege contra (1 + tasa) <= 0 que haría inválida la potencia.
        var baseFactor = 1d + tasa;
        if (baseFactor <= 0d)
            baseFactor = 1e-6d;

        var npv = 0d;
        for (var t = 0; t < flujos.Count; t++)
        {
            var flujo = (double)flujos[t];
            npv += t == 0 ? flujo : flujo / Math.Pow(baseFactor, t);
        }

        return npv;
    }

    /// <summary>Factor de descuento (1 + tasa)^t. Período 0 = 1.</summary>
    public static decimal FactorDescuento(decimal tasaDescuento, int periodo)
        => periodo <= 0
            ? 1m
            : decimal.Round((decimal)Math.Pow(1d + (double)tasaDescuento, periodo), 6);

    /// <summary>Valor presente de un flujo individual en el período indicado.</summary>
    public static decimal ValorPresente(decimal flujo, decimal tasaDescuento, int periodo)
    {
        if (periodo <= 0)
            return decimal.Round(flujo, 2);

        var vp = (double)flujo / Math.Pow(1d + (double)tasaDescuento, periodo);
        return decimal.Round((decimal)vp, 2);
    }
}
