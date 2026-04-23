using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using CarreraDominio = SistemaAranceles.Domain.Entities.Carrera;
using CarreraPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.Carrera;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCarrera(
    ContextoAplicacion contextoAplicacion,
    IRepositorioGenerico<CarreraPersistencia> repositorioGenerico)
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
        var carrera = await contextoAplicacion.Carreras
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, cancellationToken);

        return carrera is null ? null : MapearADominio(carrera);
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
