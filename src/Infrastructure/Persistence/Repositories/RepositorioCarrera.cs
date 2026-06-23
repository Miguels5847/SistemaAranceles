using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using CarreraDominio = SistemaAranceles.Domain.Entities.Carrera;
using CarreraPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.Carrera;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCarrera(
    ContextoAplicacion contextoAplicacion,
    IRepositorioGenerico<CarreraPersistencia> repositorioGenerico,
    CacheReferencia cache)
    : IRepositorioCarrera
{
    public async Task<IReadOnlyList<CarreraDominio>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var carreras = await contextoAplicacion.Carreras
            .AsNoTracking()
            .Where(x => x.EstaActivo)
            .ToListAsync(cancellationToken);

        return carreras.Select(MapearADominio).ToList();
    }

    public async Task<CarreraDominio?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // B.1: se re-pide por cada query (cabecera/labels); se cachea y se invalida al mutar carreras.
        var cacheada = cache.ObtenerCarrera(id);
        if (cacheada is not null)
            return cacheada;

        var carrera = await contextoAplicacion.Carreras
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, cancellationToken);

        var dominio = carrera is null ? null : MapearADominio(carrera);
        if (dominio is not null)
            cache.GuardarCarrera(id, dominio);
        return dominio;
    }

    public async Task<CarreraDominio?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var codigoNormalizado = codigo.Trim().ToUpperInvariant();

        var carrera = await contextoAplicacion.Carreras
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Codigo == codigoNormalizado && x.EstaActivo, cancellationToken);

        return carrera is null ? null : MapearADominio(carrera);
    }

    public async Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default)
    {
        var codigoNormalizado = codigo.Trim().ToUpperInvariant();
        return await contextoAplicacion.Carreras.AnyAsync(x => x.Codigo == codigoNormalizado && x.EstaActivo, cancellationToken);
    }

    public async Task AgregarAsync(CarreraDominio carrera, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrera);
        await repositorioGenerico.AgregarAsync(MapearAPersistencia(carrera), cancellationToken);
        cache.InvalidarCarreras();
    }

    public async Task ActualizarAsync(CarreraDominio carrera, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(carrera);

        var existente = await contextoAplicacion.Carreras
            .FirstOrDefaultAsync(x => x.Id == carrera.Id, cancellationToken);

        if (existente is null)
        {
            throw new KeyNotFoundException("No se encontro la carrera a actualizar.");
        }

        existente.Codigo = carrera.Codigo;
        existente.Nombre = carrera.Nombre;
        existente.FacultadNombre = carrera.FacultadNombre;
        existente.TotalCiclos = carrera.TotalCiclos;
        existente.ActualizadoEn = DateTime.UtcNow;
        cache.InvalidarCarreras();
    }

    public async Task EliminarPorIdAsync(int id, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        var existente = await contextoAplicacion.Carreras
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, cancellationToken);

        if (existente is null)
            throw new KeyNotFoundException("No se encontro la carrera a eliminar.");

        existente.EstaActivo = false;
        existente.EliminadoEn = DateTime.UtcNow;
        existente.EliminadoPorUsuarioId = eliminadoPorUsuarioId;
        cache.InvalidarCarreras();
    }

    private static CarreraDominio MapearADominio(CarreraPersistencia entidad)
    {
        var carrera = new CarreraDominio(entidad.Codigo, entidad.Nombre, entidad.FacultadNombre, entidad.TotalCiclos);
        carrera.RehidratarId(entidad.Id);
        return carrera;
    }

    private static CarreraPersistencia MapearAPersistencia(CarreraDominio dominio)
    {
        return new CarreraPersistencia
        {
            Codigo = dominio.Codigo,
            Nombre = dominio.Nombre,
            FacultadNombre = dominio.FacultadNombre,
            TotalCiclos = dominio.TotalCiclos
        };
    }
}
