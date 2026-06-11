using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class BalanceProyectadoDtoTests
{
    private static BalanceProyectadoPeriodoDto Periodo() => new()
    {
        CajaBancos = 5_000m,
        Inventarios = 300m,
        ActivoFijoBruto = 10_000m,
        DepreciacionAcumulada = 1_000m,
        ActivoDiferidoNeto = 700m,
        ParticipacionPorPagar = 150m,
        ImpuestoRentaPorPagar = 250m,
        PrestamoPorPagar = 4_000m,
        ConvenioPorPagar = 0m,
        Capital = 8_000m,
        ResultadosAcumulados = 2_600m
    };

    [Fact]
    public void TotalActivo_SumaCajaInventariosFijoNetoYDiferido()
    {
        var p = Periodo();

        Assert.Equal(9_000m, p.ActivoFijoNeto);
        Assert.Equal(5_000m + 300m + 9_000m + 700m, p.TotalActivo);
    }

    [Fact]
    public void Cuadra_CuandoActivoIgualaPasivoMasPatrimonio()
    {
        var p = Periodo();

        Assert.Equal(4_400m, p.TotalPasivo);
        Assert.Equal(10_600m, p.TotalPatrimonio);
        Assert.Equal(p.TotalActivo, p.TotalPasivoPatrimonio);
        Assert.True(p.Cuadra);
        Assert.Equal(0m, p.Diferencia);
    }

    [Fact]
    public void NoCuadra_CuandoLaDiferenciaSuperaLaTolerancia()
    {
        var p = new BalanceProyectadoPeriodoDto
        {
            CajaBancos = 1_000m,
            Capital = 900m
        };

        Assert.Equal(100m, p.Diferencia);
        Assert.False(p.Cuadra);
    }
}
