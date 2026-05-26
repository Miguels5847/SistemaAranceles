using SistemaAranceles.Application.DTOs.DemandaIngresos;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioRatioMaterialDemanda
{
    Task<IReadOnlyList<RatioMaterialDemandaDto>> ListarPorCarreraAsync(
        int carreraId,
        bool incluirGlobales = true,
        CancellationToken ct = default);

    Task<int> GuardarAsync(
        GuardarRatioMaterialDemandaDto dto,
        int? usuarioId,
        CancellationToken ct = default);

    Task EliminarAsync(int id, int? usuarioId, CancellationToken ct = default);
}
