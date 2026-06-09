using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DominioCatalogo = SistemaAranceles.Domain.Entities.CatalogoActivoBase;
using InfraCatalogo = SistemaAranceles.Infrastructure.Persistence.Entidades.CatalogoActivoBase;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCatalogoActivoBase(ContextoAplicacion contexto) : IRepositorioCatalogoActivoBase
{
    public async Task<IReadOnlyList<DominioCatalogo>> ListarActivosAsync(CancellationToken ct = default)
    {
        var lista = await contexto.CatalogosActivoBase
            .AsNoTracking()
            .Where(x => x.EstaActivo)
            .OrderBy(x => x.Categoria)
            .ThenBy(x => x.Descripcion)
            .ToListAsync(ct);
        return lista.Select(MapearADominio).ToList();
    }

    public async Task<DominioCatalogo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var e = await contexto.CatalogosActivoBase
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, ct);
        return e is null ? null : MapearADominio(e);
    }

    public async Task AgregarAsync(DominioCatalogo item, CancellationToken ct = default)
        => await contexto.CatalogosActivoBase.AddAsync(MapearAInfra(item), ct);

    public async Task<int> SembrarPorDefectoAsync(
        IReadOnlyList<SistemaAranceles.Domain.Constantes.ActivoBasePorDefecto> items,
        CancellationToken ct = default)
    {
        var existentes = await contexto.CatalogosActivoBase.AsNoTracking()
            .Where(x => x.EstaActivo)
            .Select(x => x.Descripcion)
            .ToListAsync(ct);
        var set = existentes.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var nuevos = items
            .Where(i => !set.Contains(i.Descripcion))
            .Select(i => new DominioCatalogo(
                i.Descripcion,
                i.Categoria,
                i.TipoCalculo,
                i.CantidadDefault,
                i.UnidadMedida,
                i.ValorUnitario,
                i.FactorMultiplicador,
                i.OffsetCantidad,
                i.VidaUtilAnios,
                i.PorcentajeResidual))
            .ToList();

        foreach (var nuevo in nuevos)
            await contexto.CatalogosActivoBase.AddAsync(MapearAInfra(nuevo), ct);

        return nuevos.Count;
    }

    public void Actualizar(DominioCatalogo item)
    {
        var rastreado = contexto.ChangeTracker.Entries<InfraCatalogo>()
            .FirstOrDefault(e => e.Entity.Id == item.Id);
        if (rastreado is not null)
            rastreado.State = EntityState.Detached;

        var infra = MapearAInfra(item);
        infra.ActualizadoEn = DateTime.UtcNow;
        contexto.CatalogosActivoBase.Update(infra);
    }

    private static DominioCatalogo MapearADominio(InfraCatalogo e)
    {
        var dominio = new DominioCatalogo(
            e.Descripcion,
            e.Categoria,
            e.TipoCalculoCantidad,
            e.CantidadDefault,
            e.UnidadMedida,
            e.ValorUnitario,
            e.FactorMultiplicador,
            e.OffsetCantidad,
            e.VidaUtilAnios,
            e.PorcentajeResidual);
        dominio.RehidratarId(e.Id);
        return dominio;
    }

    private static InfraCatalogo MapearAInfra(DominioCatalogo d) => new()
    {
        Id = d.Id,
        Descripcion = d.Descripcion,
        Categoria = d.Categoria,
        TipoCalculoCantidad = d.TipoCalculoCantidad,
        CantidadDefault = d.CantidadDefault,
        UnidadMedida = d.UnidadMedida,
        ValorUnitario = d.ValorUnitario,
        FactorMultiplicador = d.FactorMultiplicador,
        OffsetCantidad = d.OffsetCantidad,
        VidaUtilAnios = d.VidaUtilAnios,
        PorcentajeResidual = d.PorcentajeResidual,
        EstaActivo = true
    };
}
