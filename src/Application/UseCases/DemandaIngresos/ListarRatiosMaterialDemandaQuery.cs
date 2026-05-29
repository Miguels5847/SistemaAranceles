using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

public sealed class ListarRatiosMaterialDemandaQuery(IRepositorioRatioMaterialDemanda repositorio)
{
    public Task<IReadOnlyList<RatioMaterialDemandaDto>> EjecutarAsync(
        int carreraId,
        bool incluirGlobales = true,
        CancellationToken ct = default)
        => repositorio.ListarPorCarreraAsync(carreraId, incluirGlobales, ct);
}
