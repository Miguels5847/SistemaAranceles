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

        // KAN-49: la BD es siempre Postgres (Supabase); el fallback SQLite era de la época
        // KAN-03 y se eliminó junto con su paquete. Sin cadena real, `dotnet ef migrations add`
        // sigue funcionando (no conecta); solo `database update` la necesita.
        // Cadena ficticia sin credenciales: solo da forma al proveedor en tiempo de diseño.
        optionsBuilder.UseNpgsql(string.IsNullOrWhiteSpace(cadenaConexionPostgres)
            ? "Host=localhost;Database=sistema_aranceles"
            : SupabaseConnectionStringHelper.Normalizar(cadenaConexionPostgres));

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
