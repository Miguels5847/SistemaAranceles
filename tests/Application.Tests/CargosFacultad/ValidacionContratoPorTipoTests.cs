using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Domain.Enums;
using Moq;
using Xunit;

namespace SistemaAranceles.Application.Tests.CargosFacultad;

/// <summary>
/// Tests para RN-96/97: Validación de TipoContrato + TarifaHora/SueldoBaseMensual.
/// </summary>
public class ValidacionContratoPorTipoTests
{
    [Fact]
    public async Task AgregarCargoTP_RequiereTarifaHoraPositiva()
    {
        // Arrange
        var repositorio = new Mock<IRepositorioCargoFacultad>();
        var unidadTrabajo = new Mock<IUnidadTrabajo>();
        var comando = new AgregarCargoFacultadCommand(repositorio.Object, unidadTrabajo.Object);

        var dto = new CrearCargoFacultadDto
        {
            CarreraId = 1,
            NombreCargo = "Docente TP",
            TipoCargo = "Docente",
            SueldoBaseMensual = 0m,
            EsCargoDocente = true,
            CantidadDefault = 1m,
            TipoContrato = TipoContrato.TiempoParcial,
            TarifaHora = -5m, // INVÁLIDO: negativo
        };

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<ArgumentException>(
            () => comando.EjecutarAsync(dto));
        Assert.Contains("TarifaHora > 0", excepcion.Message);
    }

    [Fact]
    public async Task AgregarCargoTP_RequiereSueldoBaseCero()
    {
        // Arrange
        var repositorio = new Mock<IRepositorioCargoFacultad>();
        var unidadTrabajo = new Mock<IUnidadTrabajo>();
        var comando = new AgregarCargoFacultadCommand(repositorio.Object, unidadTrabajo.Object);

        var dto = new CrearCargoFacultadDto
        {
            CarreraId = 1,
            NombreCargo = "Docente TP",
            TipoCargo = "Docente",
            SueldoBaseMensual = 2000m, // INVÁLIDO para TP
            EsCargoDocente = true,
            CantidadDefault = 1m,
            TipoContrato = TipoContrato.TiempoParcial,
            TarifaHora = 10m,
        };

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<ArgumentException>(
            () => comando.EjecutarAsync(dto));
        Assert.Contains("SueldoBaseMensual = 0", excepcion.Message);
    }

    [Fact]
    public async Task AgregarCargoTC_RequiereSueldoBaseMayor()
    {
        // Arrange
        var repositorio = new Mock<IRepositorioCargoFacultad>();
        var unidadTrabajo = new Mock<IUnidadTrabajo>();
        var comando = new AgregarCargoFacultadCommand(repositorio.Object, unidadTrabajo.Object);

        var dto = new CrearCargoFacultadDto
        {
            CarreraId = 1,
            NombreCargo = "Docente TC",
            TipoCargo = "Docente",
            SueldoBaseMensual = 0m, // INVÁLIDO para TC
            EsCargoDocente = true,
            CantidadDefault = 1m,
            TipoContrato = TipoContrato.Administrativo,
            TarifaHora = 0m,
        };

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<ArgumentException>(
            () => comando.EjecutarAsync(dto));
        Assert.Contains("SueldoBaseMensual > 0", excepcion.Message);
    }

    [Fact]
    public async Task AgregarCargoTC_ProhíbeTarifaHora()
    {
        // Arrange
        var repositorio = new Mock<IRepositorioCargoFacultad>();
        var unidadTrabajo = new Mock<IUnidadTrabajo>();
        var comando = new AgregarCargoFacultadCommand(repositorio.Object, unidadTrabajo.Object);

        var dto = new CrearCargoFacultadDto
        {
            CarreraId = 1,
            NombreCargo = "Docente TC",
            TipoCargo = "Docente",
            SueldoBaseMensual = 3000m,
            EsCargoDocente = true,
            CantidadDefault = 1m,
            TipoContrato = TipoContrato.Administrativo,
            TarifaHora = 10m, // INVÁLIDO para no-TP
        };

        // Act & Assert
        var excepcion = await Assert.ThrowsAsync<ArgumentException>(
            () => comando.EjecutarAsync(dto));
        Assert.Contains("no puede tener TarifaHora", excepcion.Message);
    }

    [Fact]
    public async Task AgregarCargoTP_Success()
    {
        // Arrange: caso válido de TP
        var repositorio = new Mock<IRepositorioCargoFacultad>();
        var unidadTrabajo = new Mock<IUnidadTrabajo>();
        var comando = new AgregarCargoFacultadCommand(repositorio.Object, unidadTrabajo.Object);

        var dto = new CrearCargoFacultadDto
        {
            CarreraId = 1,
            NombreCargo = "Docente TP",
            TipoCargo = "Docente",
            SueldoBaseMensual = 0m,
            EsCargoDocente = true,
            CantidadDefault = 1m,
            TipoContrato = TipoContrato.TiempoParcial,
            TarifaHora = 9m,
        };

        // Act: no debe lanzar excepción
        await comando.EjecutarAsync(dto);

        // Assert: verificar que se agregó
        repositorio.Verify(r => r.AgregarAsync(It.IsAny<Domain.Entities.CargoFacultad>(), It.IsAny<CancellationToken>()),
            Times.Once);
        unidadTrabajo.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AgregarCargoTC_Success()
    {
        // Arrange: caso válido de Administrativo
        var repositorio = new Mock<IRepositorioCargoFacultad>();
        var unidadTrabajo = new Mock<IUnidadTrabajo>();
        var comando = new AgregarCargoFacultadCommand(repositorio.Object, unidadTrabajo.Object);

        var dto = new CrearCargoFacultadDto
        {
            CarreraId = 1,
            NombreCargo = "Decano",
            TipoCargo = "Administrativo",
            SueldoBaseMensual = 5000m,
            EsCargoDocente = false,
            CantidadDefault = 1m,
            TipoContrato = TipoContrato.Administrativo,
            TarifaHora = 0m,
        };

        // Act: no debe lanzar excepción
        await comando.EjecutarAsync(dto);

        // Assert
        repositorio.Verify(r => r.AgregarAsync(It.IsAny<Domain.Entities.CargoFacultad>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
