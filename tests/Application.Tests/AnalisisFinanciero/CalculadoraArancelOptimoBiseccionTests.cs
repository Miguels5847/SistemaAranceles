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
    public void ExpandeRangoSiElVanSigueNegativoEnElMaximoInicial()
    {
        var resultado = CalculadoraArancelOptimoBiseccion.Calcular(new EntradaBiseccionArancel
        {
            ArancelMinimo = 500m,
            ArancelMaximo = 5000m,
            EvaluarVan = arancel => arancel - 7000m
        });

        Assert.True(resultado.EsCalculable);
        Assert.Equal("Calculado", resultado.Estado);
        Assert.Equal(1, resultado.ExpansionesRango);
        Assert.Equal(10000m, resultado.ArancelMaximoEvaluado);
        Assert.InRange(resultado.ArancelOptimo, 6999m, 7001m);
    }

    [Fact]
    public void RetornaNoCalculableSiElVanSigueNegativoDespuesDeExpandir()
    {
        var resultado = CalculadoraArancelOptimoBiseccion.Calcular(new EntradaBiseccionArancel
        {
            ArancelMinimo = 500m,
            ArancelMaximo = 5000m,
            MaxExpansionesRango = 2,
            EvaluarVan = arancel => arancel - 50000m
        });

        Assert.False(resultado.EsCalculable);
        Assert.Equal("Rango insuficiente", resultado.Estado);
        Assert.Equal(-49500m, resultado.VanMinimo);
        Assert.Equal(-30000m, resultado.VanMaximo);
        Assert.Equal(20000m, resultado.ArancelMaximoEvaluado);
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
        Assert.Contains("positivo incluso", resultado.Mensaje);
    }
}
