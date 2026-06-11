using SistemaAranceles.Application.UseCases.Amortizacion;
using Xunit;

namespace SistemaAranceles.Application.Tests.Amortizacion;

public sealed class AmortizacionPorPeriodoTests
{
    // Inversión 20.000, 50% préstamo => capital 10.000; 12% anual, 24 meses.
    private static SistemaAranceles.Application.DTOs.Amortizacion.ResumenFinanciamientoDto Resumen()
        => ObtenerResumenAmortizacionQuery.ConstruirResumen(
            totalInversion: 20_000m,
            porcentajePrestamo: 50m,
            porcentajeConvenio: 0m,
            nombrePrestamo: null,
            nombreConvenio: null,
            tasaAnual: 12m,
            plazoMeses: 24);

    [Fact]
    public void CapitalDelPeriodo_SumaDeLosCuatroSemestres_EsElMontoDelPrestamo()
    {
        var resumen = Resumen();

        var capitalTotal = AmortizacionPorPeriodo.CapitalDelPeriodo(resumen, 1)
            + AmortizacionPorPeriodo.CapitalDelPeriodo(resumen, 2)
            + AmortizacionPorPeriodo.CapitalDelPeriodo(resumen, 3)
            + AmortizacionPorPeriodo.CapitalDelPeriodo(resumen, 4);

        Assert.Equal(resumen.MontoPrestamo, capitalTotal);
    }

    [Fact]
    public void InteresDelPeriodo_DecreceConElTiempo_YSumaElTotalDeIntereses()
    {
        var resumen = Resumen();

        var interes1 = AmortizacionPorPeriodo.InteresDelPeriodo(resumen, 1);
        var interes4 = AmortizacionPorPeriodo.InteresDelPeriodo(resumen, 4);
        var total = interes1
            + AmortizacionPorPeriodo.InteresDelPeriodo(resumen, 2)
            + AmortizacionPorPeriodo.InteresDelPeriodo(resumen, 3)
            + interes4;

        Assert.True(interes1 > interes4);
        Assert.Equal(decimal.Round(resumen.TotalIntereses, 2), total);
    }

    [Fact]
    public void SaldoAlCierre_DelUltimoPeriodo_EsCero()
    {
        var resumen = Resumen();

        Assert.True(AmortizacionPorPeriodo.SaldoAlCierreDelPeriodo(resumen, 1) > 0m);
        Assert.True(AmortizacionPorPeriodo.SaldoAlCierreDelPeriodo(resumen, 2)
            < AmortizacionPorPeriodo.SaldoAlCierreDelPeriodo(resumen, 1));
        Assert.Equal(0m, AmortizacionPorPeriodo.SaldoAlCierreDelPeriodo(resumen, 4));
        Assert.Equal(0m, AmortizacionPorPeriodo.SaldoAlCierreDelPeriodo(resumen, 9));
    }

    [Fact]
    public void PeriodoFueraDelPlazo_DevuelveCero()
    {
        var resumen = Resumen();

        Assert.Equal(0m, AmortizacionPorPeriodo.InteresDelPeriodo(resumen, 5));
        Assert.Equal(0m, AmortizacionPorPeriodo.CapitalDelPeriodo(resumen, 5));
    }

    [Fact]
    public void SinPrestamo_TodoEsCero()
    {
        var sinPrestamo = ObtenerResumenAmortizacionQuery.ConstruirResumen(
            20_000m, 0m, 0m, null, null, 12m, 24);

        Assert.Equal(0m, AmortizacionPorPeriodo.InteresDelPeriodo(sinPrestamo, 1));
        Assert.Equal(0m, AmortizacionPorPeriodo.CapitalDelPeriodo(sinPrestamo, 1));
        Assert.Equal(0m, AmortizacionPorPeriodo.SaldoAlCierreDelPeriodo(sinPrestamo, 1));
    }
}
