namespace SistemaAranceles.Application.Services.Financieros;

public sealed class EntradaBiseccionArancel
{
    public decimal ArancelMinimo { get; init; } = 500m;
    public decimal ArancelMaximo { get; init; } = 5000m;
    public decimal ToleranciaVan { get; init; } = 1m;
    public decimal MargenAproximacionVan { get; init; } = 2m;
    public int MaxIteraciones { get; init; } = 60;
    public int MaxExpansionesRango { get; init; } = 10;
    public Func<decimal, decimal> EvaluarVan { get; init; } = _ => 0m;
}

public sealed class IteracionBiseccionArancel
{
    public int Numero { get; init; }
    public decimal ArancelMinimo { get; init; }
    public decimal ArancelMaximo { get; init; }
    public decimal ArancelMedio { get; init; }
    public decimal Van { get; init; }
}

public sealed class ResultadoBiseccionArancel
{
    public bool EsCalculable { get; init; }
    public decimal ArancelOptimo { get; init; }
    public decimal Van { get; init; }
    public decimal ArancelMinimoInicial { get; init; }
    public decimal ArancelMaximoInicial { get; init; }
    public decimal ArancelMaximoEvaluado { get; init; }
    public decimal VanMinimo { get; init; }
    public decimal VanMaximo { get; init; }
    public decimal MejorArancel { get; init; }
    public decimal MejorVan { get; init; }
    public int IteracionesUsadas { get; init; }
    public int ExpansionesRango { get; init; }
    public string Estado { get; init; } = "No calculable";
    public string EstadoConvergencia { get; init; } = "No calculable";
    public string? Mensaje { get; init; }
    public IReadOnlyList<IteracionBiseccionArancel> Iteraciones { get; init; } = [];
}

public static class CalculadoraArancelOptimoBiseccion
{
    public static ResultadoBiseccionArancel Calcular(EntradaBiseccionArancel entrada)
    {
        var minimo = decimal.Round(entrada.ArancelMinimo, 2);
        var maximo = decimal.Round(entrada.ArancelMaximo, 2);
        var tolerancia = entrada.ToleranciaVan > 0m ? entrada.ToleranciaVan : 1m;
        var margen = entrada.MargenAproximacionVan >= tolerancia ? entrada.MargenAproximacionVan : tolerancia * 2m;
        var maxIteraciones = entrada.MaxIteraciones > 0 ? entrada.MaxIteraciones : 60;
        var maxExpansiones = entrada.MaxExpansionesRango >= 0 ? entrada.MaxExpansionesRango : 10;

        if (minimo <= 0m || maximo <= minimo)
        {
            return new ResultadoBiseccionArancel
            {
                ArancelMinimoInicial = minimo,
                ArancelMaximoInicial = maximo,
                ArancelMaximoEvaluado = maximo,
                Estado = "Rango invalido",
                EstadoConvergencia = "No calculable",
                Mensaje = "Rango de arancel invalido para biseccion."
            };
        }

        var vanMinimo = decimal.Round(entrada.EvaluarVan(minimo), 2);
        var vanMaximo = decimal.Round(entrada.EvaluarVan(maximo), 2);
        var maximoInicial = maximo;
        var mejorArancel = Math.Abs(vanMinimo) <= Math.Abs(vanMaximo) ? minimo : maximo;
        var mejorVan = Math.Abs(vanMinimo) <= Math.Abs(vanMaximo) ? vanMinimo : vanMaximo;

        if (Math.Abs(vanMinimo) <= tolerancia)
            return Exito(minimo, vanMinimo, minimo, maximoInicial, maximo, vanMinimo, vanMaximo, 0, []);

        if (Math.Abs(vanMaximo) <= tolerancia)
            return Exito(maximo, vanMaximo, minimo, maximoInicial, maximo, vanMinimo, vanMaximo, 0, []);

        if (vanMinimo > 0m && vanMaximo > 0m)
        {
            return new ResultadoBiseccionArancel
            {
                ArancelMinimoInicial = minimo,
                ArancelMaximoInicial = maximoInicial,
                ArancelMaximoEvaluado = maximo,
                VanMinimo = vanMinimo,
                VanMaximo = vanMaximo,
                MejorArancel = mejorArancel,
                MejorVan = mejorVan,
                Estado = "Rango sin solucion",
                EstadoConvergencia = "VAN positivo en minimo",
                Mensaje = "El VAN ya es positivo incluso con el arancel minimo. Revise el rango o los datos de costos."
            };
        }

        var expansiones = 0;
        if (vanMinimo < 0m && vanMaximo < 0m)
        {
            while (vanMaximo < 0m && expansiones < maxExpansiones)
            {
                maximo = decimal.Round(maximo * 2m, 2);
                vanMaximo = decimal.Round(entrada.EvaluarVan(maximo), 2);
                expansiones++;

                if (Math.Abs(vanMaximo) < Math.Abs(mejorVan))
                {
                    mejorArancel = maximo;
                    mejorVan = vanMaximo;
                }

                if (Math.Abs(vanMaximo) <= tolerancia)
                    return Exito(maximo, vanMaximo, minimo, maximoInicial, maximo, vanMinimo, vanMaximo, expansiones, []);
            }

            if (vanMaximo < 0m)
            {
                return new ResultadoBiseccionArancel
                {
                    ArancelMinimoInicial = minimo,
                    ArancelMaximoInicial = maximoInicial,
                    ArancelMaximoEvaluado = maximo,
                    Van = mejorVan,
                    VanMinimo = vanMinimo,
                    VanMaximo = vanMaximo,
                    MejorArancel = mejorArancel,
                    MejorVan = mejorVan,
                    ExpansionesRango = expansiones,
                    Estado = "Rango insuficiente",
                    EstadoConvergencia = "Sin cambio de signo",
                    Mensaje = "El rango de busqueda no contiene una solucion. El VAN sigue negativo incluso con el arancel maximo evaluado."
                };
            }
        }

        var bajo = minimo;
        var alto = maximo;
        var vanBajo = vanMinimo;
        var iteraciones = new List<IteracionBiseccionArancel>();

        for (var i = 1; i <= maxIteraciones; i++)
        {
            // Precisión sub-centavo en la BÚSQUEDA: cada centavo de arancel mueve el VAN ~$10
            // (cientos de estudiante-semestres), así que redondear a 2 decimales impide caer dentro
            // de la tolerancia (±$1) y la TIR del óptimo no llega a igualar la TMR. Se redondea a 6
            // decimales para converger; el arancel a cobrar se redondea a centavos sólo al mostrarlo.
            var medio = decimal.Round((bajo + alto) / 2m, 6);
            var vanMedio = decimal.Round(entrada.EvaluarVan(medio), 2);
            iteraciones.Add(new IteracionBiseccionArancel
            {
                Numero = i,
                ArancelMinimo = bajo,
                ArancelMaximo = alto,
                ArancelMedio = medio,
                Van = vanMedio
            });

            if (Math.Abs(vanMedio) < Math.Abs(mejorVan))
            {
                mejorArancel = medio;
                mejorVan = vanMedio;
            }

            if (Math.Abs(vanMedio) <= tolerancia)
                return Exito(medio, vanMedio, minimo, maximoInicial, maximo, vanMinimo, vanMaximo, expansiones, iteraciones);

            if (TieneMismoSigno(vanBajo, vanMedio))
            {
                bajo = medio;
                vanBajo = vanMedio;
            }
            else
            {
                alto = medio;
            }
        }

        var absVan = Math.Abs(mejorVan);
        var convergePorTolerancia = absVan <= tolerancia;
        var equilibrioAproximado = absVan <= margen;

        return new ResultadoBiseccionArancel
        {
            EsCalculable = true,
            ArancelOptimo = mejorArancel,
            Van = mejorVan,
            ArancelMinimoInicial = minimo,
            ArancelMaximoInicial = maximoInicial,
            ArancelMaximoEvaluado = maximo,
            VanMinimo = vanMinimo,
            VanMaximo = vanMaximo,
            MejorArancel = mejorArancel,
            MejorVan = mejorVan,
            IteracionesUsadas = iteraciones.Count,
            ExpansionesRango = expansiones,
            Estado = convergePorTolerancia ? "Calculado" : equilibrioAproximado ? "Aproximado" : "No convergió",
            EstadoConvergencia = convergePorTolerancia
                ? "Convergió"
                : equilibrioAproximado
                    ? "Equilibrio aproximado por redondeo monetario"
                    : "No convergió dentro del rango definido",
            Mensaje = convergePorTolerancia
                ? null
                : equilibrioAproximado
                    ? "Equilibrio aproximado por redondeo monetario."
                    : "No convergió dentro del rango definido; se muestra la mejor aproximación encontrada.",
            Iteraciones = iteraciones
        };
    }

    private static bool TieneMismoSigno(decimal a, decimal b)
        => a == 0m || b == 0m || (a > 0m && b > 0m) || (a < 0m && b < 0m);

    private static ResultadoBiseccionArancel Exito(
        decimal arancel,
        decimal van,
        decimal arancelMinimoInicial,
        decimal arancelMaximoInicial,
        decimal arancelMaximoEvaluado,
        decimal vanMinimo,
        decimal vanMaximo,
        int expansiones,
        IReadOnlyList<IteracionBiseccionArancel> iteraciones)
        => new()
        {
            EsCalculable = true,
            ArancelOptimo = arancel,
            Van = van,
            ArancelMinimoInicial = arancelMinimoInicial,
            ArancelMaximoInicial = arancelMaximoInicial,
            ArancelMaximoEvaluado = arancelMaximoEvaluado,
            VanMinimo = vanMinimo,
            VanMaximo = vanMaximo,
            MejorArancel = arancel,
            MejorVan = van,
            IteracionesUsadas = iteraciones.Count,
            ExpansionesRango = expansiones,
            Estado = "Calculado",
            EstadoConvergencia = "Convergió",
            Iteraciones = iteraciones
        };
}
