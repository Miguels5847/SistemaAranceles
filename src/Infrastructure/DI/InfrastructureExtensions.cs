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
        services.AddDbContext<ContextoAplicacion>(options =>
            options.UseNpgsql(cadenaConexion));

        // Repositorios
        services.AddScoped(typeof(IRepositorioGenerico<>), typeof(RepositorioGenerico<>));
        services.AddScoped<IRepositorioUsuario, RepositorioUsuario>();
        services.AddScoped<IRepositorioCarrera, RepositorioCarrera>();
        services.AddScoped<IRepositorioRol, RepositorioRol>();
        services.AddScoped<IRepositorioSesionUsuario, RepositorioSesionUsuario>();
        services.AddScoped<IRepositorioAuditoriaLog, RepositorioAuditoriaLog>();
        services.AddScoped<IUnidadTrabajo, UnidadTrabajo>();

        // Servicios
        services.AddScoped<IServicioHash, ServicioHash>();
        services.AddScoped<IAuditoriaServicio, ServicioAuditoria>();

        return services;
    }
}
