using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using System.Diagnostics;
using InflacionAnualDominio = SistemaAranceles.Domain.Entities.InflacionAnual;
using InflacionAnualPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.InflacionAnual;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioInflacionAnual(ContextoAplicacion contextoAplicacion) : IRepositorioInflacionAnual
{
    private sealed class InflacionAnualLectura
    {
        public int Id { get; init; }
        public int Anio { get; init; }
        public decimal PorcentajeInflacion { get; init; }
        public string FuenteNombre { get; init; } = string.Empty;
        public string TipoFuente { get; init; } = string.Empty;
    }

    public async Task<IReadOnlyList<InflacionAnualDominio>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var lista = await contextoAplicacion.InflacionesAnuales
            .AsNoTracking()
            .OrderBy(x => x.Anio)
            .Select(x => new InflacionAnualLectura
            {
                Id = x.Id,
                Anio = x.Anio,
                PorcentajeInflacion = x.PorcentajeInflacion,
                FuenteNombre = x.FuenteNombre,
                TipoFuente = x.TipoFuente
            })
            .ToListAsync(cancellationToken);

        return lista.Select(MapearADominioDesdeCampos).ToList();
    }

    public async Task<InflacionAnualDominio?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entidad = await contextoAplicacion.InflacionesAnuales
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new InflacionAnualLectura
            {
                Id = x.Id,
                Anio = x.Anio,
                PorcentajeInflacion = x.PorcentajeInflacion,
                FuenteNombre = x.FuenteNombre,
                TipoFuente = x.TipoFuente
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entidad is null ? null : MapearADominioDesdeCampos(entidad);
    }

    public async Task<InflacionAnualDominio?> ObtenerPorAnioAsync(int anio, CancellationToken cancellationToken = default)
    {
        var entidad = await contextoAplicacion.InflacionesAnuales
            .AsNoTracking()
            .Where(x => x.Anio == anio)
            .Select(x => new InflacionAnualLectura
            {
                Id = x.Id,
                Anio = x.Anio,
                PorcentajeInflacion = x.PorcentajeInflacion,
                FuenteNombre = x.FuenteNombre,
                TipoFuente = x.TipoFuente
            })
            .FirstOrDefaultAsync(cancellationToken);

        return entidad is null ? null : MapearADominioDesdeCampos(entidad);
    }

    public async Task<IReadOnlyList<InflacionAnualDominio>> ListarPorRangoAsync(int anioDesde, int anioHasta, CancellationToken cancellationToken = default)
    {
        var lista = await contextoAplicacion.InflacionesAnuales
            .AsNoTracking()
            .Where(x => x.Anio >= anioDesde && x.Anio <= anioHasta)
            .OrderBy(x => x.Anio)
            .Select(x => new InflacionAnualLectura
            {
                Id = x.Id,
                Anio = x.Anio,
                PorcentajeInflacion = x.PorcentajeInflacion,
                FuenteNombre = x.FuenteNombre,
                TipoFuente = x.TipoFuente
            })
            .ToListAsync(cancellationToken);

        return lista.Select(MapearADominioDesdeCampos).ToList();
    }

    public async Task<bool> ExisteAnioAsync(int anio, int? excluirId = null, CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.InflacionesAnuales
            .AnyAsync(x => x.Anio == anio && (!excluirId.HasValue || x.Id != excluirId.Value), cancellationToken);
    }

    public async Task AgregarAsync(InflacionAnualDominio inflacionAnual, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inflacionAnual);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionDB: agregar anio={inflacionAnual.Anio}, usuario={creadoPorUsuarioId}");

        var entidad = new InflacionAnualPersistencia
        {
            Anio = inflacionAnual.Anio,
            PorcentajeInflacion = inflacionAnual.PorcentajeInflacion,
            FuenteNombre = inflacionAnual.FuenteNombre,
            TipoFuente = inflacionAnual.TipoFuente,
            CreadoEn = DateTime.UtcNow,
            CreadoPorUsuarioId = creadoPorUsuarioId,
            EstaActivo = true
        };

        await contextoAplicacion.InflacionesAnuales.AddAsync(entidad, cancellationToken);
    }

    public async Task ActualizarAsync(InflacionAnualDominio inflacionAnual, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inflacionAnual);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionDB: actualizar id={inflacionAnual.Id}, anio={inflacionAnual.Anio}, usuario={actualizadoPorUsuarioId}");

        var existe = await contextoAplicacion.InflacionesAnuales
            .AsNoTracking()
            .AnyAsync(x => x.Id == inflacionAnual.Id, cancellationToken);

        if (!existe)
            throw new KeyNotFoundException($"No se encontró el registro de inflación con Id {inflacionAnual.Id}.");

        var entidad = new InflacionAnualPersistencia
        {
            Id = inflacionAnual.Id,
            Anio = inflacionAnual.Anio,
            PorcentajeInflacion = inflacionAnual.PorcentajeInflacion,
            FuenteNombre = inflacionAnual.FuenteNombre,
            TipoFuente = inflacionAnual.TipoFuente,
            ActualizadoEn = DateTime.UtcNow,
            ActualizadoPorUsuarioId = actualizadoPorUsuarioId
        };

        contextoAplicacion.Attach(entidad);
        var entry = contextoAplicacion.Entry(entidad);
        entry.Property(x => x.Anio).IsModified = true;
        entry.Property(x => x.PorcentajeInflacion).IsModified = true;
        entry.Property(x => x.FuenteNombre).IsModified = true;
        entry.Property(x => x.TipoFuente).IsModified = true;
        entry.Property(x => x.ActualizadoEn).IsModified = true;
        entry.Property(x => x.ActualizadoPorUsuarioId).IsModified = true;
    }

    public async Task<(int registrosAnualesEliminados, int registrosProyectadosEliminados)> LimpiarTodoAsync(CancellationToken cancellationToken = default)
    {
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionDB: limpieza total solicitada.");

        var proyectadas = await contextoAplicacion.InflacionesProyectadas.ExecuteDeleteAsync(cancellationToken);
        var anuales = await contextoAplicacion.InflacionesAnuales.ExecuteDeleteAsync(cancellationToken);

        Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionDB: limpieza total completada. anuales={anuales}, proyectadas={proyectadas}");
        return (anuales, proyectadas);
    }

    public async Task EliminarPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionDB: eliminar id={id}");

        var eliminados = await contextoAplicacion.InflacionesAnuales
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (eliminados == 0)
            throw new KeyNotFoundException($"No se encontró el registro de inflación con Id {id}.");
    }

    private static InflacionAnualDominio MapearADominioDesdeCampos(InflacionAnualLectura entidad)
    {
        var dominio = new InflacionAnualDominio(
            entidad.Anio,
            entidad.PorcentajeInflacion,
            entidad.FuenteNombre,
            entidad.TipoFuente);

        dominio.RehidratarId(entidad.Id);
        return dominio;
    }
}
