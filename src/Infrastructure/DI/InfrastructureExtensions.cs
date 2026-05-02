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
        services.AddTransient<IRepositorioProyeccionCargoFacultad, RepositorioProyeccionCargoFacultad>();
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

        return services;
    }
}
