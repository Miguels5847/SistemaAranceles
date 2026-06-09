using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Services.Aranceles;
using Xunit;

namespace SistemaAranceles.Application.Tests.DemandaIngresos;

public sealed class DescuentoArancelHelperTests
{
    private static DescuentoArancelCicloDto D(int desde, int hasta, decimal pct, bool activo = true)
        => new() { CicloDesde = desde, CicloHasta = hasta, PorcentajeDescuento = pct, EstaActivo = activo };

    [Theory]
    [InlineData(1, 25)]
    [InlineData(2, 25)]
    [InlineData(3, 20)]
    [InlineData(6, 15)]
    [InlineData(7, 0)]   // fuera de todo rango => 0
    [InlineData(10, 0)]  // carrera con más ciclos sin descuento configurado
    public void ResolverPorcentaje_DevuelveDescuentoDelRango(int ciclo, int esperado)
    {
        var descuentos = new List<DescuentoArancelCicloDto> { D(1, 2, 25m), D(3, 4, 20m), D(5, 6, 15m) };
        Assert.Equal((decimal)esperado, DescuentoArancelHelper.ResolverPorcentajeDescuentoCiclo(descuentos, ciclo));
    }

    [Fact]
    public void ResolverPorcentaje_SinDescuentos_DevuelveCero()
        => Assert.Equal(0m, DescuentoArancelHelper.ResolverPorcentajeDescuentoCiclo([], 1));

    [Fact]
    public void ResolverPorcentaje_IgnoraInactivos()
    {
        var descuentos = new List<DescuentoArancelCicloDto> { D(1, 2, 25m, activo: false) };
        Assert.Equal(0m, DescuentoArancelHelper.ResolverPorcentajeDescuentoCiclo(descuentos, 1));
    }

    [Fact]
    public void CalcularArancelCiclo_AplicaDescuento()
    {
        Assert.Equal(1726.50m, DescuentoArancelHelper.CalcularArancelCiclo(2302m, 25m));
        Assert.Equal(2000m, DescuentoArancelHelper.CalcularArancelCiclo(2000m, 0m));   // sin descuento = arancel base
        Assert.Equal(0m, DescuentoArancelHelper.CalcularArancelCiclo(1000m, 100m));
    }
}
