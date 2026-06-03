using SistemaAranceles.Application.Services.Financieros;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class CalculadoraPeriodoRecuperacionTests
{
    [Fact]
    public void CalculaRecuperacionInterpoladaCuandoElFlujoAcumuladoCruzaACero()
    {
        var periodos = new List<EntradaPeriodoRecuperacion>
        {
            new() { PeriodoOrden = 0, EtiquetaPeriodo = "Periodo 0", FlujoNeto = -1000m, FlujoAcumulado = -1000m },
            new() { PeriodoOrden = 1, EtiquetaPeriodo = "ABR 2026", FlujoNeto = 400m, FlujoAcumulado = -600m },
            new() { PeriodoOrden = 2, EtiquetaPeriodo = "SEP 2026", FlujoNeto = 800m, FlujoAcumulado = 200m }
        };

        var resultado = CalculadoraPeriodoRecuperacion.Calcular(periodos, mesesPorPeriodo: 6m);

        Assert.True(resultado.Recuperado);
        Assert.Equal("SEP 2026", resultado.PeriodoRecuperacion);
        Assert.Equal(10.5m, resultado.TotalMeses);
        Assert.Equal(0, resultado.Anios);
        Assert.Equal(10, resultado.Meses);
        Assert.Equal(15, resultado.Dias);
        Assert.Equal(600m, resultado.FlujoFaltanteAnterior);
        Assert.Equal(0.75m, resultado.ProporcionPeriodo);
    }

    [Fact]
    public void RetornaNoRecuperadoSiElAcumuladoNuncaLlegaACero()
    {
        var periodos = new List<EntradaPeriodoRecuperacion>
        {
            new() { PeriodoOrden = 0, EtiquetaPeriodo = "Periodo 0", FlujoNeto = -1000m, FlujoAcumulado = -1000m },
            new() { PeriodoOrden = 1, EtiquetaPeriodo = "ABR 2026", FlujoNeto = 200m, FlujoAcumulado = -800m },
            new() { PeriodoOrden = 2, EtiquetaPeriodo = "SEP 2026", FlujoNeto = 200m, FlujoAcumulado = -600m }
        };

        var resultado = CalculadoraPeriodoRecuperacion.Calcular(periodos, mesesPorPeriodo: 6m);

        Assert.False(resultado.Recuperado);
        Assert.Equal("No recuperado", resultado.Estado);
        Assert.Equal("No recuperado dentro del horizonte proyectado", resultado.PeriodoRecuperacion);
    }

    [Fact]
    public void RetornaNoRecuperadoSiElAcumuladoFinalVuelveANegativo()
    {
        var periodos = new List<EntradaPeriodoRecuperacion>
        {
            new() { PeriodoOrden = 0, EtiquetaPeriodo = "Periodo 0", FlujoNeto = -1000m, FlujoAcumulado = -1000m },
            new() { PeriodoOrden = 1, EtiquetaPeriodo = "ABR 2026", FlujoNeto = 1400m, FlujoAcumulado = 400m },
            new() { PeriodoOrden = 2, EtiquetaPeriodo = "SEP 2026", FlujoNeto = -600m, FlujoAcumulado = -200m }
        };

        var resultado = CalculadoraPeriodoRecuperacion.Calcular(periodos, mesesPorPeriodo: 6m);

        Assert.False(resultado.Recuperado);
        Assert.Equal("No recuperado", resultado.Estado);
        Assert.Contains("recuperación estable", resultado.Mensaje);
    }

    [Fact]
    public void UsaPrimerCruceEstableSiHuboCruceTemporalPrevio()
    {
        var periodos = new List<EntradaPeriodoRecuperacion>
        {
            new() { PeriodoOrden = 0, EtiquetaPeriodo = "Periodo 0", FlujoNeto = -1000m, FlujoAcumulado = -1000m },
            new() { PeriodoOrden = 1, EtiquetaPeriodo = "ABR 2026", FlujoNeto = 1300m, FlujoAcumulado = 300m },
            new() { PeriodoOrden = 2, EtiquetaPeriodo = "SEP 2026", FlujoNeto = -500m, FlujoAcumulado = -200m },
            new() { PeriodoOrden = 3, EtiquetaPeriodo = "ABR 2027", FlujoNeto = 700m, FlujoAcumulado = 500m }
        };

        var resultado = CalculadoraPeriodoRecuperacion.Calcular(periodos, mesesPorPeriodo: 6m);

        Assert.True(resultado.Recuperado);
        Assert.Equal("ABR 2027", resultado.PeriodoRecuperacion);
        Assert.Equal(3, resultado.PeriodoOrdenRecuperacion);
    }
}
