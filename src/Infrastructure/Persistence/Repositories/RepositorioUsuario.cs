using Microsoft.EntityFrameworkCore;
using Npgsql;
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
                x.Estado,
                x.UltimoAccesoEn
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
                usuario.UltimoAccesoEn);
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

    public async Task<int> AgregarConRolAsync(
        UsuarioDominio usuario,
        int rolId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        await using var transaction = await contextoAplicacion.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var entidadUsuario = MapearAPersistencia(usuario);
            await contextoAplicacion.Usuarios.AddAsync(entidadUsuario, cancellationToken);
            await contextoAplicacion.SaveChangesAsync(cancellationToken);

            var existeRol = await contextoAplicacion.UsuariosRoles
                .AnyAsync(x => x.UsuarioId == entidadUsuario.Id && x.RolId == rolId, cancellationToken);

            if (!existeRol)
            {
                await contextoAplicacion.UsuariosRoles.AddAsync(new Infrastructure.Persistence.Entidades.UsuarioRol
                {
                    UsuarioId = entidadUsuario.Id,
                    RolId = rolId
                }, cancellationToken);

                await contextoAplicacion.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return entidadUsuario.Id;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
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
        const string sql = """
            SELECT DISTINCT r.nombre
            FROM usuario_rol ur
            INNER JOIN rol r ON r.id = ur.rol_id
            WHERE ur.usuario_id = @usuario_id
            ORDER BY r.nombre
            """;

        var cadenaConexion = contextoAplicacion.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(cadenaConexion))
            throw new InvalidOperationException("No se encontró la cadena de conexión para obtener roles de usuario.");

        cadenaConexion = SupabaseConnectionStringHelper.Normalizar(cadenaConexion);
        await using var connection = new NpgsqlConnection(cadenaConexion);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 15;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@usuario_id";
        parameter.Value = id;
        command.Parameters.Add(parameter);

        var roles = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
                roles.Add(reader.GetString(0));
        }

        return roles;
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
