using SistemaAranceles.Application.Services.Financieros;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class CalculadoraTMRTests
{
    [Fact]
    public void CalcularFormula_UsaLaFormulaDelDocente()
    {
        // TMR = (9.50 * 2.20 / 100) + 9.33 = 0.209 + 9.33 = 9.539
        var tmr = CalculadoraTMR.CalcularFormula(9.50m, 2.20m, 9.33m);

        Assert.Equal(9.5390m, tmr);
    }

    [Fact]
    public void Calcular_SinManual_UsaLaFormula()
    {
        var resultado = CalculadoraTMR.Calcular(new ParametrosTmr
        {
            TasaInteresFinancieraPorcentaje = 9.50m,
            InflacionPromedioPorcentaje = 2.20m,
            PremioRiesgoPorcentaje = 9.33m,
            UsarTmrManual = false
        });

        Assert.False(resultado.EsManual);
        Assert.Equal(9.5390m, resultado.TmrPorcentaje);
        Assert.Equal(0.09539m, resultado.TmrTasa);
    }

    [Fact]
    public void Calcular_ConManual_UsaTmrManualDirectamente()
    {
        var resultado = CalculadoraTMR.Calcular(new ParametrosTmr
        {
            TasaInteresFinancieraPorcentaje = 9.50m,
            InflacionPromedioPorcentaje = 2.20m,
            PremioRiesgoPorcentaje = 9.33m,
            UsarTmrManual = true,
            TmrManualPorcentaje = 12.75m
        });

        Assert.True(resultado.EsManual);
        Assert.Equal(12.75m, resultado.TmrPorcentaje);
        Assert.Equal(0.1275m, resultado.TmrTasa);
    }
}
