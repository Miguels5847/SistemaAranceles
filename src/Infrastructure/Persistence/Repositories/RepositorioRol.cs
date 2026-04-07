using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;
using System.Diagnostics;
using Npgsql;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioRol(ContextoAplicacion contextoAplicacion) : IRepositorioRol
{
    public async Task<IReadOnlyList<(int Id, string Nombre, string Descripcion)>> ListarAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await contextoAplicacion.Roles
            .AsNoTracking()
            .Where(r => r.EstaActivo)
            .Select(r => new { r.Id, r.Nombre, r.Descripcion })
            .ToListAsync(cancellationToken);

        return roles
            .Select(r => (r.Id, r.Nombre, r.Descripcion))
            .ToList();
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
        Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioRol: consultando roles para usuarioId={usuarioId}.");

        const string sql = """
            SELECT DISTINCT r.nombre
            FROM usuario_rol ur
            INNER JOIN rol r ON r.id = ur.rol_id
            WHERE ur.usuario_id = @usuario_id
            """;

        var cadenaConexion = contextoAplicacion.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(cadenaConexion))
            throw new InvalidOperationException("No se encontró la cadena de conexión para leer roles de usuario.");

        cadenaConexion = SupabaseConnectionStringHelper.Normalizar(cadenaConexion);

        await using var connection = new NpgsqlConnection(cadenaConexion);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 2;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@usuario_id";
        parameter.Value = usuarioId;
        command.Parameters.Add(parameter);

        var roles = new List<string>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (!reader.IsDBNull(0))
            {
                roles.Add(reader.GetString(0));
            }
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioRol: roles obtenidos para usuarioId={usuarioId}. Total={roles.Count}.");

        return roles;
    }
}
