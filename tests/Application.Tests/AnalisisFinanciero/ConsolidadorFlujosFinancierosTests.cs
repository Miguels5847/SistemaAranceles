using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.Services.Financieros;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class ConsolidadorFlujosFinancierosTests
{
    private static IReadOnlyList<FlujoFondosPeriodoDto> FlujoSemestral() =>
    [
        new() { PeriodoOrden = 0, Anio = 0, EtiquetaPeriodo = "Periodo 0", FlujoNeto = -1000m },
        new() { PeriodoOrden = 1, Anio = 2025, EtiquetaPeriodo = "MAR 2025", FlujoNeto = 100m },
        new() { PeriodoOrden = 2, Anio = 2025, EtiquetaPeriodo = "SEP 2025", FlujoNeto = 150m },
        new() { PeriodoOrden = 3, Anio = 2026, EtiquetaPeriodo = "MAR 2026", FlujoNeto = 200m },
        new() { PeriodoOrden = 4, Anio = 2026, EtiquetaPeriodo = "SEP 2026", FlujoNeto = 250m }
    ];

    [Fact]
    public void ConstruirFlujosAnuales_AgrupaSemestresPorAnioYMantienePeriodo0()
    {
        var flujos = ConsolidadorFlujosFinancieros.ConstruirFlujosAnuales(FlujoSemestral());

        // [-1000, 100+150, 200+250]
        Assert.Equal(new[] { -1000m, 250m, 450m }, flujos);
    }

    [Fact]
    public void ConstruirDetalleAnual_DejaPeriodo0EnOrden0YUnaFilaPorAnio()
    {
        var detalle = ConsolidadorFlujosFinancieros.ConstruirDetalleAnual(FlujoSemestral());

        Assert.Equal(3, detalle.Count);
        Assert.Equal(0, detalle[0].Orden);
        Assert.Null(detalle[0].Anio);
        Assert.Equal(-1000m, detalle[0].FlujoNeto);
        Assert.Equal(2025, detalle[1].Anio);
        Assert.Equal(250m, detalle[1].FlujoNeto);
        Assert.Equal(2026, detalle[2].Anio);
        Assert.Equal(450m, detalle[2].FlujoNeto);
    }

    [Fact]
    public void ConstruirFlujosAnuales_Generico_SeparaPeriodo0DeLosAnios()
    {
        var operativos = new[] { (2025, 100m), (2025, 150m), (2026, 200m) };

        var flujos = ConsolidadorFlujosFinancieros.ConstruirFlujosAnuales(-500m, operativos);

        Assert.Equal(new[] { -500m, 250m, 200m }, flujos);
    }
}
