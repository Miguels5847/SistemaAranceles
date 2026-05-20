using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioItemMaterialInsumo(ContextoAplicacion ctx) : IRepositorioItemMaterialInsumo
{
    public async Task<decimal> SumarCostoMensualPorCategoriaAsync(
        int carreraId, string categoria, CancellationToken ct = default)
        => await ctx.ItemsMaterialInsumo.AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.CategoriaNombre == categoria)
            .SumAsync(x => x.CantidadBase * x.PrecioUnitario, ct);
}
