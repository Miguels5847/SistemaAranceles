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
        const string sql = """
            SELECT r.id, r.nombre, r.descripcion
            FROM rol r
            WHERE lower(coalesce(r.esta_activo::text, '0')) IN ('1', 't', 'true')
            ORDER BY r.nombre
            """;

        await using var connection = await ObtenerConexionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 10;

        var roles = new List<(int Id, string Nombre, string Descripcion)>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var descripcion = await reader.IsDBNullAsync(2, cancellationToken) ? string.Empty : reader.GetString(2);
            roles.Add((reader.GetInt32(0), reader.GetString(1), descripcion));
        }

        return roles;
    }

    public async Task<(int Id, string Nombre)?> ObtenerPorNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT r.id, r.nombre
            FROM rol r
            WHERE r.nombre = @nombre
              AND lower(coalesce(r.esta_activo::text, '0')) IN ('1', 't', 'true')
            LIMIT 1
            """;

        await using var connection = await ObtenerConexionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 10;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@nombre";
        parameter.Value = nombre;
        command.Parameters.Add(parameter);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
            return (reader.GetInt32(0), reader.GetString(1));

        return null;
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

    public async Task<int?> AsignarRolAUsuarioPorCorreoAsync(
        string correoInstitucional,
        int rolId,
        CancellationToken cancellationToken = default)
    {
        var correoNormalizado = correoInstitucional.Trim().ToLowerInvariant();

        try
        {
            // Prefer EF path on the existing DbContext to avoid extra connection overhead.
            var usuarioIdEf = await contextoAplicacion.Usuarios
                .AsNoTracking()
                .Where(u => u.CorreoInstitucional == correoNormalizado)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (usuarioIdEf > 0)
            {
                await AsignarRolAUsuarioAsync(usuarioIdEf, rolId, cancellationToken);
                return usuarioIdEf;
            }
        }
        catch (Exception ex) when (EsTimeoutTransitorio(ex))
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioRol: timeout en ruta EF para AsignarRolAUsuarioPorCorreoAsync. Se intentará SQL.");
        }

        const string sql = """
            WITH usuario_objetivo AS (
                SELECT u.id
                FROM usuario u
                WHERE lower(btrim(u.correo_institucional)) = lower(btrim(@correo))
                LIMIT 1
            ), insercion AS (
                INSERT INTO usuario_rol (usuario_id, rol_id)
                SELECT u.id, @rol_id
                FROM usuario_objetivo u
                ON CONFLICT DO NOTHING
                RETURNING usuario_id
            )
            SELECT COALESCE(
                (SELECT i.usuario_id FROM insercion i LIMIT 1),
                (SELECT u.id FROM usuario_objetivo u LIMIT 1)
            )
            """;

        for (var intento = 1; intento <= 2; intento++)
        {
            try
            {
                await using var connection = await ObtenerConexionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = 25;

                var pCorreo = command.CreateParameter();
                pCorreo.ParameterName = "@correo";
                pCorreo.Value = correoInstitucional;
                command.Parameters.Add(pCorreo);

                var pRol = command.CreateParameter();
                pRol.ParameterName = "@rol_id";
                pRol.Value = rolId;
                command.Parameters.Add(pRol);

                var result = await command.ExecuteScalarAsync(cancellationToken);
                if (result is null || result == DBNull.Value)
                    continue;

                return Convert.ToInt32(result);
            }
            catch (Exception ex) when (EsTimeoutTransitorio(ex) && intento == 1)
            {
                Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioRol: timeout transitorio en AsignarRolAUsuarioPorCorreoAsync (intento {intento}). Reintentando.");
            }
            catch (Exception ex) when (EsTimeoutTransitorio(ex) && intento == 2)
            {
                Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioRol: timeout persistente en AsignarRolAUsuarioPorCorreoAsync. Fallback por EF.");
                return await AsignarRolConFallbackEfAsync(correoNormalizado, rolId, cancellationToken);
            }
        }

        return await AsignarRolConFallbackEfAsync(correoNormalizado, rolId, cancellationToken);
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

        await using var connection = await ObtenerConexionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 15;

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

    private static bool EsTimeoutTransitorio(Exception ex)
    {
        if (ex is TimeoutException)
            return true;

        if (ex is NpgsqlException npgsqlEx)
        {
            if (npgsqlEx.InnerException is TimeoutException)
                return true;

            if (npgsqlEx.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private async Task<int?> AsignarRolConFallbackEfAsync(
        string correoNormalizado,
        int rolId,
        CancellationToken cancellationToken)
    {
        var usuarioId = await contextoAplicacion.Usuarios
            .AsNoTracking()
            .Where(u => u.CorreoInstitucional.ToLower() == correoNormalizado)
            .Select(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (usuarioId <= 0)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioRol: fallback EF no encontró usuario por correo '{correoNormalizado}'.");
            return null;
        }

        await AsignarRolAUsuarioAsync(usuarioId, rolId, cancellationToken);
        return usuarioId;
    }

    // Usa una conexión dedicada por operación para aislar fallos de stream intermitentes.
    private async Task<NpgsqlConnection> ObtenerConexionAsync(
        CancellationToken cancellationToken)
    {
        var cadenaConexion = contextoAplicacion.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(cadenaConexion))
            throw new InvalidOperationException("No se encontró la cadena de conexión para consultas de roles.");

        cadenaConexion = SupabaseConnectionStringHelper.Normalizar(cadenaConexion);
        var connection = new NpgsqlConnection(cadenaConexion);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
