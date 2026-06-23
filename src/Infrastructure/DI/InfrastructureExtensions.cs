using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Infrastructure.Persistence;
using SistemaAranceles.Infrastructure.Persistence.Repositories;
using SistemaAranceles.Infrastructure.Servicios;

namespace SistemaAranceles.Infrastructure.DI;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string cadenaConexion)
    {
        services.AddDbContext<ContextoAplicacion>(
            options => options.UseNpgsql(cadenaConexion),
            contextLifetime: ServiceLifetime.Scoped,
            optionsLifetime: ServiceLifetime.Singleton);

        // Caché de datos de referencia (B.1): singleton compartido por todos los scopes/repos.
        services.AddSingleton<Persistence.CacheReferencia>();

        // Repositorios
        services.AddTransient(typeof(IRepositorioGenerico<>), typeof(RepositorioGenerico<>));
        services.AddTransient<IRepositorioUsuario, RepositorioUsuario>();
        services.AddTransient<IRepositorioCarrera, RepositorioCarrera>();
        services.AddTransient<IRepositorioInflacionAnual, RepositorioInflacionAnual>();
        services.AddTransient<IRepositorioConfiguracionRetencion, RepositorioConfiguracionRetencion>();
        services.AddTransient<IRepositorioCriterioReferenciaRetencion, RepositorioCriterioReferenciaRetencion>();
        services.AddTransient<IRepositorioSimulacionRetencion, RepositorioSimulacionRetencion>();
        services.AddTransient<IRepositorioDetalleSimulacionRetencion, RepositorioDetalleSimulacionRetencion>();
        services.AddTransient<IRepositorioEscenarioProyeccion, RepositorioEscenarioProyeccion>();
        services.AddTransient<IRepositorioProyeccionEstudiantes, RepositorioProyeccionEstudiantes>();
        services.AddTransient<IRepositorioCargoFacultad, RepositorioCargoFacultad>();
        services.AddTransient<IRepositorioOverrideHorasPeriodo, RepositorioOverrideHorasPeriodo>();
        services.AddTransient<IRepositorioProyeccionCargoFacultad, RepositorioProyeccionCargoFacultad>();
        services.AddTransient<IRepositorioCargoPlantaCentral, RepositorioCargoPlantaCentral>();
        services.AddTransient<IRepositorioProyeccionCargoPlantaCentral, RepositorioProyeccionCargoPlantaCentral>();
        services.AddTransient<IRepositorioDatosInstitucionales, RepositorioDatosInstitucionales>();
        services.AddTransient<IRepositorioCatalogoActivoBase, RepositorioCatalogoActivoBase>();
        services.AddTransient<IRepositorioActivoFijo, RepositorioActivoFijo>();
        services.AddTransient<IRepositorioInversionFutura, RepositorioInversionFutura>();
        services.AddTransient<IRepositorioServicioMantenimiento, RepositorioServicioMantenimiento>();
        services.AddTransient<IRepositorioActivoDiferido, RepositorioActivoDiferido>();
        services.AddTransient<IRepositorioItemMaterialInsumo, RepositorioItemMaterialInsumo>();
        services.AddTransient<IRepositorioConfiguracionArancelCarrera, RepositorioConfiguracionArancelCarrera>();
        services.AddTransient<IRepositorioDescuentoArancelCiclo, RepositorioDescuentoArancelCiclo>();
        services.AddTransient<IRepositorioRatioMaterialDemanda, RepositorioRatioMaterialDemanda>();
        services.AddTransient<IRepositorioPeriodoAcademico, RepositorioPeriodoAcademico>();
        services.AddTransient<IRepositorioRol, RepositorioRol>();
        services.AddTransient<IRepositorioPermiso, RepositorioPermiso>();
        services.AddTransient<IRepositorioSesionUsuario, RepositorioSesionUsuario>();
        services.AddTransient<IRepositorioAuditoriaLog, RepositorioAuditoriaLog>();
        services.AddTransient<IUnidadTrabajo, UnidadTrabajo>();

        // Servicios
        services.AddTransient<IServicioHash, ServicioHash>();
        services.AddTransient<IAuditoriaServicio, ServicioAuditoria>();
        services.AddTransient<IServicioImportacionExcel, ServicioImportacionExcel>();
        services.AddTransient<IServicioImportacionBceArchivo, ServicioImportacionBceArchivo>();
        services.AddTransient<IServicioProyeccion, ServicioProyeccion>();
        services.AddTransient<IServicioEstudiantesTotales, ServicioEstudiantesTotales>();
        services.AddTransient<IServicioDocentesTotales, ServicioDocentesTotales>();
        services.AddTransient<IServicioExportacionPdf, Export.ServicioExportacionPdfQuestPdf>();
        services.AddTransient<IServicioExportacionXlsx, Export.ServicioExportacionXlsxClosedXml>();

        return services;
    }
}
