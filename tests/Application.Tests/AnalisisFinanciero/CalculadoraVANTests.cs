using SistemaAranceles.Application.Services.Financieros;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class CalculadoraVANTests
{
    [Fact]
    public void Calcular_NoDescuentaElPeriodo0()
    {
        // VAN = -1000 + 600/1.1 + 600/1.21 = 41.32
        var flujos = new[] { -1000m, 600m, 600m };

        var van = CalculadoraVAN.Calcular(flujos, 0.10m);

        Assert.Equal(41.32m, van);
    }

    [Fact]
    public void FactorDescuento_EnPeriodo0_EsUno()
    {
        Assert.Equal(1m, CalculadoraVAN.FactorDescuento(0.10m, 0));
    }

    [Fact]
    public void ValorPresente_EnPeriodo0_NoSeDescuenta()
    {
        Assert.Equal(-1000m, CalculadoraVAN.ValorPresente(-1000m, 0.10m, 0));
    }

    [Fact]
    public void Calcular_ConTmrDocente_QuedaEnEquilibrioDentroDeTolerancia()
    {
        // TMR docente = 9.539% -> tasa 0.09539. Flujo de equilibrio: VAN ~ 0.
        var tmr = CalculadoraTMR.Calcular(new ParametrosTmr
        {
            TasaInteresFinancieraPorcentaje = 9.50m,
            InflacionPromedioPorcentaje = 2.20m,
            PremioRiesgoPorcentaje = 9.33m
        });
        var flujos = new[] { -1000m, 1095.39m };

        var van = CalculadoraVAN.Calcular(flujos, tmr.TmrTasa);

        Assert.InRange(van, -1m, 1m);
    }
}
