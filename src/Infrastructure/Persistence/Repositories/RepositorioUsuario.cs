using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Domain.ValueObjects;
using System.Globalization;
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
        const string sql = """
            SELECT id, nombre_completo, correo_institucional, hash_contrasena, estado, ultimo_acceso_en
            FROM usuario
            ORDER BY id
            """;

        var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 15;

        var usuarios = new List<UsuarioDominio>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = reader.GetInt32(0);
            var nombreCompleto = reader.GetString(1);
            var correo = reader.GetString(2);
            var hash = reader.GetString(3);
            var estado = reader.GetString(4);

            DateTime? ultimoAcceso = null;
            if (!await reader.IsDBNullAsync(5, cancellationToken))
            {
                var ultimoAccesoTexto = reader.GetString(5);
                if (DateTime.TryParse(ultimoAccesoTexto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                    ultimoAcceso = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            usuarios.Add(MapearADominio(id, nombreCompleto, correo, hash, estado, ultimoAcceso));
        }

        return usuarios;
    }

    public async Task<UsuarioDominio?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, nombre_completo, correo_institucional, hash_contrasena, estado, ultimo_acceso_en
            FROM usuario
            WHERE id = @id
            """;

        var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 15;

        var pId = command.CreateParameter();
        pId.ParameterName = "@id";
        pId.Value = id;
        command.Parameters.Add(pId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var idUsuario = reader.GetInt32(0);
            var nombreCompleto = reader.GetString(1);
            var correo = reader.GetString(2);
            var hash = reader.GetString(3);
            var estado = reader.GetString(4);

            DateTime? ultimoAcceso = null;
            if (!await reader.IsDBNullAsync(5, cancellationToken))
            {
                var ultimoAccesoTexto = reader.GetString(5);
                if (DateTime.TryParse(ultimoAccesoTexto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                    ultimoAcceso = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            return MapearADominio(idUsuario, nombreCompleto, correo, hash, estado, ultimoAcceso);
        }

        return null;
    }

    public async Task<UsuarioDominio?> ObtenerPorCorreoInstitucionalAsync(string correoInstitucional, CancellationToken cancellationToken = default)
    {
        var correo = correoInstitucional.Trim().ToLowerInvariant();

        const string sql = """
            SELECT id, nombre_completo, correo_institucional, hash_contrasena, estado, ultimo_acceso_en
            FROM usuario
            WHERE lower(correo_institucional) = @correo
            """;

        var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 15;

        var pCorreo = command.CreateParameter();
        pCorreo.ParameterName = "@correo";
        pCorreo.Value = correo;
        command.Parameters.Add(pCorreo);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var idUsuario = reader.GetInt32(0);
            var nombreCompleto = reader.GetString(1);
            var correoLeido = reader.GetString(2);
            var hash = reader.GetString(3);
            var estado = reader.GetString(4);

            DateTime? ultimoAcceso = null;
            if (!await reader.IsDBNullAsync(5, cancellationToken))
            {
                var ultimoAccesoTexto = reader.GetString(5);
                if (DateTime.TryParse(ultimoAccesoTexto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
                    ultimoAcceso = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            }

            return MapearADominio(idUsuario, nombreCompleto, correoLeido, hash, estado, ultimoAcceso);
        }

        return null;
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
            // INSERT directo con RETURNING para: (a) evitar mismatch bool→integer en esta_activo,
            // (b) obtener el id real generado por la BD (EF no rehidrata la entidad tras SaveChanges).
            var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
            var dbTx = (NpgsqlTransaction)contextoAplicacion.Database.CurrentTransaction!.GetDbTransaction();

            int usuarioId;
            await using (var cmd = connection.CreateCommand())
            {
                cmd.Transaction = dbTx;
                cmd.CommandTimeout = 15;
                cmd.CommandText = """
                    INSERT INTO usuario
                        (nombre_completo, correo_institucional, hash_contrasena, estado, creado_en, esta_activo)
                    VALUES
                        (@nombre, @correo, @hash, @estado, @creadoEn, TRUE)
                    RETURNING id
                    """;
                cmd.Parameters.AddWithValue("@nombre", usuario.NombreCompleto);
                cmd.Parameters.AddWithValue("@correo", usuario.CorreoInstitucional.ToString());
                cmd.Parameters.AddWithValue("@hash", usuario.HashContrasena);
                cmd.Parameters.AddWithValue("@estado", usuario.Estado.ToString());
                cmd.Parameters.AddWithValue("@creadoEn", DateTime.UtcNow);

                var scalar = await cmd.ExecuteScalarAsync(cancellationToken);
                usuarioId = Convert.ToInt32(scalar);
            }

            var existeRol = await contextoAplicacion.UsuariosRoles
                .AnyAsync(x => x.UsuarioId == usuarioId && x.RolId == rolId, cancellationToken);

            if (!existeRol)
            {
                await contextoAplicacion.UsuariosRoles.AddAsync(new Infrastructure.Persistence.Entidades.UsuarioRol
                {
                    UsuarioId = usuarioId,
                    RolId = rolId
                }, cancellationToken);

                await contextoAplicacion.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return usuarioId;
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
                .SetProperty(x => x.EliminadoEn, DateTime.UtcNow)
                .SetProperty(x => x.Estado, EstadoUsuario.Inactivo.ToString()), cancellationToken);

        if (filas == 0)
        {
            throw new KeyNotFoundException($"No se encontro el usuario con Id {id}.");
        }
    }

    public async Task EliminarDefinitivamenteAsync(int id, CancellationToken cancellationToken = default)
    {
        var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var transaction = await contextoAplicacion.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await contextoAplicacion.AuditoriasLog
                .Where(x => x.EjecutadoPorUsuarioId == id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.EjecutadoPorUsuarioId, (int?)null), cancellationToken);

            await contextoAplicacion.SesionesUsuario
                .Where(x => x.UsuarioId == id)
                .ExecuteDeleteAsync(cancellationToken);

            await contextoAplicacion.UsuariosRoles
                .Where(x => x.UsuarioId == id)
                .ExecuteDeleteAsync(cancellationToken);

            var filas = await contextoAplicacion.Usuarios
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            if (filas == 0)
            {
                throw new KeyNotFoundException($"No se encontro el usuario con Id {id}.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RegistrarUltimoAccesoAsync(
        int id,
        DateTime ultimoAccesoEn,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE usuario
            SET ultimo_acceso_en = @ultimo_acceso_en,
                actualizado_en = @actualizado_en
            WHERE id = @id
            """;

        var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 8;

        var pId = command.CreateParameter();
        pId.ParameterName = "@id";
        pId.Value = id;
        command.Parameters.Add(pId);

        var pUltimoAcceso = command.CreateParameter();
        pUltimoAcceso.ParameterName = "@ultimo_acceso_en";
        pUltimoAcceso.Value = ultimoAccesoEn.ToUniversalTime().ToString("O");
        command.Parameters.Add(pUltimoAcceso);

        var pActualizado = command.CreateParameter();
        pActualizado.ParameterName = "@actualizado_en";
        pActualizado.Value = DateTime.UtcNow.ToString("O");
        command.Parameters.Add(pActualizado);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ObtenerRolesDelUsuarioAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var rolesPorUsuario = await ObtenerRolesPorUsuariosAsync([id], cancellationToken);
        return rolesPorUsuario.TryGetValue(id, out var roles) ? roles : [];
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<string>>> ObtenerRolesPorUsuariosAsync(
        IEnumerable<int> usuarioIds,
        CancellationToken cancellationToken = default)
    {
        var ids = usuarioIds
            .Distinct()
            .Where(x => x > 0)
            .ToArray();

        if (ids.Length == 0)
            return new Dictionary<int, IReadOnlyList<string>>();

        const string sql = """
            SELECT ur.usuario_id, r.nombre
            FROM usuario_rol ur
            INNER JOIN rol r ON r.id = ur.rol_id
            WHERE ur.usuario_id = ANY(@usuario_ids)
            ORDER BY ur.usuario_id, r.nombre
            """;

        var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 8;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@usuario_ids";
        parameter.Value = ids;
        command.Parameters.Add(parameter);

        var rolesPorUsuario = new Dictionary<int, List<string>>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (await reader.IsDBNullAsync(0, cancellationToken) || await reader.IsDBNullAsync(1, cancellationToken))
                continue;

            var usuarioId = reader.GetInt32(0);
            var rol = reader.GetString(1);

            if (!rolesPorUsuario.TryGetValue(usuarioId, out var lista))
            {
                lista = [];
                rolesPorUsuario[usuarioId] = lista;
            }

            lista.Add(rol);
        }

        return rolesPorUsuario.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<string>)kvp.Value);
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
