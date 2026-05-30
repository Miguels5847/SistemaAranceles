using SistemaAranceles.Application.Services.Financieros;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class CalculadoraPuntoEquilibrioTests
{
    [Fact]
    public void CalculaPuntoEquilibrioCuandoElMargenEsPositivo()
    {
        var entrada = new EntradaPuntoEquilibrioPeriodo
        {
            PeriodoAcademicoId = 10,
            EtiquetaPeriodo = "ABR 2026",
            Estudiantes = 100m,
            Ingresos = 100000m,
            CostosVariables = 30000m,
            CostosFijos = 35000m
        };

        var resultado = CalculadoraPuntoEquilibrio.CalcularPeriodo(entrada);

        Assert.True(resultado.EsCalculable);
        Assert.Equal("Calculado", resultado.Estado);
        Assert.Equal(1000m, resultado.IngresoPromedioEstudiante);
        Assert.Equal(300m, resultado.CostoVariablePorEstudiante);
        Assert.Equal(700m, resultado.MargenContribucion);
        Assert.Equal(50m, resultado.PuntoEquilibrioEstudiantes);
        Assert.Equal(50000m, resultado.PuntoEquilibrioMonetario);
    }

    [Fact]
    public void NoCalculaPuntoEquilibrioCuandoElMargenNoCubreCostosVariables()
    {
        var entrada = new EntradaPuntoEquilibrioPeriodo
        {
            PeriodoAcademicoId = 11,
            EtiquetaPeriodo = "SEP 2026",
            Estudiantes = 100m,
            Ingresos = 20000m,
            CostosVariables = 25000m,
            CostosFijos = 10000m
        };

        var resultado = CalculadoraPuntoEquilibrio.CalcularPeriodo(entrada);

        Assert.False(resultado.EsCalculable);
        Assert.Equal("Punto de equilibrio no calculable", resultado.Estado);
        Assert.Equal(-50m, resultado.MargenContribucion);
        Assert.Equal(0m, resultado.PuntoEquilibrioEstudiantes);
        Assert.Equal(0m, resultado.PuntoEquilibrioMonetario);
    }

    [Fact]
    public void NoDivideParaCeroCuandoNoHayEstudiantes()
    {
        var entrada = new EntradaPuntoEquilibrioPeriodo
        {
            PeriodoAcademicoId = 12,
            EtiquetaPeriodo = "ABR 2027",
            Estudiantes = 0m,
            Ingresos = 10000m,
            CostosVariables = 1000m,
            CostosFijos = 5000m
        };

        var resultado = CalculadoraPuntoEquilibrio.CalcularPeriodo(entrada);

        Assert.False(resultado.EsCalculable);
        Assert.Equal("Sin estudiantes", resultado.Estado);
        Assert.Equal(0m, resultado.IngresoPromedioEstudiante);
        Assert.Equal(0m, resultado.CostoVariablePorEstudiante);
    }
}
