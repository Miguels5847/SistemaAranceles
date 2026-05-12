using SistemaAranceles.Application.UseCases.CargosFacultad;
using Xunit;

namespace SistemaAranceles.Application.Tests.CargosFacultad;

/// <summary>
/// Tests para RN-79b: Pago de Tiempo Parcial por hora sin beneficios sociales.
/// Verifica que el costo semestral de TP se calcula como:
/// Costo = TarifaHora × HorasTP × 4 semanas × 6 meses × Personas × Peso × FactorInflacion
/// </summary>
public class PagoTiempoParcialsTests
{
    [Fact]
    public void CalcularCostoSemestralTiempoParcial_BaseCaseNoInflacion()
    {
        // Arrange: TarifaHora=10, hTP=6, personas=1, peso=1.0, inflación=1.0
        // Esperado: 10 * 6 * 4 * 6 = 1440 (6 horas/semana × 4 semanas × 6 meses)
        var tarifaHora = 10m;
        var horasAsignadasSemana = 6m;
        var personas = 1m;
        var peso = 1m;
        var factorInflacion = 1m;

        // Act
        var costo = CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
            tarifaHora, horasAsignadasSemana, personas, peso, factorInflacion);

        // Assert
        Assert.Equal(1440m, costo);
    }

    [Fact]
    public void CalcularCostoSemestralTiempoParcial_ConInflacion5Porciento()
    {
        // Arrange: TarifaHora=10, inflación=1.05
        // Esperado: 1440 * 1.05 = 1512 (aplicada a la tarifa)
        var tarifaHora = 10m * 1.05m; // 10.5
        var horasAsignadasSemana = 6m;
        var personas = 1m;
        var peso = 1m;
        var factorInflacion = 1m;

        // Act
        var costo = CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
            tarifaHora, horasAsignadasSemana, personas, peso, factorInflacion);

        // Assert
        Assert.Equal(1512m, costo);
    }

    [Fact]
    public void CalcularCostoSemestralTiempoParcial_ConPesoProporcional()
    {
        // Arrange: peso administrativo = 0.5 (carrera tiene mitad de estudiantes que UA)
        // Esperado: 1440 * 0.5 = 720
        var tarifaHora = 10m;
        var horasAsignadasSemana = 6m;
        var personas = 1m;
        var peso = 0.5m;
        var factorInflacion = 1m;

        // Act
        var costo = CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
            tarifaHora, horasAsignadasSemana, personas, peso, factorInflacion);

        // Assert
        Assert.Equal(720m, costo);
    }

    [Fact]
    public void CalcularCostoSemestralTiempoParcial_MultiplesPersonas()
    {
        // Arrange: 2 personas (ej. dos docentes TP)
        // Esperado: 1440 * 2 = 2880
        var tarifaHora = 10m;
        var horasAsignadasSemana = 6m;
        var personas = 2m;
        var peso = 1m;
        var factorInflacion = 1m;

        // Act
        var costo = CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
            tarifaHora, horasAsignadasSemana, personas, peso, factorInflacion);

        // Assert
        Assert.Equal(2880m, costo);
    }

    [Fact]
    public void CalcularCostoSemestralTiempoParcial_InflacionyPeso()
    {
        // Arrange: inflación 1.05, peso 0.8
        // Esperado: 1440 * 1.05 * 0.8 = 1209.6
        var tarifaHora = 10m * 1.05m; // 10.5
        var horasAsignadasSemana = 6m;
        var personas = 1m;
        var peso = 0.8m;
        var factorInflacion = 1m;

        // Act
        var costo = CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
            tarifaHora, horasAsignadasSemana, personas, peso, factorInflacion);

        // Assert
        Assert.Equal(1209.6m, costo);
    }

    [Fact]
    public void CalcularCostoSemestralTiempoParcial_ZeroHoras()
    {
        // Arrange: hTP = 0 (sin carga)
        // Esperado: 0
        var tarifaHora = 10m;
        var horasAsignadasSemana = 0m;
        var personas = 1m;
        var peso = 1m;
        var factorInflacion = 1m;

        // Act
        var costo = CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
            tarifaHora, horasAsignadasSemana, personas, peso, factorInflacion);

        // Assert
        Assert.Equal(0m, costo);
    }
}
