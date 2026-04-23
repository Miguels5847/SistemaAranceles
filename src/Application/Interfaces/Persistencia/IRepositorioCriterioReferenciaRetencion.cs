using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioCriterioReferenciaRetencion
{
    Task<CriterioReferenciaRetencion?> ObtenerPorConfiguracionAsync(int configuracionRetencionId, CancellationToken cancellationToken = default);

    Task<CriterioReferenciaRetencion?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistePorConfiguracionAsync(int configuracionRetencionId, CancellationToken cancellationToken = default);

    Task AgregarAsync(CriterioReferenciaRetencion criterio, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default);

    Task ActualizarAsync(CriterioReferenciaRetencion criterio, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default);
}
