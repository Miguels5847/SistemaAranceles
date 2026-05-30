namespace SistemaAranceles.Application.Services.Financieros;

public sealed class EntradaBiseccionArancel
{
    public decimal ArancelMinimo { get; init; } = 500m;
    public decimal ArancelMaximo { get; init; } = 5000m;
    public decimal ToleranciaVan { get; init; } = 1m;
    public int MaxIteraciones { get; init; } = 60;
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
    public decimal VanMinimo { get; init; }
    public decimal VanMaximo { get; init; }
    public int IteracionesUsadas { get; init; }
    public string Estado { get; init; } = "No calculable";
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
        var maxIteraciones = entrada.MaxIteraciones > 0 ? entrada.MaxIteraciones : 60;

        if (minimo <= 0m || maximo <= minimo)
        {
            return new ResultadoBiseccionArancel
            {
                Mensaje = "Rango de arancel invalido para biseccion."
            };
        }

        var vanMinimo = decimal.Round(entrada.EvaluarVan(minimo), 2);
        var vanMaximo = decimal.Round(entrada.EvaluarVan(maximo), 2);

        if (Math.Abs(vanMinimo) <= tolerancia)
            return Exito(minimo, vanMinimo, vanMinimo, vanMaximo, []);

        if (Math.Abs(vanMaximo) <= tolerancia)
            return Exito(maximo, vanMaximo, vanMinimo, vanMaximo, []);

        if (vanMinimo > 0m && vanMaximo > 0m)
        {
            return new ResultadoBiseccionArancel
            {
                VanMinimo = vanMinimo,
                VanMaximo = vanMaximo,
                Mensaje = "El VAN ya es positivo en el arancel minimo; el optimo queda por debajo del rango configurado."
            };
        }

        if (vanMinimo < 0m && vanMaximo < 0m)
        {
            return new ResultadoBiseccionArancel
            {
                VanMinimo = vanMinimo,
                VanMaximo = vanMaximo,
                Mensaje = "El VAN sigue negativo en el arancel maximo; no hay punto de equilibrio dentro del rango configurado."
            };
        }

        var bajo = minimo;
        var alto = maximo;
        var iteraciones = new List<IteracionBiseccionArancel>();
        decimal mejorArancel = 0m;
        decimal mejorVan = 0m;

        for (var i = 1; i <= maxIteraciones; i++)
        {
            var medio = decimal.Round((bajo + alto) / 2m, 2);
            var vanMedio = decimal.Round(entrada.EvaluarVan(medio), 2);
            iteraciones.Add(new IteracionBiseccionArancel
            {
                Numero = i,
                ArancelMinimo = bajo,
                ArancelMaximo = alto,
                ArancelMedio = medio,
                Van = vanMedio
            });

            mejorArancel = medio;
            mejorVan = vanMedio;

            if (Math.Abs(vanMedio) <= tolerancia)
                return Exito(medio, vanMedio, vanMinimo, vanMaximo, iteraciones);

            if (vanMedio > 0m)
                alto = medio;
            else
                bajo = medio;
        }

        return new ResultadoBiseccionArancel
        {
            EsCalculable = true,
            ArancelOptimo = mejorArancel,
            Van = mejorVan,
            VanMinimo = vanMinimo,
            VanMaximo = vanMaximo,
            IteracionesUsadas = iteraciones.Count,
            Estado = Math.Abs(mejorVan) <= tolerancia ? "Calculado" : "Aproximado",
            Mensaje = Math.Abs(mejorVan) <= tolerancia
                ? null
                : "Se alcanzo el maximo de iteraciones; se muestra la mejor aproximacion encontrada.",
            Iteraciones = iteraciones
        };
    }

    private static ResultadoBiseccionArancel Exito(
        decimal arancel,
        decimal van,
        decimal vanMinimo,
        decimal vanMaximo,
        IReadOnlyList<IteracionBiseccionArancel> iteraciones)
        => new()
        {
            EsCalculable = true,
            ArancelOptimo = arancel,
            Van = van,
            VanMinimo = vanMinimo,
            VanMaximo = vanMaximo,
            IteracionesUsadas = iteraciones.Count,
            Estado = "Calculado",
            Iteraciones = iteraciones
        };
}
