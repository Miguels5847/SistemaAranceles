using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.Services.Financieros;
using Xunit;

namespace SistemaAranceles.Application.Tests.AnalisisFinanciero;

public sealed class ConsolidadorDashboardFinancieroTests
{
    [Fact]
    public void ConstruyeDashboardViableCuandoTodosLosIndicadoresSonFavorables()
    {
        var dashboard = ConsolidadorDashboardFinanciero.Construir(
            carreraId: 1,
            carreraNombre: "Sistemas",
            escenarioProyeccionId: 2,
            escenarioNombre: "Base",
            estado: new EstadoPerdidasGananciasDto
            {
                ValoresPorPeriodo =
                [
                    new() { Ingresos = 100000m, CostosServicios = 50000m, UtilidadPerdidaEjercicio = 25000m }
                ]
            },
            flujo: new FlujoFondosDto
            {
                ValoresPorPeriodo =
                [
                    new() { FlujoAcumulado = -10000m },
                    new() { FlujoAcumulado = 20000m }
                ]
            },
            indicadores: new IndicadoresFinancierosDto
            {
                TieneDatos = true,
                Van = 15000m,
                EsTirCalculable = true,
                TirPorcentaje = 22m,
                TmrPorcentaje = 16m,
                EstadoViabilidad = "Viable"
            },
            periodoRecuperacion: new PeriodoRecuperacionDto
            {
                Recuperado = true,
                PeriodoRecuperacion = "SEP 2027",
                TotalMeses = 18m,
                Detalle =
                [
                    new() { EtiquetaPeriodo = "SEP 2027", FlujoNeto = 30000m, FlujoAcumulado = 20000m }
                ]
            },
            puntoEquilibrio: new PuntoEquilibrioDto
            {
                Periodos =
                [
                    new()
                    {
                        Estudiantes = 100m,
                        PuntoEquilibrioEstudiantes = 80m,
                        EsCalculable = true
                    }
                ]
            },
            arancelOptimo: new ArancelOptimoBiseccionDto
            {
                Disponible = true,
                ArancelOptimo = 2500m,
                Van = 0m
            });

        Assert.True(dashboard.EsViable);
        Assert.Equal("Viable", dashboard.EstadoGeneral);
        Assert.Equal(5, dashboard.IndicadoresViables);
        Assert.Contains(dashboard.Recomendaciones, r => r.Prioridad == "Baja");
    }

    [Fact]
    public void MarcaDashboardEnAtencionCuandoVanYTirNoSonViables()
    {
        var dashboard = ConsolidadorDashboardFinanciero.Construir(
            carreraId: 1,
            carreraNombre: "Sistemas",
            escenarioProyeccionId: 2,
            escenarioNombre: "Base",
            estado: null,
            flujo: null,
            indicadores: new IndicadoresFinancierosDto
            {
                TieneDatos = true,
                Van = -5000m,
                EsTirCalculable = true,
                TirPorcentaje = 10m,
                TmrPorcentaje = 16m,
                EstadoViabilidad = "No viable"
            },
            periodoRecuperacion: new PeriodoRecuperacionDto { Recuperado = false, Detalle = [new()] },
            puntoEquilibrio: new PuntoEquilibrioDto(),
            arancelOptimo: new ArancelOptimoBiseccionDto());

        Assert.False(dashboard.EsViable);
        Assert.Equal("Atención requerida", dashboard.EstadoGeneral);
        Assert.Contains(dashboard.Indicadores, i => i.Nombre == "VAN" && !i.EsViable);
        Assert.Contains(dashboard.Recomendaciones, r => r.Origen == "VAN" && r.Prioridad == "Alta");
    }

    [Fact]
    public void TIRConMultiplesCambiosDeSigno_SeTrataComoAdvertenciaSiVanEsViable()
    {
        var dashboard = ConsolidadorDashboardFinanciero.Construir(
            carreraId: 1,
            carreraNombre: "Sistemas",
            escenarioProyeccionId: 2,
            escenarioNombre: "Base",
            estado: null,
            flujo: null,
            indicadores: new IndicadoresFinancierosDto
            {
                TieneDatos = true,
                Van = 1500m,
                EsTirCalculable = true,
                TirPorcentaje = 8m,
                TmrPorcentaje = 16m,
                TirPosibleNoUnica = true,
                EstadoViabilidad = "Viable"
            },
            periodoRecuperacion: null,
            puntoEquilibrio: null,
            arancelOptimo: null);

        var indicadorTir = Assert.Single(dashboard.Indicadores, i => i.Nombre == "TIR");
        Assert.True(indicadorTir.EsViable);
        Assert.Contains("Advertencia", indicadorTir.Estado);
        Assert.DoesNotContain(dashboard.Recomendaciones, r => r.Origen == "TIR" && r.Prioridad == "Alta");
        Assert.Contains(dashboard.Recomendaciones, r => r.Origen == "TIR" && r.Prioridad == "Media");
    }
}
