using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Domain.ValueObjects;
using UsuarioDominio = SistemaAranceles.Domain.Entities.Usuario;
using UsuarioPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.Usuario;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioUsuario(
    ContextoAplicacion contextoAplicacion,
    IRepositorioGenerico<UsuarioPersistencia> repositorioGenerico)
    : IRepositorioUsuario
{
    public async Task<IReadOnlyList<UsuarioDominio>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await contextoAplicacion.Usuarios
            .AsNoTracking()
            .Select(x => new
            {
                x.Id,
                x.NombreCompleto,
                x.CorreoInstitucional,
                x.HashContrasena,
                x.Estado
            })
            .ToListAsync(cancellationToken);

        return usuarios
            .Select(x => MapearADominio(
                x.Id,
                x.NombreCompleto,
                x.CorreoInstitucional,
                x.HashContrasena,
                x.Estado,
                null))
            .ToList();
    }

    public async Task<UsuarioDominio?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await contextoAplicacion.Usuarios
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.NombreCompleto,
                x.CorreoInstitucional,
                x.HashContrasena,
                x.Estado
            })
            .FirstOrDefaultAsync(cancellationToken);

        return usuario is null
            ? null
            : MapearADominio(
                usuario.Id,
                usuario.NombreCompleto,
                usuario.CorreoInstitucional,
                usuario.HashContrasena,
                usuario.Estado,
                null);
    }

    public async Task<UsuarioDominio?> ObtenerPorCorreoInstitucionalAsync(string correoInstitucional, CancellationToken cancellationToken = default)
    {
        var correo = correoInstitucional.Trim().ToLowerInvariant();

        var usuario = await contextoAplicacion.Usuarios
            .AsNoTracking()
            .Where(x => x.CorreoInstitucional == correo)
            .Select(x => new
            {
                x.Id,
                x.NombreCompleto,
                x.CorreoInstitucional,
                x.HashContrasena,
                x.Estado
            })
            .FirstOrDefaultAsync(cancellationToken);

        return usuario is null
            ? null
            : MapearADominio(
                usuario.Id,
                usuario.NombreCompleto,
                usuario.CorreoInstitucional,
                usuario.HashContrasena,
                usuario.Estado,
                null);
    }

    public async Task<bool> ExisteCorreoInstitucionalAsync(string correoInstitucional, CancellationToken cancellationToken = default)
    {
        var correo = correoInstitucional.Trim().ToLowerInvariant();
        return await contextoAplicacion.Usuarios.AnyAsync(x => x.CorreoInstitucional == correo, cancellationToken);
    }

    public async Task AgregarAsync(UsuarioDominio usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);
        await repositorioGenerico.AgregarAsync(MapearAPersistencia(usuario), cancellationToken);
    }

    public async Task ActualizarAsync(UsuarioDominio usuario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        var existe = await contextoAplicacion.Usuarios
            .AnyAsync(x => x.Id == usuario.Id, cancellationToken);

        if (!existe)
        {
            throw new KeyNotFoundException("No se encontro el usuario a actualizar.");
        }

        await contextoAplicacion.Usuarios
            .Where(x => x.Id == usuario.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.NombreCompleto, usuario.NombreCompleto)
                .SetProperty(x => x.CorreoInstitucional, usuario.CorreoInstitucional.ToString())
                .SetProperty(x => x.HashContrasena, usuario.HashContrasena)
                .SetProperty(x => x.Estado, usuario.Estado.ToString())
                .SetProperty(x => x.UltimoAccesoEn, usuario.UltimoAccesoEn), cancellationToken);
    }

    public async Task EliminarAsync(int id, int eliminadoPorUsuarioId, CancellationToken cancellationToken = default)
    {
        var filas = await contextoAplicacion.Usuarios
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.EstaActivo, false)
                .SetProperty(x => x.EliminadoPorUsuarioId, eliminadoPorUsuarioId)
                .SetProperty(x => x.Estado, EstadoUsuario.Inactivo.ToString()), cancellationToken);

        if (filas == 0)
        {
            throw new KeyNotFoundException($"No se encontro el usuario con Id {id}.");
        }
    }

    public async Task<IReadOnlyList<string>> ObtenerRolesDelUsuarioAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.UsuariosRoles
            .AsNoTracking()
            .Where(ur => ur.UsuarioId == id)
            .Include(ur => ur.Rol)
            .Select(ur => ur.Rol!.Nombre)
            .ToListAsync(cancellationToken);
    }

    private static UsuarioDominio MapearADominio(
        int id,
        string nombreCompleto,
        string correoInstitucional,
        string hashContrasena,
        string estadoPersistencia,
        DateTime? ultimoAccesoEn)
    {
        var usuario = new UsuarioDominio(
            nombreCompleto,
            new CorreoInstitucional(correoInstitucional),
            hashContrasena);

        usuario.RehidratarId(id);

        if (Enum.TryParse<EstadoUsuario>(estadoPersistencia, true, out var estado))
        {
            switch (estado)
            {
                case EstadoUsuario.Suspendido:
                    usuario.Suspender();
                    break;
                case EstadoUsuario.Inactivo:
                    usuario.Inactivar();
                    break;
                default:
                    usuario.Reactivar();
                    break;
            }
        }

        if (ultimoAccesoEn.HasValue)
        {
            usuario.RegistrarAcceso(ultimoAccesoEn.Value);
        }

        return usuario;
    }

    private static UsuarioPersistencia MapearAPersistencia(UsuarioDominio dominio)
    {
        return new UsuarioPersistencia
        {
            NombreCompleto = dominio.NombreCompleto,
            CorreoInstitucional = dominio.CorreoInstitucional.ToString(),
            HashContrasena = dominio.HashContrasena,
            Estado = dominio.Estado.ToString(),
            UltimoAccesoEn = dominio.UltimoAccesoEn
        };
    }
}
