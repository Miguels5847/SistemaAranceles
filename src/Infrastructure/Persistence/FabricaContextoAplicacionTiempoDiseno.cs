using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SistemaAranceles.Infrastructure.Persistence;

public sealed class FabricaContextoAplicacionTiempoDiseno : IDesignTimeDbContextFactory<ContextoAplicacion>
{
    public ContextoAplicacion CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContextoAplicacion>();
        var cadenaConexionPostgres = ObtenerCadenaConexion();

        if (!string.IsNullOrWhiteSpace(cadenaConexionPostgres))
        {
            optionsBuilder.UseNpgsql(SupabaseConnectionStringHelper.Normalizar(cadenaConexionPostgres));
        }
        else
        {
            optionsBuilder.UseSqlite("Data Source=sistema_aranceles.db");
        }

        return new ContextoAplicacion(optionsBuilder.Options);
    }

    private static string? ObtenerCadenaConexion()
    {
        var cadenaPorVariable = Environment.GetEnvironmentVariable("SUPABASE_DB_CONNECTION");
        if (!string.IsNullOrWhiteSpace(cadenaPorVariable))
        {
            return cadenaPorVariable;
        }

        var basePath = ObtenerRutaBaseConfiguracion();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Local.json", optional: true)
            .Build();

        return configuration.GetConnectionString("DefaultConnection");
    }

    private static string ObtenerRutaBaseConfiguracion()
    {
        var current = Directory.GetCurrentDirectory();
        var presentation = Path.Combine(current, "src", "Presentation");

        if (Directory.Exists(presentation))
        {
            return presentation;
        }

        return current;
    }
}
