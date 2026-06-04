namespace SistemaAranceles.Application.Services.Financieros;

public sealed class ResultadoTir
{
    public bool EsCalculable { get; init; }
    /// <summary>TIR como fracción (0.18 = 18%). Sólo válida si <see cref="EsCalculable"/>.</summary>
    public decimal Tir { get; init; }
    public int Iteraciones { get; init; }
    /// <summary>Cambios de signo de la serie de flujos (ignorando ceros).</summary>
    public int CambiosSigno { get; init; }
    /// <summary>True si hay más de un cambio de signo: la TIR puede no ser única (Descartes).</summary>
    public bool PosibleTirNoUnica { get; init; }
    /// <summary>Diagnóstico o advertencia para mostrar; se prioriza el VAN para la viabilidad.</summary>
    public string? Mensaje { get; init; }
}

/// <summary>
/// Tasa Interna de Retorno sin depender de Excel.
/// Estrategia robusta: búsqueda incremental para hallar un cambio de signo del VAN,
/// luego bisección. Si no hay cambio de signo, devuelve "No calculable" sin lanzar errores (#NUM).
///
/// La TIR es un indicador complementario: la viabilidad se decide con el VAN, porque los flujos
/// con inversiones futuras pueden tener múltiples cambios de signo y por tanto múltiples raíces.
/// </summary>
public static class CalculadoraTIR
{
    private const double TasaMinima = -0.9d;      // -90%
    private const double TasaMaxima = 10d;        // 1000%
    private const double PasoIncremental = 0.01d; // 1%
    private const double Tolerancia = 1e-7d;
    private const int MaxIteracionesBiseccion = 200;

    /// <summary>
    /// Cuenta los cambios de signo de la serie de flujos ignorando los ceros.
    /// Por la regla de Descartes, el número de raíces reales positivas (TIR) no supera este valor.
    /// </summary>
    public static int ContarCambiosSigno(IReadOnlyList<decimal> flujos)
    {
        if (flujos is null || flujos.Count == 0)
            return 0;

        var cambios = 0;
        var signoAnterior = 0;
        foreach (var flujo in flujos)
        {
            var signo = Math.Sign(flujo);
            if (signo == 0)
                continue;

            if (signoAnterior != 0 && signo != signoAnterior)
                cambios++;

            signoAnterior = signo;
        }

        return cambios;
    }

    public static ResultadoTir Calcular(IReadOnlyList<decimal> flujos)
    {
        if (flujos is null || flujos.Count < 2)
            return NoCalculable("Se requieren al menos dos períodos para calcular la TIR.");

        var cambiosSigno = ContarCambiosSigno(flujos);
        var hayPositivo = flujos.Any(f => f > 0m);
        var hayNegativo = flujos.Any(f => f < 0m);
        if (!hayPositivo || !hayNegativo)
            return NoCalculable(
                "La TIR no es calculable porque el flujo no cambia de signo. Con el arancel vigente no existen flujos positivos suficientes para recuperar la inversión.",
                cambiosSigno);

        var advertenciaMultiple = cambiosSigno > 1
            ? "El flujo presenta múltiples cambios de signo. La TIR puede no ser única. Se muestra la TIR normal calculada para mantener compatibilidad con el Excel. La viabilidad financiera debe evaluarse principalmente con el VAN."
            : null;

        var tasaAnterior = TasaMinima;
        var npvAnterior = CalculadoraVAN.ValorActualNeto(flujos, tasaAnterior);

        for (var tasa = TasaMinima + PasoIncremental; tasa <= TasaMaxima + 1e-9d; tasa += PasoIncremental)
        {
            var npv = CalculadoraVAN.ValorActualNeto(flujos, tasa);

            if (npv == 0d)
                return Calculable((decimal)tasa, 0, cambiosSigno, advertenciaMultiple);

            if (Math.Sign(npv) != Math.Sign(npvAnterior))
                return Biseccion(flujos, tasaAnterior, tasa, cambiosSigno, advertenciaMultiple);

            tasaAnterior = tasa;
            npvAnterior = npv;
        }

        return NoCalculable("No se encontró un cambio de signo del VAN en el rango evaluado.", cambiosSigno);
    }

    /// <summary>
    /// TIR normal (NO TIRM). Si el flujo tiene varias raíces (varios cambios de signo), devuelve la
    /// más cercana a <paramref name="tasaObjetivo"/> (la TMR), para coincidir con el Excel del tutor.
    /// La viabilidad principal se evalúa con el VAN.
    /// </summary>
    public static ResultadoTir CalcularCercanaA(IReadOnlyList<decimal> flujos, decimal tasaObjetivo)
    {
        if (flujos is null || flujos.Count < 2)
            return NoCalculable("Se requieren al menos dos períodos para calcular la TIR.");

        var cambiosSigno = ContarCambiosSigno(flujos);
        if (!flujos.Any(f => f > 0m) || !flujos.Any(f => f < 0m))
            return NoCalculable(
                "La TIR no es calculable porque el flujo no cambia de signo. Con el arancel vigente no existen flujos positivos suficientes para recuperar la inversión.",
                cambiosSigno);

        var raices = EncontrarRaices(flujos);
        if (raices.Count == 0)
            return NoCalculable("No se encontró un cambio de signo del VAN en el rango evaluado.", cambiosSigno);

        var advertencia = cambiosSigno > 1
            ? "El flujo presenta múltiples cambios de signo: la TIR puede no ser única. Se muestra la TIR normal más cercana a la TMR; la viabilidad se evalúa principalmente con el VAN."
            : null;

        var objetivo = (double)tasaObjetivo;
        var mejor = raices.OrderBy(r => Math.Abs(r.tasa - objetivo)).First();
        return Calculable((decimal)mejor.tasa, mejor.iteraciones, cambiosSigno, advertencia);
    }

    private static List<(double tasa, int iteraciones)> EncontrarRaices(IReadOnlyList<decimal> flujos)
    {
        var raices = new List<(double, int)>();
        var tasaAnterior = TasaMinima;
        var npvAnterior = CalculadoraVAN.ValorActualNeto(flujos, tasaAnterior);

        for (var tasa = TasaMinima + PasoIncremental; tasa <= TasaMaxima + 1e-9d; tasa += PasoIncremental)
        {
            var npv = CalculadoraVAN.ValorActualNeto(flujos, tasa);

            if (npv == 0d)
                raices.Add((tasa, 0));
            else if (npvAnterior != 0d && Math.Sign(npv) != Math.Sign(npvAnterior))
                raices.Add(BiseccionRaiz(flujos, tasaAnterior, tasa));

            tasaAnterior = tasa;
            npvAnterior = npv;
        }

        return raices;
    }

    private static ResultadoTir Biseccion(
        IReadOnlyList<decimal> flujos,
        double a,
        double b,
        int cambiosSigno,
        string? advertencia)
    {
        var (tasa, iteraciones) = BiseccionRaiz(flujos, a, b);
        return Calculable((decimal)tasa, iteraciones, cambiosSigno, advertencia);
    }

    private static (double tasa, int iteraciones) BiseccionRaiz(IReadOnlyList<decimal> flujos, double a, double b)
    {
        var fa = CalculadoraVAN.ValorActualNeto(flujos, a);

        for (var i = 1; i <= MaxIteracionesBiseccion; i++)
        {
            var medio = (a + b) / 2d;
            var fm = CalculadoraVAN.ValorActualNeto(flujos, medio);

            if (Math.Abs(fm) < Tolerancia || (b - a) / 2d < 1e-8d)
                return (medio, i);

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

        return ((a + b) / 2d, MaxIteracionesBiseccion);
    }

    private static ResultadoTir Calculable(decimal tasa, int iteraciones, int cambiosSigno, string? advertencia) => new()
    {
        EsCalculable = true,
        Tir = decimal.Round(tasa, 6),
        Iteraciones = iteraciones,
        CambiosSigno = cambiosSigno,
        PosibleTirNoUnica = cambiosSigno > 1,
        Mensaje = advertencia
    };

    private static ResultadoTir NoCalculable(string mensaje, int cambiosSigno = 0) => new()
    {
        EsCalculable = false,
        CambiosSigno = cambiosSigno,
        PosibleTirNoUnica = cambiosSigno > 1,
        Mensaje = mensaje
    };
}
