using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

public sealed class ListarConfiguracionesArancelCarreraQuery(IRepositorioConfiguracionArancelCarrera repositorio)
{
    public Task<IReadOnlyList<ConfiguracionArancelCarreraDto>> EjecutarAsync(
        int? carreraId = null,
        CancellationToken ct = default)
        => repositorio.ListarAsync(carreraId, ct);
}
