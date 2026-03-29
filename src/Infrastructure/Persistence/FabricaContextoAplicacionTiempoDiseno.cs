using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SistemaAranceles.Infrastructure.Persistence;

public sealed class FabricaContextoAplicacionTiempoDiseno : IDesignTimeDbContextFactory<ContextoAplicacion>
{
    public ContextoAplicacion CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContextoAplicacion>();
        optionsBuilder.UseSqlite("Data Source=sistema_aranceles.db");

        return new ContextoAplicacion(optionsBuilder.Options);
    }
}
