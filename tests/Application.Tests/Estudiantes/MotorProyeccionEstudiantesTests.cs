using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.UseCases.Estudiantes;
using Xunit;

namespace SistemaAranceles.Application.Tests.Estudiantes;

public class MotorProyeccionEstudiantesTests
{
    // Caso del bug KAN-34: la matriz debe respetar cohortes.
    // detalle[c].EstudiantesInicio = detalle[c-1].EstudiantesInicio * 0.9 (tasa fija para test simple)
    // estudiantesPorParalelo = 30, paralelos1 = 1, paralelos2 = 2.
    private static List<DetalleSimulacionRetencionDto> DetallesCarrera8Ciclos()
        => new()
        {
            new() { Ciclo = 1, EstudiantesInicio = 30m },
            new() { Ciclo = 2, EstudiantesInicio = 27m },
            new() { Ciclo = 3, EstudiantesInicio = 24.3m },
            new() { Ciclo = 4, EstudiantesInicio = 21.87m },
            new() { Ciclo = 5, EstudiantesInicio = 19.683m },
            new() { Ciclo = 6, EstudiantesInicio = 17.7147m },
            new() { Ciclo = 7, EstudiantesInicio = 15.9432m },
            new() { Ciclo = 8, EstudiantesInicio = 14.3489m },
        };

    private static decimal Celda(IReadOnlyList<CeldaProyeccionEstudiantesDto> celdas, int ciclo, int periodo)
        => celdas.First(c => c.NumeroCiclo == ciclo && c.NumeroPeriodo == periodo).TotalEstudiantes;

    [Fact]
    public void Ciclo1_UsaParalelosDelPeriodoActual()
    {
        var celdas = MotorProyeccionEstudiantes.Ejecutar(8, 1, 2, DetallesCarrera8Ciclos());

        Assert.Equal(30m, Celda(celdas, 1, 1));
        Assert.Equal(60m, Celda(celdas, 1, 2));
        Assert.Equal(30m, Celda(celdas, 1, 3));
        Assert.Equal(60m, Celda(celdas, 1, 4));
    }

    // Bug histórico: Ciclo 2 SEP 2023 salía 27 × 2 = 54.
    // Debe salir 27 porque proviene de los 30 estudiantes de ABR 2023.
    [Fact]
    public void Ciclo2_Periodo2_NoSeMultiplicaPorParalelosDelPeriodoActual()
    {
        var celdas = MotorProyeccionEstudiantes.Ejecutar(8, 1, 2, DetallesCarrera8Ciclos());

        Assert.Equal(27m, Celda(celdas, 2, 2));
        Assert.NotEqual(54m, Celda(celdas, 2, 2));
    }

    // Ciclo 2 ABR 2024 sí debe ser ≈ 54 porque viene de los 60 de SEP 2023.
    [Fact]
    public void Ciclo2_Periodo3_ProvieneDeCohorteIngresadaEnPeriodo2()
    {
        var celdas = MotorProyeccionEstudiantes.Ejecutar(8, 1, 2, DetallesCarrera8Ciclos());

        Assert.Equal(54m, Celda(celdas, 2, 3));
    }

    [Fact]
    public void Matriz_RespetaCohortesEnDiagonalCompleta()
    {
        var celdas = MotorProyeccionEstudiantes.Ejecutar(8, 1, 2, DetallesCarrera8Ciclos());

        Assert.Equal(30m, Celda(celdas, 1, 1));
        Assert.Equal(60m, Celda(celdas, 1, 2));
        Assert.Equal(27m, Celda(celdas, 2, 2));
        Assert.Equal(30m, Celda(celdas, 1, 3));
        Assert.Equal(54m, Celda(celdas, 2, 3));
        Assert.Equal(24.3m, Celda(celdas, 3, 3));
        Assert.Equal(60m, Celda(celdas, 1, 4));
        Assert.Equal(27m, Celda(celdas, 2, 4));
        Assert.Equal(48.6m, Celda(celdas, 3, 4));
        Assert.Equal(21.87m, Celda(celdas, 4, 4));
    }

    [Fact]
    public void Matriz_EsTriangularSuperior_NoHayCiclosMayoresQuePeriodo()
    {
        var celdas = MotorProyeccionEstudiantes.Ejecutar(8, 1, 2, DetallesCarrera8Ciclos());

        Assert.DoesNotContain(celdas, c => c.NumeroCiclo > c.NumeroPeriodo);
        Assert.Equal(8 * 9 / 2, celdas.Count);
    }

    [Fact]
    public void CantidadParalelos_RegistraParalelosDelPeriodo_PeroNoAfectaCiclosSuperiores()
    {
        var celdas = MotorProyeccionEstudiantes.Ejecutar(8, 1, 2, DetallesCarrera8Ciclos());

        var ciclo2Periodo2 = celdas.First(c => c.NumeroCiclo == 2 && c.NumeroPeriodo == 2);
        Assert.Equal(2, ciclo2Periodo2.CantidadParalelos);
        Assert.Equal(27m, ciclo2Periodo2.TotalEstudiantes);
    }

    [Fact]
    public void TransicionCiclo4ACiclo5_UsaRatioDeDetalles_NoCambiaReglaRetencion()
    {
        var celdas = MotorProyeccionEstudiantes.Ejecutar(8, 1, 2, DetallesCarrera8Ciclos());

        // Ciclo 5 en período 5 viene de ciclo 4 período 4 (21.87) × ratio(detalle5/detalle4) = 21.87 × 0.9 = 19.683
        Assert.Equal(19.683m, Celda(celdas, 5, 5));
    }

    [Fact]
    public void SinDetalles_DevuelveCeldasConCero()
    {
        var celdas = MotorProyeccionEstudiantes.Ejecutar(4, 1, 2, new List<DetalleSimulacionRetencionDto>());

        Assert.Equal(4 * 5 / 2, celdas.Count);
        Assert.All(celdas, c => Assert.Equal(0m, c.TotalEstudiantes));
    }
}
