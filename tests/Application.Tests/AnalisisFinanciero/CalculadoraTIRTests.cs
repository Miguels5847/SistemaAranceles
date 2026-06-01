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
}
