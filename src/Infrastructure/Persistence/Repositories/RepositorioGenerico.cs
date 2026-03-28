using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using System.Linq.Expressions;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioGenerico<TEntidad>(ContextoAplicacion contextoAplicacion)
    : IRepositorioGenerico<TEntidad>
    where TEntidad : class
{
    private readonly DbSet<TEntidad> _entidades = contextoAplicacion.Set<TEntidad>();

    public async Task<IReadOnlyList<TEntidad>> ListarAsync(CancellationToken cancellationToken = default)
    {
        return await _entidades
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TEntidad>> BuscarAsync(
        Expression<Func<TEntidad, bool>> predicado,
        CancellationToken cancellationToken = default)
    {
        return await _entidades
            .AsNoTracking()
            .Where(predicado)
            .ToListAsync(cancellationToken);
    }

    public async Task<TEntidad?> ObtenerPorLlaveAsync(object[] claves, CancellationToken cancellationToken = default)
    {
        if (claves is null || claves.Length == 0)
        {
            throw new ArgumentException("Debe proporcionar al menos una clave de busqueda.", nameof(claves));
        }

        return await _entidades.FindAsync(claves, cancellationToken);
    }

    public async Task AgregarAsync(TEntidad entidad, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entidad);
        await _entidades.AddAsync(entidad, cancellationToken);
    }

    public void Actualizar(TEntidad entidad)
    {
        ArgumentNullException.ThrowIfNull(entidad);
        _entidades.Update(entidad);
    }

    public void Eliminar(TEntidad entidad)
    {
        ArgumentNullException.ThrowIfNull(entidad);
        _entidades.Remove(entidad);
    }
}
