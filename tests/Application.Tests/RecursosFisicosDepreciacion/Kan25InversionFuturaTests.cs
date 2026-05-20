using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Entities;
using Xunit;

namespace SistemaAranceles.Application.Tests.RecursosFisicosDepreciacion;

public class Kan25InversionFuturaTests
{
    [Fact]
    public void InversionFutura_RechazaCantidadNegativa()
    {
        Assert.Throws<DominioException>(() => new InversionFutura(1, 2024, 1, -1m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void InversionFutura_RechazaSemestreInvalido(int semestre)
    {
        Assert.Throws<DominioException>(() => new InversionFutura(1, 2024, semestre, 1m));
    }

    [Fact]
    public void InversionFutura_CalculaMontoNominalConFactor()
    {
        var inversion = new InversionFutura(1, 2024, 2, 10m);
        Assert.Equal(115m, inversion.CalcularMontoNominal(10m, 1.15m));
    }

    [Fact]
    public void InversionFutura_FactorInflacionMenorAUnoSeNormaliza()
    {
        var inversion = new InversionFutura(1, 2024, 2, 10m);
        Assert.Equal(100m, inversion.CalcularMontoNominal(10m, 0.80m));
    }

    [Fact]
    public void InflacionAplicada_FaltaRegistroUsaFactorUno()
    {
        var factor = CalculoInflacionAplicada.CalcularFactorPeriodo([], 2023, 2024, 2);
        Assert.Equal(1m, factor);
    }

    [Fact]
    public void InflacionAplicada_InflacionNegativaNoReduceFactor()
    {
        var registros = new[]
        {
            new InflacionAnual(2023, -5m, "Test", "historico")
        };

        var factor = CalculoInflacionAplicada.CalcularFactorPeriodo(registros, 2023, 2023, 2);
        Assert.Equal(1m, factor);
    }

    [Fact]
    public void InflacionAplicada_AplicaInflacionEnSegundoSemestre()
    {
        var registros = new[]
        {
            new InflacionAnual(2023, 10m, "Test", "historico")
        };

        Assert.Equal(1m, CalculoInflacionAplicada.CalcularFactorPeriodo(registros, 2023, 2023, 1));
        Assert.Equal(1.1m, CalculoInflacionAplicada.CalcularFactorPeriodo(registros, 2023, 2023, 2));
    }
}
