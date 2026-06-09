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
    public DbSet<UsuarioPermisoOverride> UsuariosPermisosOverride => Set<UsuarioPermisoOverride>();
    public DbSet<SesionUsuario> SesionesUsuario => Set<SesionUsuario>();
    public DbSet<AuditoriaLog> AuditoriasLog => Set<AuditoriaLog>();
    public DbSet<Carrera> Carreras => Set<Carrera>();
    public DbSet<PeriodoAcademico> PeriodosAcademicos => Set<PeriodoAcademico>();
    public DbSet<EscenarioProyeccion> EscenariosProyeccion => Set<EscenarioProyeccion>();
    public DbSet<InflacionAnual> InflacionesAnuales => Set<InflacionAnual>();
    public DbSet<InflacionProyectada> InflacionesProyectadas => Set<InflacionProyectada>();
    public DbSet<ConfiguracionRetencion> ConfiguracionesRetencion => Set<ConfiguracionRetencion>();
    public DbSet<CriterioReferenciaRetencion> CriteriosReferenciaRetencion => Set<CriterioReferenciaRetencion>();
    public DbSet<SimulacionRetencion> SimulacionesRetencion => Set<SimulacionRetencion>();
    public DbSet<DetalleSimulacionRetencion> DetallesSimulacionRetencion => Set<DetalleSimulacionRetencion>();
    public DbSet<ProyeccionEstudiantes> ProyeccionesEstudiantes => Set<ProyeccionEstudiantes>();
    public DbSet<DetalleProyeccionEstudiantes> DetallesProyeccionEstudiantes => Set<DetalleProyeccionEstudiantes>();
    public DbSet<ConfiguracionCargaDocente> ConfiguracionesCargaDocente => Set<ConfiguracionCargaDocente>();
    public DbSet<ProyeccionRequerimientoDocente> ProyeccionesRequerimientoDocente => Set<ProyeccionRequerimientoDocente>();
    public DbSet<CargoFacultad> CargosFacultad => Set<CargoFacultad>();
    public DbSet<ProyeccionCargoFacultad> ProyeccionesCargoFacultad => Set<ProyeccionCargoFacultad>();
    public DbSet<CargoPlantaCentral> CargosPlantaCentral => Set<CargoPlantaCentral>();
    public DbSet<ProyeccionCargoPlantaCentral> ProyeccionesCargoPlantaCentral => Set<ProyeccionCargoPlantaCentral>();
    public DbSet<ConfiguracionArancel> ConfiguracionesArancel => Set<ConfiguracionArancel>();
    public DbSet<PresupuestoInstitucional> PresupuestosInstitucionales => Set<PresupuestoInstitucional>();
    public DbSet<ItemMaterialInsumo> ItemsMaterialInsumo => Set<ItemMaterialInsumo>();
    public DbSet<ProyeccionMaterialInsumo> ProyeccionesMaterialInsumo => Set<ProyeccionMaterialInsumo>();
    public DbSet<ResumenProyeccionFinanciera> ResumenesProyeccionFinanciera => Set<ResumenProyeccionFinanciera>();
    public DbSet<OverrideHorasPeriodo> OverridesHorasPeriodo => Set<OverrideHorasPeriodo>();
    public DbSet<DatosInstitucionales> DatosInstitucionales => Set<DatosInstitucionales>();
    public DbSet<CatalogoActivoBase> CatalogosActivoBase => Set<CatalogoActivoBase>();
    public DbSet<ActivoFijo> ActivosFijos => Set<ActivoFijo>();
    public DbSet<InversionFutura> InversionesFuturas => Set<InversionFutura>();
    public DbSet<ServicioMantenimiento> ServiciosMantenimiento => Set<ServicioMantenimiento>();
    public DbSet<ActivoDiferido> ActivosDiferidos => Set<ActivoDiferido>();
    public DbSet<ConfiguracionArancelCarrera> ConfiguracionesArancelCarrera => Set<ConfiguracionArancelCarrera>();
    public DbSet<DescuentoArancelCiclo> DescuentosArancelCiclo => Set<DescuentoArancelCiclo>();
    public DbSet<RatioMaterialDemanda> RatiosMaterialDemanda => Set<RatioMaterialDemanda>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ContextoAplicacion).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
