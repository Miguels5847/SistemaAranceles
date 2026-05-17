using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;
using Xunit;

namespace SistemaAranceles.Application.Tests.CargosFacultad;

public class Kan21PesoInflacionTests
{
    [Fact]
    public void CalcularPeso_DevuelveUnoParaDocente()
    {
        var cargo = new CargoFacultad(1, "PhD", "Docente", 3000m, true);

        var peso = CalculoCargosFacultad.CalcularPeso(cargo, 120m, 250m);

        Assert.Equal(1m, peso);
    }

    [Fact]
    public void CalcularPeso_DevuelveProporcionalParaNoDocente()
    {
        var cargo = new CargoFacultad(1, "Coordinador", "Administrativo", 2000m, false);

        var peso = CalculoCargosFacultad.CalcularPeso(cargo, 100m, 200m);

        Assert.Equal(0.3333m, peso);
    }

    [Fact]
    public void CalcularFactorInflacionEncadenado_NoAplicaInflacionActualEnP1()
    {
        var registros = new[]
        {
            new InflacionAnual(2026, 2m, "Fuente oficial", "oficial")
        };

        var factor = CalculoCargosFacultad.CalcularFactorInflacionEncadenado(
            registros,
            anioBase: 2026,
            anioPeriodo: 2026,
            numeroPeriodo: 1);

        Assert.Equal(1m, factor);
    }

    [Fact]
    public void CalcularFactorInflacionEncadenado_AplicaInflacionActualEnP2()
    {
        var registros = new[]
        {
            new InflacionAnual(2026, 2m, "Fuente oficial", "oficial")
        };

        var factor = CalculoCargosFacultad.CalcularFactorInflacionEncadenado(
            registros,
            anioBase: 2026,
            anioPeriodo: 2026,
            numeroPeriodo: 2);

        Assert.Equal(1.02m, factor);
    }

    [Fact]
    public void CalcularFactorInflacionEncadenado_EncadenaAniosPreviosYActual()
    {
        var registros = new[]
        {
            new InflacionAnual(2026, 2m, "Fuente oficial", "oficial"),
            new InflacionAnual(2027, 3m, "Fuente oficial", "oficial")
        };

        var factor = CalculoCargosFacultad.CalcularFactorInflacionEncadenado(
            registros,
            anioBase: 2026,
            anioPeriodo: 2027,
            numeroPeriodo: 2);

        Assert.Equal(1.0506m, factor);
    }
}
