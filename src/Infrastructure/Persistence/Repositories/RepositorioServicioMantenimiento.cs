using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;
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
        int carreraId,
        TipoRubroMantenimiento? tipo = null,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default)
    {
        var q = await FiltrarPorCarreraYEscenarioAsync(carreraId, escenarioProyeccionId, ct);

        if (tipo.HasValue)
            q = q.Where(x => x.TipoRubro == tipo.Value);

        var lista = await q
            .OrderBy(x => x.TipoRubro)
            .ThenBy(x => x.NombreRubro)
            .ToListAsync(ct);

        return lista.Select(MapearADominio).ToList();
    }

    public async Task<IReadOnlyDictionary<TipoRubroMantenimiento, decimal>> SumarCostosAnualesPorTipoAsync(
        int carreraId,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default)
    {
        var q = await FiltrarPorCarreraYEscenarioAsync(carreraId, escenarioProyeccionId, ct);
        var sumas = await q
            .GroupBy(x => x.TipoRubro)
            .Select(g => new { Tipo = g.Key, Total = g.Sum(x => x.CostoAnualUniversidad) })
            .ToListAsync(ct);
        return sumas.ToDictionary(x => x.Tipo, x => x.Total);
    }

    public async Task AgregarAsync(DominioServicio servicio, CancellationToken ct = default)
        => await ctx.ServiciosMantenimiento.AddAsync(MapearAInfra(servicio), ct);

    public async Task<int> SembrarPorDefectoAsync(
        int carreraId,
        int? escenarioProyeccionId,
        IReadOnlyList<ServicioMantenimientoPorDefecto> items,
        CancellationToken ct = default)
    {
        var escenarioId = escenarioProyeccionId is > 0 ? escenarioProyeccionId : null;

        // Si el escenario tiene rubros específicos, la vista muestra SOLO esos (tapan los generales):
        // hay que completar los específicos. Si no, se completa la config general (escenario NULL),
        // que aplica a todos los escenarios sin específicos.
        var tieneEspecificos = escenarioId is not null && await ctx.ServiciosMantenimiento.AsNoTracking()
            .AnyAsync(x => x.CarreraId == carreraId && x.EstaActivo && x.EscenarioProyeccionId == escenarioId, ct);
        var destinoEscenarioId = tieneEspecificos ? escenarioId : null;

        var existentes = await ctx.ServiciosMantenimiento.AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo && x.EscenarioProyeccionId == destinoEscenarioId)
            .Select(x => new { x.TipoRubro, x.NombreRubro })
            .ToListAsync(ct);
        var set = existentes
            .Select(x => (x.TipoRubro, Nombre: x.NombreRubro.ToUpperInvariant()))
            .ToHashSet();

        var nuevos = items
            .Where(i => !set.Contains((i.Tipo, i.Nombre.ToUpperInvariant())))
            .Select(i => new DominioServicio(carreraId, i.Tipo, i.Nombre, i.CostoAnual, destinoEscenarioId, i.Sede))
            .ToList();

        foreach (var nuevo in nuevos)
            await ctx.ServiciosMantenimiento.AddAsync(MapearAInfra(nuevo), ct);

        return nuevos.Count;
    }

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

    private async Task<IQueryable<InfraServicio>> FiltrarPorCarreraYEscenarioAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct)
    {
        var baseQuery = ctx.ServiciosMantenimiento
            .AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo);

        if (escenarioProyeccionId is null or <= 0)
        {
            return baseQuery.Where(x => x.EscenarioProyeccionId == null);
        }

        var tieneEspecificos = await ctx.ServiciosMantenimiento
            .AsNoTracking()
            .AnyAsync(x => x.CarreraId == carreraId
                      && x.EstaActivo
                      && x.EscenarioProyeccionId == escenarioProyeccionId.Value, ct);

        return tieneEspecificos
            ? baseQuery.Where(x => x.EscenarioProyeccionId == escenarioProyeccionId.Value)
            : baseQuery.Where(x => x.EscenarioProyeccionId == null);
    }

    private static DominioServicio MapearADominio(InfraServicio e)
    {
        var d = new DominioServicio(
            e.CarreraId,
            e.TipoRubro,
            e.NombreRubro,
            e.CostoAnualUniversidad,
            e.EscenarioProyeccionId,
            e.Sede);
        d.RehidratarId(e.Id);
        return d;
    }

    private static InfraServicio MapearAInfra(DominioServicio d) => new()
    {
        Id = d.Id,
        CarreraId = d.CarreraId,
        EscenarioProyeccionId = d.EscenarioProyeccionId,
        Sede = d.Sede,
        TipoRubro = d.TipoRubro,
        NombreRubro = d.NombreRubro,
        CostoAnualUniversidad = d.CostoAnualUniversidad,
        EstaActivo = true,
    };
}
