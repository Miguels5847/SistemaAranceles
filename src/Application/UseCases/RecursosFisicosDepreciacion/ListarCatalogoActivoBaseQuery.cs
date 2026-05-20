using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public sealed class ListarCatalogoActivoBaseQuery(IRepositorioCatalogoActivoBase repositorio)
{
    public Task<IReadOnlyList<CatalogoActivoBase>> EjecutarAsync(CancellationToken ct = default)
        => repositorio.ListarActivosAsync(ct);
}
