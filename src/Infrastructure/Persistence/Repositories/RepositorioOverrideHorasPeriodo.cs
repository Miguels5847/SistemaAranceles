using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioOverrideHorasPeriodo(ContextoAplicacion ctx)
    : IRepositorioOverrideHorasPeriodo
{
    public async Task<IReadOnlyList<Domain.Entities.OverrideHorasPeriodo>> ListarPorProyeccionAsync(
        int proyeccionId, CancellationToken ct = default)
    {
        var lista = await ctx.OverridesHorasPeriodo
            .Where(x => x.ProyeccionId == proyeccionId && x.EstaActivo)
            .OrderBy(x => x.Periodo)
            .ToListAsync(ct);
        return lista.Select(MapearADominio).ToList();
    }

    public async Task<Domain.Entities.OverrideHorasPeriodo?> ObtenerPorProyeccionYPeriodoAsync(
        int proyeccionId, int periodo, CancellationToken ct = default)
    {
        var e = await ctx.OverridesHorasPeriodo
            .FirstOrDefaultAsync(x => x.ProyeccionId == proyeccionId && x.Periodo == periodo && x.EstaActivo, ct);
        return e is null ? null : MapearADominio(e);
    }

    public async Task AgregarAsync(Domain.Entities.OverrideHorasPeriodo entidad, CancellationToken ct = default)
        => await ctx.OverridesHorasPeriodo.AddAsync(MapearAInfra(entidad), ct);

    public void Actualizar(Domain.Entities.OverrideHorasPeriodo entidad)
    {
        var tracked = ctx.OverridesHorasPeriodo.Local
            .FirstOrDefault(x => x.ProyeccionId == entidad.ProyeccionId && x.Periodo == entidad.Periodo);
        if (tracked is not null)
        {
            tracked.HorasDocencia = entidad.HorasDocencia;
            tracked.HorasPractica = entidad.HorasPractica;
            tracked.ActualizadoEn = DateTime.UtcNow;
        }
        else
        {
            ctx.OverridesHorasPeriodo.Update(MapearAInfra(entidad));
        }
    }

    public void Eliminar(Domain.Entities.OverrideHorasPeriodo entidad)
    {
        var tracked = ctx.OverridesHorasPeriodo.Local
            .FirstOrDefault(x => x.ProyeccionId == entidad.ProyeccionId && x.Periodo == entidad.Periodo);
        if (tracked is not null) { ctx.OverridesHorasPeriodo.Remove(tracked); return; }
        ctx.Entry(MapearAInfra(entidad)).State = EntityState.Deleted;
    }

    private static Domain.Entities.OverrideHorasPeriodo MapearADominio(OverrideHorasPeriodo e)
    {
        var d = new Domain.Entities.OverrideHorasPeriodo(e.ProyeccionId, e.Periodo, e.HorasDocencia, e.HorasPractica);
        d.RehidratarId(e.Id);
        return d;
    }

    private static OverrideHorasPeriodo MapearAInfra(Domain.Entities.OverrideHorasPeriodo d) => new()
    {
        Id = d.Id,
        ProyeccionId = d.ProyeccionId,
        Periodo = d.Periodo,
        HorasDocencia = d.HorasDocencia,
        HorasPractica = d.HorasPractica,
    };
}
