using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioItemMaterialInsumo(ContextoAplicacion ctx) : IRepositorioItemMaterialInsumo
{
    public async Task<IReadOnlyList<ItemCapitalTrabajoDto>> ListarPorCarreraAsync(
        int carreraId,
        string? categoria = null,
        CancellationToken ct = default)
    {
        var consulta = ctx.ItemsMaterialInsumo.AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo);

        if (!string.IsNullOrWhiteSpace(categoria))
            consulta = consulta.Where(x => x.CategoriaNombre == categoria);

        return await consulta
            .OrderBy(x => x.CategoriaNombre)
            .ThenBy(x => x.NombreItem)
            .Select(x => new ItemCapitalTrabajoDto
            {
                Id = x.Id,
                CarreraId = x.CarreraId,
                CategoriaNombre = x.CategoriaNombre,
                Concepto = x.NombreItem,
                Cantidad = x.CantidadBase,
                ValorUnitario = x.PrecioUnitario
            })
            .ToListAsync(ct);
    }

    public async Task GuardarAsync(GuardarItemCapitalTrabajoDto dto, CancellationToken ct = default)
    {
        if (dto.Id is null or <= 0)
        {
            await ctx.ItemsMaterialInsumo.AddAsync(new ItemMaterialInsumo
            {
                CarreraId = dto.CarreraId,
                CategoriaNombre = dto.CategoriaNombre,
                NombreItem = dto.Concepto.Trim(),
                UnidadNombre = "Unidad",
                CantidadBase = dto.Cantidad,
                PrecioUnitario = dto.ValorUnitario,
                EsCantidadFija = false
            }, ct);
        }
        else
        {
            var existente = await ctx.ItemsMaterialInsumo
                .FirstOrDefaultAsync(x => x.Id == dto.Id.Value && x.EstaActivo, ct);

            if (existente is null)
                return;

            existente.CategoriaNombre = dto.CategoriaNombre;
            existente.NombreItem = dto.Concepto.Trim();
            existente.CantidadBase = dto.Cantidad;
            existente.PrecioUnitario = dto.ValorUnitario;
            existente.ActualizadoEn = DateTime.UtcNow;
        }

        await ctx.SaveChangesAsync(ct);
    }

    public async Task<int> SembrarItemsPorDefectoAsync(
        int carreraId,
        IReadOnlyList<MaterialPorDefecto> items,
        CancellationToken ct = default)
    {
        var existentes = await ctx.ItemsMaterialInsumo.AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo)
            .Select(x => x.NombreItem)
            .ToListAsync(ct);
        var existentesSet = existentes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nuevos = items
            .Where(i => !existentesSet.Contains(i.Nombre))
            .Select(i => new ItemMaterialInsumo
            {
                CarreraId = carreraId,
                CategoriaNombre = i.Categoria,
                NombreItem = i.Nombre,
                UnidadNombre = i.Unidad,
                CantidadBase = i.CantidadBase,
                PrecioUnitario = i.PrecioUnitario,
                EsCantidadFija = false
            })
            .ToList();

        if (nuevos.Count == 0)
            return 0;

        await ctx.ItemsMaterialInsumo.AddRangeAsync(nuevos, ct);
        await ctx.SaveChangesAsync(ct);
        return nuevos.Count;
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var existente = await ctx.ItemsMaterialInsumo
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (existente is null)
            return;

        ctx.ItemsMaterialInsumo.Remove(existente);
        await ctx.SaveChangesAsync(ct);
    }

    public async Task<decimal> SumarCostoMensualPorCategoriaAsync(
        int carreraId, string categoria, CancellationToken ct = default)
        => await ctx.ItemsMaterialInsumo.AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.CategoriaNombre == categoria && x.EstaActivo)
            .SumAsync(x => x.CantidadBase * x.PrecioUnitario, ct);
}
