using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioRol(ContextoAplicacion contextoAplicacion) : IRepositorioRol
{
    public async Task<IReadOnlyList<(int Id, string Nombre, string Descripcion)>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.Roles
            .AsNoTracking()
            .Where(r => r.EstaActivo)
            .Select(r => new { r.Id, r.Nombre, r.Descripcion })
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<(int, string, string)>)
                t.Result.Select(r => (r.Id, r.Nombre, r.Descripcion)).ToList(),
                TaskContinuationOptions.ExecuteSynchronously);
    }

    public async Task<(int Id, string Nombre)?> ObtenerPorNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default)
    {
        var rol = await contextoAplicacion.Roles
            .AsNoTracking()
            .Where(r => r.Nombre == nombre && r.EstaActivo)
            .Select(r => new { r.Id, r.Nombre })
            .FirstOrDefaultAsync(cancellationToken);

        return rol is null ? null : (rol.Id, rol.Nombre);
    }

    public async Task AsignarRolAUsuarioAsync(
        int usuarioId,
        int rolId,
        CancellationToken cancellationToken = default)
    {
        var existe = await contextoAplicacion.UsuariosRoles
            .AnyAsync(ur => ur.UsuarioId == usuarioId && ur.RolId == rolId, cancellationToken);

        if (!existe)
        {
            await contextoAplicacion.UsuariosRoles.AddAsync(
                new UsuarioRol { UsuarioId = usuarioId, RolId = rolId },
                cancellationToken);
        }
    }

    public async Task QuitarRolDeUsuarioAsync(
        int usuarioId,
        int rolId,
        CancellationToken cancellationToken = default)
    {
        var entrada = await contextoAplicacion.UsuariosRoles
            .FirstOrDefaultAsync(ur => ur.UsuarioId == usuarioId && ur.RolId == rolId, cancellationToken);

        if (entrada is not null)
            contextoAplicacion.UsuariosRoles.Remove(entrada);
    }

    public async Task<IReadOnlyList<string>> ObtenerNombresDeRolesDelUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.UsuariosRoles
            .AsNoTracking()
            .Where(ur => ur.UsuarioId == usuarioId)
            .Include(ur => ur.Rol)
            .Select(ur => ur.Rol!.Nombre)
            .ToListAsync(cancellationToken);
    }
}
