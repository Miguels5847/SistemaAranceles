using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioDetalleSimulacionRetencion
{
    Task<IReadOnlyList<DetalleSimulacionRetencionDto>> ListarDtoPorSimulacionAsync(int simulacionRetencionId, CancellationToken cancellationToken = default);

    Task ReemplazarPorSimulacionAsync(
        int simulacionRetencionId,
        IReadOnlyList<DetalleSimulacionRetencion> detalles,
        int? usuarioId = null,
        CancellationToken cancellationToken = default);

    Task<int> EliminarPorSimulacionAsync(int simulacionRetencionId, CancellationToken cancellationToken = default);

    Task<int> EliminarPorConfiguracionAsync(int configuracionRetencionId, CancellationToken cancellationToken = default);
}
