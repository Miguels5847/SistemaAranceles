using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioConfiguracionRetencion
{
    Task<IReadOnlyList<ConfiguracionRetencionDto>> ListarDtoAsync(CancellationToken cancellationToken = default);

    Task<ConfiguracionRetencionDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ConfiguracionRetencion?> ObtenerDominioPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ConfiguracionRetencion?> ObtenerActivoPorCarreraYEscenarioNombreAsync(int carreraId, string escenarioNombre, CancellationToken cancellationToken = default);

    Task<bool> ExisteCombinacionAsync(int carreraId, int escenarioProyeccionId, int? excluirId = null, CancellationToken cancellationToken = default);

    Task AgregarAsync(ConfiguracionRetencion configuracion, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default);

    Task ActualizarAsync(ConfiguracionRetencion configuracion, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default);

    Task EliminarPorIdAsync(int id, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default);
}
