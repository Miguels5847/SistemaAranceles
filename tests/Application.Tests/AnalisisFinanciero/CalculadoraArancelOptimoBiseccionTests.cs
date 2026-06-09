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

    [Fact]
    public void EquilibrioAproximadoCuandoVanQuedaDentroDelMargen()
    {
        // 1 iteración deja el mejor VAN ≈ -500.5 (no llega a tolerancia 1, pero entra en el margen).
        var resultado = CalculadoraArancelOptimoBiseccion.Calcular(new EntradaBiseccionArancel
        {
            ArancelMinimo = 500m,
            ArancelMaximo = 5000m,
            ToleranciaVan = 1m,
            MargenAproximacionVan = 600m,
            MaxIteraciones = 1,
            EvaluarVan = arancel => arancel - 1000.5m
        });

        Assert.True(resultado.EsCalculable);
        Assert.Equal("Aproximado", resultado.Estado);
        Assert.Equal("Equilibrio aproximado por redondeo monetario", resultado.EstadoConvergencia);
    }

    [Fact]
    public void NoConvergioCuandoVanSuperaElMargen()
    {
        var resultado = CalculadoraArancelOptimoBiseccion.Calcular(new EntradaBiseccionArancel
        {
            ArancelMinimo = 500m,
            ArancelMaximo = 5000m,
            ToleranciaVan = 1m,
            MargenAproximacionVan = 100m,
            MaxIteraciones = 1,
            EvaluarVan = arancel => arancel - 1000.5m
        });

        Assert.True(resultado.EsCalculable);
        Assert.Equal("No convergió", resultado.Estado);
        Assert.Equal("No convergió dentro del rango definido", resultado.EstadoConvergencia);
    }
}
