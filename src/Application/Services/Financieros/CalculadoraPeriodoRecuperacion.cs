namespace SistemaAranceles.Application.Services.Financieros;

public sealed class EntradaPeriodoRecuperacion
{
    public int PeriodoOrden { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal FlujoNeto { get; init; }
    public decimal FlujoAcumulado { get; init; }
}

public sealed class ResultadoPeriodoRecuperacion
{
    public bool Recuperado { get; init; }
    public string Estado { get; init; } = "No recuperado";
    public string PeriodoRecuperacion { get; init; } = "No recuperado dentro del horizonte proyectado";
    public decimal TotalMeses { get; init; }
    public int Anios { get; init; }
    public int Meses { get; init; }
    public int Dias { get; init; }
    public int? PeriodoOrdenRecuperacion { get; init; }
    public decimal FlujoFaltanteAnterior { get; init; }
    public decimal ProporcionPeriodo { get; init; }
    public string? Mensaje { get; init; }
}

public static class CalculadoraPeriodoRecuperacion
{
    public static ResultadoPeriodoRecuperacion Calcular(
        IReadOnlyList<EntradaPeriodoRecuperacion> periodos,
        decimal mesesPorPeriodo)
    {
        if (periodos.Count == 0)
            return NoRecuperado("No hay flujo de fondos para calcular el periodo de recuperación.");

        var mesesPeriodo = mesesPorPeriodo > 0m ? mesesPorPeriodo : 6m;
        var primero = periodos.First();
        if (primero.FlujoAcumulado >= 0m)
        {
            return new ResultadoPeriodoRecuperacion
            {
                Recuperado = true,
                Estado = "Recuperado",
                PeriodoRecuperacion = primero.EtiquetaPeriodo,
                PeriodoOrdenRecuperacion = primero.PeriodoOrden
            };
        }

        for (var i = 1; i < periodos.Count; i++)
        {
            var anterior = periodos[i - 1];
            var actual = periodos[i];
            if (anterior.FlujoAcumulado >= 0m || actual.FlujoAcumulado < 0m)
                continue;

            if (actual.FlujoNeto <= 0m)
                return NoRecuperado("El flujo acumulado cambia sin un flujo neto positivo; no se puede interpolar el periodo de recuperación.");

            var faltante = Math.Abs(anterior.FlujoAcumulado);
            var proporcion = decimal.Round(Math.Clamp(faltante / actual.FlujoNeto, 0m, 1m), 6);
            var mesesTotales = decimal.Round(((i - 1) * mesesPeriodo) + (proporcion * mesesPeriodo), 4);
            var (anios, meses, dias) = ConvertirTiempo(mesesTotales);

            return new ResultadoPeriodoRecuperacion
            {
                Recuperado = true,
                Estado = "Recuperado",
                PeriodoRecuperacion = actual.EtiquetaPeriodo,
                TotalMeses = mesesTotales,
                Anios = anios,
                Meses = meses,
                Dias = dias,
                PeriodoOrdenRecuperacion = actual.PeriodoOrden,
                FlujoFaltanteAnterior = decimal.Round(faltante, 2),
                ProporcionPeriodo = proporcion
            };
        }

        return NoRecuperado("No recuperado dentro del horizonte proyectado.");
    }

    private static ResultadoPeriodoRecuperacion NoRecuperado(string mensaje) => new()
    {
        Recuperado = false,
        Estado = "No recuperado",
        PeriodoRecuperacion = "No recuperado dentro del horizonte proyectado",
        Mensaje = mensaje
    };

    private static (int anios, int meses, int dias) ConvertirTiempo(decimal totalMeses)
    {
        var anios = (int)Math.Floor(totalMeses / 12m);
        var mesesRestantes = totalMeses - (anios * 12m);
        var meses = (int)Math.Floor(mesesRestantes);
        var dias = (int)Math.Round((mesesRestantes - meses) * 30m, MidpointRounding.AwayFromZero);

        if (dias >= 30)
        {
            meses += dias / 30;
            dias %= 30;
        }

        if (meses >= 12)
        {
            anios += meses / 12;
            meses %= 12;
        }

        return (anios, meses, dias);
    }
}
