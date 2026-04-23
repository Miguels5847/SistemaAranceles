using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioSimulacionRetencion
{
    Task<IReadOnlyList<ResumenSimulacionRetencionDto>> ListarResumenAsync(
        int? carreraId = null,
        int? escenarioProyeccionId = null,
        int? cohorteAnio = null,
        CancellationToken cancellationToken = default);

    Task<SimulacionRetencionDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<SimulacionRetencion?> ObtenerDominioPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<SimulacionRetencion?> ObtenerDominioPorConfiguracionYCohorteAsync(int configuracionRetencionId, int cohorteAnio, CancellationToken cancellationToken = default);

    Task<bool> ExisteActivaPorConfiguracionAsync(int configuracionRetencionId, CancellationToken cancellationToken = default);

    Task AgregarAsync(SimulacionRetencion simulacion, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default);

    Task ActualizarAsync(SimulacionRetencion simulacion, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default);

    Task EliminarPorIdAsync(int id, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default);

    Task<int> LimpiarPorConfiguracionAsync(int configuracionRetencionId, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default);
}
