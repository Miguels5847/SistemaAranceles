using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;
using Npgsql;
using System.Diagnostics;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioSesionUsuario(ContextoAplicacion contextoAplicacion) : IRepositorioSesionUsuario
{
    public async Task<int> CrearAsync(
        int usuarioId,
        string tokenSesion,
        DateTime expiraEn,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO sesion_usuario (usuario_id, token_sesion, emitido_en, expira_en)
            VALUES (@usuario_id, @token_sesion, @emitido_en, @expira_en)
            RETURNING id
            """;

        var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.CommandTimeout = 8;

        var pUsuarioId = command.CreateParameter();
        pUsuarioId.ParameterName = "@usuario_id";
        pUsuarioId.Value = usuarioId;
        command.Parameters.Add(pUsuarioId);

        var pToken = command.CreateParameter();
        pToken.ParameterName = "@token_sesion";
        pToken.Value = tokenSesion;
        command.Parameters.Add(pToken);

        var pEmitido = command.CreateParameter();
        pEmitido.ParameterName = "@emitido_en";
        pEmitido.Value = DateTime.UtcNow;
        command.Parameters.Add(pEmitido);

        var pExpira = command.CreateParameter();
        pExpira.ParameterName = "@expira_en";
        pExpira.Value = expiraEn;
        command.Parameters.Add(pExpira);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
    }

    public async Task RevocarAsync(string tokenSesion, CancellationToken cancellationToken = default)
    {
        var connection = (NpgsqlConnection)contextoAplicacion.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE sesion_usuario
            SET revocado_en = NOW()
            WHERE token_sesion = @token_sesion
              AND revocado_en IS NULL
            """;
        command.CommandTimeout = 8;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@token_sesion";
        parameter.Value = tokenSesion;
        command.Parameters.Add(parameter);

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        Trace.WriteLine($"[{DateTime.UtcNow:O}] RepositorioSesionUsuario: revocación ejecutada. Filas={rows}.");
    }

    public async Task RevocarTodasDelUsuarioAsync(int usuarioId, CancellationToken cancellationToken = default)
    {
        await contextoAplicacion.SesionesUsuario
            .Where(s => s.UsuarioId == usuarioId && s.RevocadoEn == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(s => s.RevocadoEn, DateTime.UtcNow),
                cancellationToken);
    }
}
