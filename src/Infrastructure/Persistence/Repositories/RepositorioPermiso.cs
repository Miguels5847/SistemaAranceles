using Microsoft.EntityFrameworkCore;
using Npgsql;
using SistemaAranceles.Application.DTOs.Permisos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;
using System.Diagnostics;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioPermiso(ContextoAplicacion contextoAplicacion) : IRepositorioPermiso
{
    public async Task<IReadOnlySet<string>> ObtenerPermisosEfectivosAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH rol_base AS (
                SELECT DISTINCT rp.permiso_id
                FROM rol_permiso rp
                INNER JOIN usuario_rol ur ON ur.rol_id = rp.rol_id
                WHERE ur.usuario_id = @usuario_id
            ),
            overrides AS (
                SELECT permiso_id, concedido
                FROM usuario_permiso_override
                WHERE usuario_id = @usuario_id
            )
            SELECT p.codigo
            FROM permiso p
            WHERE p.esta_activo = true
              AND (
                (p.id IN (SELECT permiso_id FROM rol_base)
                 AND p.id NOT IN (SELECT permiso_id FROM overrides WHERE concedido = false))
                OR
                p.id IN (SELECT permiso_id FROM overrides WHERE concedido = true)
              )
            """;

        for (var intento = 1; intento <= 2; intento++)
        {
            try
            {
                var (connection, _) = await ObtenerConexionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = 10;

                var pUsuario = command.CreateParameter();
                pUsuario.ParameterName = "@usuario_id";
                pUsuario.Value = usuarioId;
                command.Parameters.Add(pUsuario);

                var permisos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    if (!reader.IsDBNull(0))
                        permisos.Add(reader.GetString(0));
                }

                Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioPermiso: permisos efectivos cargados para usuarioId={usuarioId}. Total={permisos.Count}.");
                return permisos;
            }
            catch (Exception ex) when (EsTimeoutTransitorio(ex) && intento == 1)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioPermiso: timeout transitorio en ObtenerPermisosEfectivosAsync (intento {intento}). Reintentando.");
            }
        }

        return new HashSet<string>();
    }

    public async Task<IReadOnlyList<PermisoConEstadoDto>> ObtenerTodosConEstadoParaUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            WITH rol_base AS (
                SELECT DISTINCT rp.permiso_id
                FROM rol_permiso rp
                INNER JOIN usuario_rol ur ON ur.rol_id = rp.rol_id
                WHERE ur.usuario_id = @usuario_id
            )
            SELECT
                p.id,
                p.codigo,
                p.modulo_nombre,
                p.accion_nombre,
                p.descripcion,
                CASE WHEN rb.permiso_id IS NOT NULL THEN true ELSE false END AS es_de_rol_base,
                upo.concedido AS override_concedido
            FROM permiso p
            LEFT JOIN rol_base rb ON rb.permiso_id = p.id
            LEFT JOIN usuario_permiso_override upo
                ON upo.usuario_id = @usuario_id AND upo.permiso_id = p.id
            WHERE p.esta_activo = true
            ORDER BY p.modulo_nombre, p.accion_nombre
            """;

        for (var intento = 1; intento <= 2; intento++)
        {
            try
            {
                var (connection, _) = await ObtenerConexionAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = 15;

                var pUsuario = command.CreateParameter();
                pUsuario.ParameterName = "@usuario_id";
                pUsuario.Value = usuarioId;
                command.Parameters.Add(pUsuario);

                var lista = new List<PermisoConEstadoDto>();

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    bool? overrideConcedido = reader.IsDBNull(6) ? null : reader.GetBoolean(6);

                    lista.Add(new PermisoConEstadoDto
                    {
                        Id = reader.GetInt32(0),
                        Codigo = reader.GetString(1),
                        ModuloNombre = reader.GetString(2),
                        AccionNombre = reader.GetString(3),
                        Descripcion = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        EsDeRolBase = reader.GetBoolean(5),
                        OverrideConcedido = overrideConcedido
                    });
                }

                Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioPermiso: permisos con estado cargados para usuarioId={usuarioId}. Total={lista.Count}.");
                return lista;
            }
            catch (Exception ex) when (EsTimeoutTransitorio(ex) && intento == 1)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioPermiso: timeout transitorio en ObtenerTodosConEstadoParaUsuarioAsync (intento {intento}). Reintentando.");
            }
        }

        return [];
    }

    public async Task GuardarOverridesAsync(
        int usuarioId,
        IEnumerable<PermisoOverrideDto> overrides,
        int creadoPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        // Si ya hay una transacción activa (iniciada por el use case coordinador), no crear una nueva.
        var gestionarTransaccion = contextoAplicacion.Database.CurrentTransaction == null;
        Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? transaction = null;

        if (gestionarTransaccion)
            transaction = await contextoAplicacion.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var existentes = contextoAplicacion.UsuariosPermisosOverride
                .Where(x => x.UsuarioId == usuarioId)
                .ToList();
            contextoAplicacion.UsuariosPermisosOverride.RemoveRange(existentes);

            var listaOverrides = overrides.ToList();
            if (listaOverrides.Count > 0)
            {
                var creadoEn = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");
                foreach (var ov in listaOverrides)
                {
                    contextoAplicacion.UsuariosPermisosOverride.Add(new UsuarioPermisoOverride
                    {
                        UsuarioId = usuarioId,
                        PermisoId = ov.PermisoId,
                        Concedido = ov.Concedido,
                        CreadoEn = creadoEn,
                        CreadoPorUsuarioId = creadoPorUsuarioId > 0 ? creadoPorUsuarioId : null
                    });
                }
            }

            await contextoAplicacion.SaveChangesAsync(cancellationToken);

            if (gestionarTransaccion)
                await transaction!.CommitAsync(cancellationToken);

            Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioPermiso: overrides guardados para usuarioId={usuarioId}. Total={listaOverrides.Count}.");
        }
        catch (Exception ex)
        {
            if (gestionarTransaccion && transaction is not null)
                await transaction.RollbackAsync(CancellationToken.None);
            Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioPermiso: error en GuardarOverridesAsync -> {ex.Message}. Rollback={gestionarTransaccion}.");
            throw;
        }
        finally
        {
            if (transaction is not null)
                await transaction.DisposeAsync();
        }
    }

    private static bool EsTimeoutTransitorio(Exception ex)
    {
        if (ex is TimeoutException || ex is OperationCanceledException)
            return true;

        if (ex is NpgsqlException npgsqlEx)
        {
            if (npgsqlEx.InnerException is TimeoutException)
                return true;
            if (npgsqlEx.Message.Contains("Timeout", StringComparison.OrdinalIgnoreCase))
                return true;
            if (npgsqlEx.Message.Contains("stream", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return ex.Message.Contains("stream", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("transient", StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(NpgsqlConnection Connection, bool FueAbiertaAqui)> ObtenerConexionAsync(
        CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
        var fueAbiertaAqui = connection.State != System.Data.ConnectionState.Open;
        if (fueAbiertaAqui)
            await connection.OpenAsync(cancellationToken);
        return (connection, fueAbiertaAqui);
    }
}
