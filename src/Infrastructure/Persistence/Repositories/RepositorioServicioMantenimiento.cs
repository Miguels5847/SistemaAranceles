using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;
using DominioServicio = SistemaAranceles.Domain.Entities.ServicioMantenimiento;
using InfraServicio = SistemaAranceles.Infrastructure.Persistence.Entidades.ServicioMantenimiento;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioServicioMantenimiento(ContextoAplicacion ctx) : IRepositorioServicioMantenimiento
{
    public async Task<DominioServicio?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var e = await ctx.ServiciosMantenimiento.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, ct);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<IReadOnlyList<DominioServicio>> ListarPorCarreraAsync(
        int carreraId, TipoRubroMantenimiento? tipo = null, CancellationToken ct = default)
    {
        var q = ctx.ServiciosMantenimiento.AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo);
        if (tipo.HasValue)
            q = q.Where(x => x.TipoRubro == tipo.Value);
        var lista = await q.OrderBy(x => x.TipoRubro).ThenBy(x => x.NombreRubro).ToListAsync(ct);
        return lista.Select(MapearADominio).ToList();
    }

    public async Task<decimal> SumarCostoAnualPorTipoAsync(int carreraId, TipoRubroMantenimiento tipo, CancellationToken ct = default)
        => await ctx.ServiciosMantenimiento.AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo && x.TipoRubro == tipo)
            .SumAsync(x => x.CostoAnualUniversidad, ct);

    public async Task AgregarAsync(DominioServicio servicio, CancellationToken ct = default)
        => await ctx.ServiciosMantenimiento.AddAsync(MapearAInfra(servicio), ct);

    public void Actualizar(DominioServicio servicio)
    {
        var rastreado = ctx.ChangeTracker.Entries<InfraServicio>()
            .FirstOrDefault(e => e.Entity.Id == servicio.Id);
        if (rastreado is not null) rastreado.State = EntityState.Detached;
        var infra = MapearAInfra(servicio);
        infra.ActualizadoEn = DateTime.UtcNow;
        ctx.ServiciosMantenimiento.Update(infra);
    }

    public void EliminarLogico(DominioServicio servicio, int? eliminadoPorUsuarioId)
    {
        var rastreado = ctx.ChangeTracker.Entries<InfraServicio>()
            .FirstOrDefault(e => e.Entity.Id == servicio.Id);
        if (rastreado is not null) rastreado.State = EntityState.Detached;
        var infra = MapearAInfra(servicio);
        infra.EstaActivo = false;
        infra.EliminadoEn = DateTime.UtcNow;
        infra.EliminadoPorUsuarioId = eliminadoPorUsuarioId;
        ctx.ServiciosMantenimiento.Update(infra);
    }

    private static DominioServicio MapearADominio(InfraServicio e)
    {
        var d = new DominioServicio(e.CarreraId, e.TipoRubro, e.NombreRubro, e.CostoAnualUniversidad);
        d.RehidratarId(e.Id);
        return d;
    }

    private static InfraServicio MapearAInfra(DominioServicio d) => new()
    {
        Id = d.Id,
        CarreraId = d.CarreraId,
        TipoRubro = d.TipoRubro,
        NombreRubro = d.NombreRubro,
        CostoAnualUniversidad = d.CostoAnualUniversidad,
        EstaActivo = true,
    };
}
