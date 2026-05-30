namespace SistemaAranceles.Application.Services.Financieros;

public sealed class ResultadoTir
{
    public bool EsCalculable { get; init; }
    /// <summary>TIR como fracción (0.18 = 18%). Sólo válida si <see cref="EsCalculable"/>.</summary>
    public decimal Tir { get; init; }
    public int Iteraciones { get; init; }
    public string? Mensaje { get; init; }
}

/// <summary>
/// Tasa Interna de Retorno sin depender de Excel.
/// Estrategia robusta: búsqueda incremental para hallar un cambio de signo del VAN,
/// luego bisección. Si no hay cambio de signo, devuelve "No calculable" sin lanzar errores (#NUM).
/// </summary>
public static class CalculadoraTIR
{
    private const double TasaMinima = -0.9d;      // -90%
    private const double TasaMaxima = 10d;        // 1000%
    private const double PasoIncremental = 0.01d; // 1%
    private const double Tolerancia = 1e-7d;
    private const int MaxIteracionesBiseccion = 200;

    public static ResultadoTir Calcular(IReadOnlyList<decimal> flujos)
    {
        if (flujos is null || flujos.Count < 2)
            return NoCalculable("Se requieren al menos dos períodos para calcular la TIR.");

        var hayPositivo = flujos.Any(f => f > 0m);
        var hayNegativo = flujos.Any(f => f < 0m);
        if (!hayPositivo || !hayNegativo)
            return NoCalculable("El flujo de fondos no cambia de signo; la TIR no es calculable.");

        var tasaAnterior = TasaMinima;
        var npvAnterior = CalculadoraVAN.ValorActualNeto(flujos, tasaAnterior);

        for (var tasa = TasaMinima + PasoIncremental; tasa <= TasaMaxima + 1e-9d; tasa += PasoIncremental)
        {
            var npv = CalculadoraVAN.ValorActualNeto(flujos, tasa);

            if (npv == 0d)
                return Calculable((decimal)tasa, 0);

            if (Math.Sign(npv) != Math.Sign(npvAnterior))
                return Biseccion(flujos, tasaAnterior, tasa);

            tasaAnterior = tasa;
            npvAnterior = npv;
        }

        return NoCalculable("No se encontró un cambio de signo del VAN en el rango evaluado.");
    }

    private static ResultadoTir Biseccion(IReadOnlyList<decimal> flujos, double a, double b)
    {
        var fa = CalculadoraVAN.ValorActualNeto(flujos, a);

        for (var i = 1; i <= MaxIteracionesBiseccion; i++)
        {
            var medio = (a + b) / 2d;
            var fm = CalculadoraVAN.ValorActualNeto(flujos, medio);

            if (Math.Abs(fm) < Tolerancia || (b - a) / 2d < 1e-8d)
                return Calculable((decimal)medio, i);

            if (Math.Sign(fm) == Math.Sign(fa))
            {
                a = medio;
                fa = fm;
            }
            else
            {
                b = medio;
            }
        }

        return Calculable((decimal)((a + b) / 2d), MaxIteracionesBiseccion);
    }

    private static ResultadoTir Calculable(decimal tasa, int iteraciones) => new()
    {
        EsCalculable = true,
        Tir = decimal.Round(tasa, 6),
        Iteraciones = iteraciones
    };

    private static ResultadoTir NoCalculable(string mensaje) => new()
    {
        EsCalculable = false,
        Mensaje = mensaje
    };
}
