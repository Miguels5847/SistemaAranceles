using SistemaAranceles.Application.Services.Financieros;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class CalculadoraArancelOptimoBiseccionTests
{
    [Fact]
    public void EncuentraArancelCuandoElVanCambiaDeSigno()
    {
        var resultado = CalculadoraArancelOptimoBiseccion.Calcular(new EntradaBiseccionArancel
        {
            ArancelMinimo = 500m,
            ArancelMaximo = 5000m,
            ToleranciaVan = 1m,
            EvaluarVan = arancel => arancel - 2500m
        });

        Assert.True(resultado.EsCalculable);
        Assert.Equal("Calculado", resultado.Estado);
        Assert.InRange(resultado.ArancelOptimo, 2499m, 2501m);
        Assert.InRange(resultado.Van, -1m, 1m);
        Assert.NotEmpty(resultado.Iteraciones);
    }

    [Fact]
    public void RetornaNoCalculableSiElVanSigueNegativoEnElMaximo()
    {
        var resultado = CalculadoraArancelOptimoBiseccion.Calcular(new EntradaBiseccionArancel
        {
            ArancelMinimo = 500m,
            ArancelMaximo = 5000m,
            EvaluarVan = arancel => arancel - 7000m
        });

        Assert.False(resultado.EsCalculable);
        Assert.Equal(-6500m, resultado.VanMinimo);
        Assert.Equal(-2000m, resultado.VanMaximo);
        Assert.Contains("sigue negativo", resultado.Mensaje);
    }

    [Fact]
    public void RetornaNoCalculableSiElVanYaEsPositivoEnElMinimo()
    {
        var resultado = CalculadoraArancelOptimoBiseccion.Calcular(new EntradaBiseccionArancel
        {
            ArancelMinimo = 500m,
            ArancelMaximo = 5000m,
            EvaluarVan = arancel => arancel + 100m
        });

        Assert.False(resultado.EsCalculable);
        Assert.Equal(600m, resultado.VanMinimo);
        Assert.Equal(5100m, resultado.VanMaximo);
        Assert.Contains("por debajo", resultado.Mensaje);
    }
}
