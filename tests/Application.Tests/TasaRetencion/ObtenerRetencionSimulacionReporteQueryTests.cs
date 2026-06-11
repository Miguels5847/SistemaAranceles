using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.TasaRetencion;
using SistemaAranceles.Domain.Entities;
using Xunit;

namespace SistemaAranceles.Application.Tests.TasaRetencion;

public sealed class ObtenerRetencionSimulacionReporteQueryTests
{
    [Fact]
    public async Task SinConfiguracion_DevuelveSinDatos()
    {
        var query = new ObtenerRetencionSimulacionReporteQuery(new RepoFake(null));

        var dto = await query.EjecutarAsync(1, 1);

        Assert.False(dto.TieneDatos);
    }

    [Fact]
    public async Task ConConfiguracion_ReplicaElDecaimientoDeLaPantalla()
    {
        // 4 ciclos, mitad = 2: los ciclos 1-2 decaen con retención (90%) y el 3 con graduación (80%).
        var config = new ConfiguracionRetencionDto
        {
            Id = 7,
            CarreraId = 1,
            EscenarioProyeccionId = 1,
            TotalCiclos = 4,
            TasaRetencionPorcentaje = 90m,
            TasaGraduacionPorcentaje = 80m,
            EstudiantesPeriodo1 = 100m,
            EstudiantesPeriodo2 = 50m,
            ParalelosPeriodo1 = 2,
            ParalelosPeriodo2 = 1
        };
        var query = new ObtenerRetencionSimulacionReporteQuery(new RepoFake(config));

        var dto = await query.EjecutarAsync(1, 1);

        Assert.True(dto.TieneDatos);
        Assert.Equal([100m, 90m, 81m, 65m], dto.AlumnosPeriodo1PorCiclo);
        Assert.Equal([50m, 45m, 41m], dto.AlumnosPeriodo2PorCiclo);
        Assert.Equal(4, dto.TotalCiclos);
        Assert.Equal(90m, dto.TasaRetencionAplicada);
    }

    [Fact]
    public async Task ConMetas_LaTasaAplicadaEsLaMeta()
    {
        var config = new ConfiguracionRetencionDto
        {
            Id = 7,
            CarreraId = 1,
            EscenarioProyeccionId = 1,
            TotalCiclos = 2,
            TasaRetencionPorcentaje = 90m,
            TasaGraduacionPorcentaje = 80m,
            EstudiantesPeriodo1 = 10m,
            EstudiantesPeriodo2 = 10m,
            MetaRetencionPorcentaje = 95m,
            MetaGraduacionPorcentaje = 85m
        };
        var query = new ObtenerRetencionSimulacionReporteQuery(new RepoFake(config));

        var dto = await query.EjecutarAsync(1, 1);

        Assert.Equal(95m, dto.TasaRetencionAplicada);
        Assert.Equal(85m, dto.TasaGraduacionAplicada);
        // La tabla replica la pantalla: usa las tasas configuradas, no las metas.
        Assert.Equal(90m, dto.TasaRetencionPorcentaje);
    }

    private sealed class RepoFake(ConfiguracionRetencionDto? config) : IRepositorioConfiguracionRetencion
    {
        public Task<IReadOnlyList<ConfiguracionRetencionDto>> ListarDtoAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ConfiguracionRetencionDto>>(config is null ? [] : [config]);
        public Task<ConfiguracionRetencionDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(config);
        public Task<ConfiguracionRetencion?> ObtenerDominioPorIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<ConfiguracionRetencion?>(null);
        public Task<ConfiguracionRetencion?> ObtenerActivoPorCarreraYEscenarioNombreAsync(int carreraId, string escenarioNombre, CancellationToken cancellationToken = default)
            => Task.FromResult<ConfiguracionRetencion?>(null);
        public Task<bool> ExisteCombinacionAsync(int carreraId, int escenarioProyeccionId, int? excluirId = null, CancellationToken cancellationToken = default)
            => Task.FromResult(config is not null);
        public Task AgregarAsync(ConfiguracionRetencion configuracion, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ActualizarAsync(ConfiguracionRetencion configuracion, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EliminarPorIdAsync(int id, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
