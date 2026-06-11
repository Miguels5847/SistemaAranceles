using SistemaAranceles.Application.DTOs.Reportes;
using Xunit;

namespace SistemaAranceles.Application.Tests.Reportes;

public sealed class SeccionesReporteTests
{
    [Fact]
    public void Completo_IncluyeTodasLasSeccionesDeLasDemasDirecciones()
    {
        var completo = SeccionesReporte.ParaDireccion(DireccionReporte.Completo);

        foreach (DireccionReporte direccion in Enum.GetValues<DireccionReporte>())
        {
            foreach (var seccion in SeccionesReporte.ParaDireccion(direccion))
                Assert.Contains(seccion, completo);
        }
    }

    [Fact]
    public void Financiera_ContieneInversionCapitalFinanciamientoIndicadoresCostosFlujoYBalance()
    {
        var secciones = SeccionesReporte.ParaDireccion(DireccionReporte.Financiera);

        Assert.Equal(
        [
            SeccionReporte.InversionInicial,
            SeccionReporte.CapitalTrabajo,
            SeccionReporte.FinanciamientoAmortizacion,
            SeccionReporte.Indicadores,
            SeccionReporte.PuntoEquilibrio,
            SeccionReporte.CostosGastos,
            SeccionReporte.FlujoFondos,
            SeccionReporte.EstadoResultados,
            SeccionReporte.BalanceProyectado
        ], secciones);
    }

    [Theory]
    [InlineData(DireccionReporte.GestionDocente)]
    [InlineData(DireccionReporte.EstrategiaComercial)]
    [InlineData(DireccionReporte.Completo)]
    public void GraficoMatricula_ApareceDondeCorresponde(DireccionReporte direccion)
        => Assert.Contains(SeccionReporte.GraficoMatricula, SeccionesReporte.ParaDireccion(direccion));

    [Theory]
    [InlineData(DireccionReporte.GestionDocente)]
    [InlineData(DireccionReporte.TalentoHumano)]
    [InlineData(DireccionReporte.Completo)]
    public void GraficoDocentes_ApareceDondeCorresponde(DireccionReporte direccion)
        => Assert.Contains(SeccionReporte.GraficoDocentes, SeccionesReporte.ParaDireccion(direccion));

    [Fact]
    public void TodasLasDirecciones_TienenSeccionesTituloYSlug()
    {
        foreach (DireccionReporte direccion in Enum.GetValues<DireccionReporte>())
        {
            Assert.NotEmpty(SeccionesReporte.ParaDireccion(direccion));
            Assert.False(string.IsNullOrWhiteSpace(SeccionesReporte.Titulo(direccion)));
            Assert.False(string.IsNullOrWhiteSpace(SeccionesReporte.SlugArchivo(direccion)));
            Assert.DoesNotContain(' ', SeccionesReporte.SlugArchivo(direccion));
        }
    }
}
