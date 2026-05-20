using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DominioInversionFutura = SistemaAranceles.Domain.Entities.InversionFutura;
using InfraInversionFutura = SistemaAranceles.Infrastructure.Persistence.Entidades.InversionFutura;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioInversionFutura(ContextoAplicacion contextoAplicacion) : IRepositorioInversionFutura
{
    public async Task<DominioInversionFutura?> ObtenerPorActivoPeriodoAsync(
        int activoFijoId,
        int anio,
        int semestre,
        CancellationToken cancellationToken = default)
    {
        var entidad = await contextoAplicacion.InversionesFuturas
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.ActivoFijoId == activoFijoId &&
                x.Anio == anio &&
                x.Semestre == semestre &&
                x.EstaActivo,
                cancellationToken);

        return entidad is null ? null : MapearADominio(entidad);
    }

    public async Task<IReadOnlyList<DominioInversionFutura>> ListarPorActivosAsync(
        IReadOnlyCollection<int> activoFijoIds,
        CancellationToken cancellationToken = default)
    {
        if (activoFijoIds.Count == 0)
        {
            return [];
        }

        var lista = await contextoAplicacion.InversionesFuturas
            .AsNoTracking()
            .Where(x => activoFijoIds.Contains(x.ActivoFijoId) && x.EstaActivo)
            .OrderBy(x => x.ActivoFijoId)
            .ThenBy(x => x.Anio)
            .ThenBy(x => x.Semestre)
            .ToListAsync(cancellationToken);

        return lista.Select(MapearADominio).ToList();
    }

    public async Task AgregarAsync(DominioInversionFutura inversion, CancellationToken cancellationToken = default)
        => await contextoAplicacion.InversionesFuturas.AddAsync(MapearAInfra(inversion), cancellationToken);

    public void Actualizar(DominioInversionFutura inversion)
    {
        var rastreado = contextoAplicacion.ChangeTracker.Entries<InfraInversionFutura>()
            .FirstOrDefault(e => e.Entity.Id == inversion.Id);

        if (rastreado is not null)
        {
            rastreado.State = EntityState.Detached;
        }

        var infra = MapearAInfra(inversion);
        infra.ActualizadoEn = DateTime.UtcNow;
        contextoAplicacion.InversionesFuturas.Update(infra);
    }

    public void EliminarLogico(DominioInversionFutura inversion, int? eliminadoPorUsuarioId = null)
    {
        var rastreado = contextoAplicacion.ChangeTracker.Entries<InfraInversionFutura>()
            .FirstOrDefault(e => e.Entity.Id == inversion.Id);

        if (rastreado is not null)
        {
            rastreado.State = EntityState.Detached;
        }

        var infra = MapearAInfra(inversion);
        infra.EstaActivo = false;
        infra.EliminadoEn = DateTime.UtcNow;
        infra.EliminadoPorUsuarioId = eliminadoPorUsuarioId;
        contextoAplicacion.InversionesFuturas.Update(infra);
    }

    private static DominioInversionFutura MapearADominio(InfraInversionFutura e)
    {
        var dominio = new DominioInversionFutura(
            e.ActivoFijoId,
            e.Anio,
            e.Semestre,
            e.CantidadProyectada);

        dominio.RehidratarId(e.Id);
        return dominio;
    }

    private static InfraInversionFutura MapearAInfra(DominioInversionFutura d) => new()
    {
        Id = d.Id,
        ActivoFijoId = d.ActivoFijoId,
        Anio = d.Anio,
        Semestre = d.Semestre,
        CantidadProyectada = d.CantidadProyectada,
        EstaActivo = true
    };
}
