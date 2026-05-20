using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;
using DominioActivoFijo = SistemaAranceles.Domain.Entities.ActivoFijo;
using InfraActivoFijo = SistemaAranceles.Infrastructure.Persistence.Entidades.ActivoFijo;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioActivoFijo(ContextoAplicacion contextoAplicacion) : IRepositorioActivoFijo
{
    public async Task<DominioActivoFijo?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var e = await contextoAplicacion.ActivosFijos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, ct);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<IReadOnlyList<DominioActivoFijo>> ListarPorCarreraAsync(
        int carreraId,
        CategoriaActivoFijo? categoria = null,
        CancellationToken ct = default)
    {
        var consulta = contextoAplicacion.ActivosFijos
            .AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo);

        if (categoria.HasValue)
            consulta = consulta.Where(x => x.Categoria == categoria.Value);

        var lista = await consulta
            .OrderBy(x => x.Categoria)
            .ThenBy(x => x.Descripcion)
            .ToListAsync(ct);

        return lista.Select(MapearADominio).ToList();
    }

    public async Task<IReadOnlyCollection<string>> ListarDescripcionesRegistradasPorCarreraAsync(
        int carreraId,
        CancellationToken ct = default)
    {
        return await contextoAplicacion.ActivosFijos
            .AsNoTracking()
            .Where(x => x.CarreraId == carreraId)
            .Select(x => x.Descripcion)
            .ToListAsync(ct);
    }

    public async Task AgregarAsync(DominioActivoFijo activo, CancellationToken ct = default)
        => await contextoAplicacion.ActivosFijos.AddAsync(MapearAInfra(activo), ct);

    public void Actualizar(DominioActivoFijo activo)
    {
        var rastreado = contextoAplicacion.ChangeTracker.Entries<InfraActivoFijo>()
            .FirstOrDefault(e => e.Entity.Id == activo.Id);
        if (rastreado is not null)
            rastreado.State = EntityState.Detached;

        var infra = MapearAInfra(activo);
        infra.ActualizadoEn = DateTime.UtcNow;
        contextoAplicacion.ActivosFijos.Update(infra);
    }

    public void EliminarLogico(DominioActivoFijo activo, int? eliminadoPorUsuarioId)
    {
        var rastreado = contextoAplicacion.ChangeTracker.Entries<InfraActivoFijo>()
            .FirstOrDefault(e => e.Entity.Id == activo.Id);
        if (rastreado is not null)
            rastreado.State = EntityState.Detached;

        var infra = MapearAInfra(activo);
        infra.EstaActivo = false;
        infra.EliminadoEn = DateTime.UtcNow;
        infra.EliminadoPorUsuarioId = eliminadoPorUsuarioId;
        contextoAplicacion.ActivosFijos.Update(infra);
    }

    private static DominioActivoFijo MapearADominio(InfraActivoFijo e)
    {
        var dominio = new DominioActivoFijo(
            e.CarreraId,
            e.Descripcion,
            e.Categoria,
            e.Cantidad,
            e.UnidadMedida,
            e.ValorUnitario,
            e.VidaUtilAnios,
            e.PorcentajeResidual,
            e.FechaAdquisicion,
            e.TipoCalculoCantidad,
            e.FactorMultiplicador,
            e.OffsetCantidad,
            e.CategoriaPersonalizada);

        dominio.RehidratarId(e.Id);
        dominio.RehidratarFechaAdquisicion(e.FechaAdquisicion);
        return dominio;
    }

    private static InfraActivoFijo MapearAInfra(DominioActivoFijo d) => new()
    {
        Id = d.Id,
        CarreraId = d.CarreraId,
        Descripcion = d.Descripcion,
        Categoria = d.Categoria,
        CategoriaPersonalizada = d.CategoriaPersonalizada,
        Cantidad = d.Cantidad,
        UnidadMedida = d.UnidadMedida,
        ValorUnitario = d.ValorUnitario,
        VidaUtilAnios = d.VidaUtilAnios,
        PorcentajeResidual = d.PorcentajeResidual,
        FechaAdquisicion = d.FechaAdquisicion,
        TipoCalculoCantidad = d.TipoCalculoCantidad,
        FactorMultiplicador = d.FactorMultiplicador,
        OffsetCantidad = d.OffsetCantidad,
        EstaActivo = true
    };
}
