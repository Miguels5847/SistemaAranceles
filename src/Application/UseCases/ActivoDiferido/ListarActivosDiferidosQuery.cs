using SistemaAranceles.Application.DTOs.ActivoDiferido;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.ActivoDiferido;

public sealed class ListarActivosDiferidosQuery(IRepositorioActivoDiferido repositorio)
{
    public async Task<IReadOnlyList<ActivoDiferidoDto>> EjecutarAsync(int carreraId, CancellationToken ct = default)
    {
        var lista = await repositorio.ListarPorCarreraAsync(carreraId, ct);
        return lista.Select(MapeoActivoDiferido.ADto).ToList();
    }
}
