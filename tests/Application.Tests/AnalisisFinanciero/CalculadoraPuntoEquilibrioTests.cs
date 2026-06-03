using SistemaAranceles.Application.DTOs.CostosGastos;
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

    [Fact]
    public void CalcularResumen_ArmaLosDosBloquesSegunElExcel()
    {
        var costo = new CostoGastoPeriodoDto
        {
            // Costo por servicio (9 rubros del Excel) => 18000
            MantenimientoEdificio = 10000m,
            CapacitacionDocente = 2000m,
            Internacionalizacion = 2000m,
            CostoSeguroEstudiantil = 1000m,
            BecasInstitucionales = 1000m,
            Investigacion = 500m,
            Vinculacion = 500m,
            MaterialesSuministros = 500m,
            ServiciosBasicos = 500m,
            // Gastos de Personal (docentes) => 4000
            TiempoCompletoPhd = 2000m,
            TiempoCompletoMgs = 1000m,
            MedioTiempo = 500m,
            TiempoParcial = 300m,
            OcasionalTipo2TecnicoDocente = 200m,
            // Gastos Administrativos y Ventas => 1500 + 500 = 2000
            AdministracionCentral = 1000m,
            Decano = 500m,
            MarketingComunicacion = 500m,
            // Depreciacion y Amortizacion => 600
            Depreciacion = 400m,
            Amortizacion = 200m,
            // Intereses
            Interes = 1000m
        };

        var r = CalculadoraPuntoEquilibrio.CalcularResumen(costo, ingresos: 20000m, estudiantes: 100m, ciclos: 8);

        // Bloque A
        Assert.Equal(20000m, r.Ingresos);
        Assert.Equal(18000m, r.CostoServicio);
        Assert.Equal(2000m, r.MargenBruto);
        Assert.Equal(4000m, r.GastosPersonal);
        Assert.Equal(2000m, r.GastosAdminVentas);
        Assert.Equal(600m, r.DepreciacionAmortizacion);
        Assert.Equal(1000m, r.Intereses);
        Assert.Equal(6400m, r.TotalGastos); // 4000 + 2000 - 600 + 1000
        Assert.Equal(-4400m, r.Beneficio);  // 2000 - 6400

        // Bloque B
        Assert.Equal(18000m, r.CostoVariable);
        Assert.Equal(6400m, r.CostoFijo);
        Assert.Equal(200m, r.IngresoPromedio);
        Assert.Equal(180m, r.CostoVariablePorEstudiante);
        Assert.Equal(20m, r.MargenContribucion);
        Assert.Equal(320m, r.PuntoEquilibrioEstudiantes); // 6400 / 20
        Assert.Equal(8, r.Ciclos);
        Assert.Equal(40m, r.EstudiantesPorCiclo);          // 320 / 8
        Assert.Equal(54m, r.EstudiantesPorCicloDesercion35);  // 40 * 1.35
        Assert.Equal(47m, r.EstudiantesPorCicloDesercion175); // 40 * 1.175
        Assert.True(r.EsCalculable);
    }

    [Fact]
    public void CalcularResumen_SinEstudiantes_NoEsCalculable()
    {
        var costo = new CostoGastoPeriodoDto { MantenimientoEdificio = 1000m, Interes = 500m };

        var r = CalculadoraPuntoEquilibrio.CalcularResumen(costo, ingresos: 10000m, estudiantes: 0m, ciclos: 8);

        Assert.False(r.EsCalculable);
        Assert.Equal(0m, r.IngresoPromedio);
        Assert.Equal(0m, r.PuntoEquilibrioEstudiantes);
        Assert.Equal(0m, r.EstudiantesPorCiclo);
    }
}
