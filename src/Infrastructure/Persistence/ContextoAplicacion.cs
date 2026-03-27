using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence;

public sealed class ContextoAplicacion(DbContextOptions<ContextoAplicacion> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Permiso> Permisos => Set<Permiso>();
    public DbSet<RolPermiso> RolesPermisos => Set<RolPermiso>();
    public DbSet<UsuarioRol> UsuariosRoles => Set<UsuarioRol>();
    public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();
    public DbSet<AuditoriaLog> AuditoriasLog => Set<AuditoriaLog>();
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<PeriodoAcademico> PeriodosAcademicos => Set<PeriodoAcademico>();
    public DbSet<EscenarioProyeccion> EscenariosProyeccion => Set<EscenarioProyeccion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextoAplicacion).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
