using Npgsql;

namespace SistemaAranceles.Infrastructure.Persistence;

public static class SupabaseConnectionStringHelper
{
    public static string Normalizar(string cadenaConexion)
    {
        if (string.IsNullOrWhiteSpace(cadenaConexion))
            return cadenaConexion;

        var builder = new NpgsqlConnectionStringBuilder(cadenaConexion);
        if (!EsPoolerSupabase(builder.Host ?? string.Empty))
            return cadenaConexion;

        if (builder.Port == 5432)
            builder.Port = 6543;

        return builder.ConnectionString;
    }

    private static bool EsPoolerSupabase(string host) =>
        host.EndsWith(".pooler.supabase.com", StringComparison.OrdinalIgnoreCase);
}