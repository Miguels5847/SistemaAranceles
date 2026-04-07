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
        var sesion = new SesionUsuario
        {
            UsuarioId = usuarioId,
            TokenSesion = tokenSesion,
            EmitidoEn = DateTime.UtcNow,
            ExpiraEn = expiraEn
        };

        await contextoAplicacion.SesionesUsuario.AddAsync(sesion, cancellationToken);
        await contextoAplicacion.SaveChangesAsync(cancellationToken);
        return sesion.Id;
    }

    public async Task RevocarAsync(string tokenSesion, CancellationToken cancellationToken = default)
    {
        var cadenaConexion = contextoAplicacion.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(cadenaConexion))
            throw new InvalidOperationException("No se encontró la cadena de conexión para revocar sesión.");

        cadenaConexion = SupabaseConnectionStringHelper.Normalizar(cadenaConexion);

        await using var connection = new NpgsqlConnection(cadenaConexion);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE sesion_usuario
            SET revocado_en = NOW()
            WHERE token_sesion = @token_sesion
              AND revocado_en IS NULL
            """;
        command.CommandTimeout = 2;

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
