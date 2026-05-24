using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DominioActivoDiferido = SistemaAranceles.Domain.Entities.ActivoDiferido;
using InfraActivoDiferido = SistemaAranceles.Infrastructure.Persistence.Entidades.ActivoDiferido;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioActivoDiferido(ContextoAplicacion ctx) : IRepositorioActivoDiferido
{
    public async Task<DominioActivoDiferido?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var e = await ctx.ActivosDiferidos.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, ct);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<IReadOnlyList<DominioActivoDiferido>> ListarPorCarreraAsync(int carreraId, CancellationToken ct = default)
    {
        var lista = await ctx.ActivosDiferidos.AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo)
            .OrderBy(x => x.NombreRubro)
            .ToListAsync(ct);
        return lista.Select(MapearADominio).ToList();
    }

    public async Task<decimal> SumarValorPorCarreraAsync(int carreraId, CancellationToken ct = default)
        => await ctx.ActivosDiferidos.AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo)
            .SumAsync(x => x.Valor, ct);

    public async Task AgregarAsync(DominioActivoDiferido activo, CancellationToken ct = default)
        => await ctx.ActivosDiferidos.AddAsync(MapearAInfra(activo), ct);

    public void Actualizar(DominioActivoDiferido activo)
    {
        var rastreado = ctx.ChangeTracker.Entries<InfraActivoDiferido>()
            .FirstOrDefault(e => e.Entity.Id == activo.Id);
        if (rastreado is not null) rastreado.State = EntityState.Detached;
        var infra = MapearAInfra(activo);
        infra.ActualizadoEn = DateTime.UtcNow;
        ctx.ActivosDiferidos.Update(infra);
    }

    public void EliminarLogico(DominioActivoDiferido activo, int? eliminadoPorUsuarioId)
    {
        var rastreado = ctx.ChangeTracker.Entries<InfraActivoDiferido>()
            .FirstOrDefault(e => e.Entity.Id == activo.Id);
        if (rastreado is not null) rastreado.State = EntityState.Detached;
        var infra = MapearAInfra(activo);
        infra.EstaActivo = false;
        infra.EliminadoEn = DateTime.UtcNow;
        infra.EliminadoPorUsuarioId = eliminadoPorUsuarioId;
        ctx.ActivosDiferidos.Update(infra);
    }

    private static DominioActivoDiferido MapearADominio(InfraActivoDiferido e)
    {
        var d = new DominioActivoDiferido(e.CarreraId, e.NombreRubro, e.Valor, e.TasaAmortizacionAnual);
        d.RehidratarId(e.Id);
        return d;
    }

    private static InfraActivoDiferido MapearAInfra(DominioActivoDiferido d) => new()
    {
        Id = d.Id,
        CarreraId = d.CarreraId,
        NombreRubro = d.NombreRubro,
        Valor = d.Valor,
        TasaAmortizacionAnual = d.TasaAmortizacionAnual,
        EstaActivo = true,
    };
}
