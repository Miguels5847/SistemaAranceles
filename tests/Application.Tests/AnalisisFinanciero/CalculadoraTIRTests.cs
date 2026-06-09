using SistemaAranceles.Application.Services.Financieros;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class CalculadoraTIRTests
{
    [Fact]
    public void ContarCambiosSigno_IgnoraCeros()
    {
        Assert.Equal(1, CalculadoraTIR.ContarCambiosSigno(new[] { -100m, 0m, 50m }));
        Assert.Equal(0, CalculadoraTIR.ContarCambiosSigno(new[] { 100m, 200m }));
        Assert.Equal(2, CalculadoraTIR.ContarCambiosSigno(new[] { -1000m, 2600m, -1650m }));
    }

    [Fact]
    public void Calcular_TodosNegativos_NoCalculable()
    {
        var resultado = CalculadoraTIR.Calcular(new[] { -100m, -50m, -30m });

        Assert.False(resultado.EsCalculable);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Mensaje));
    }

    [Fact]
    public void Calcular_ConCambioDeSigno_EsCalculable()
    {
        var resultado = CalculadoraTIR.Calcular(new[] { -1000m, 600m, 600m });

        Assert.True(resultado.EsCalculable);
        Assert.Equal(1, resultado.CambiosSigno);
        Assert.False(resultado.PosibleTirNoUnica);
    }

    [Fact]
    public void Calcular_MultiplesCambiosDeSigno_AdvierteTirNoUnica()
    {
        var resultado = CalculadoraTIR.Calcular(new[] { -1000m, 2600m, -1650m });

        Assert.True(resultado.EsCalculable);
        Assert.Equal(2, resultado.CambiosSigno);
        Assert.True(resultado.PosibleTirNoUnica);
        Assert.False(string.IsNullOrWhiteSpace(resultado.Mensaje));
    }

    [Fact]
    public void CalcularCercanaA_MultiplesRaices_DevuelveLaCercanaAlObjetivo()
    {
        // Flujo con raíces TIR ≈ 10% y ≈ 50%.
        var flujos = new[] { -1000m, 2600m, -1650m };

        var cercaBaja = CalculadoraTIR.CalcularCercanaA(flujos, 0.12m);
        var cercaAlta = CalculadoraTIR.CalcularCercanaA(flujos, 0.45m);

        Assert.True(cercaBaja.EsCalculable);
        Assert.Equal(0.10m, decimal.Round(cercaBaja.Tir, 2));
        Assert.True(cercaBaja.PosibleTirNoUnica);
        Assert.Equal(0.50m, decimal.Round(cercaAlta.Tir, 2));
    }

    [Fact]
    public void CalcularCercanaA_UnaRaiz_CoincideConCalcular()
    {
        var flujos = new[] { -1000m, 600m, 600m };

        var cercana = CalculadoraTIR.CalcularCercanaA(flujos, 0.05m);

        Assert.True(cercana.EsCalculable);
        Assert.Equal(decimal.Round(CalculadoraTIR.Calcular(flujos).Tir, 4), decimal.Round(cercana.Tir, 4));
    }
}
