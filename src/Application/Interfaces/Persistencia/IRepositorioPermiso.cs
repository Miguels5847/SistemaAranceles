using SistemaAranceles.Application.DTOs.Permisos;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioPermiso
{
    /// <summary>
    /// Retorna el conjunto de códigos de permiso efectivos para el usuario.
    /// Aplica: permisos del rol base MINUS overrides revocadores UNION overrides concedidos.
    /// Usado en login para poblar SesionActual.PermisosEfectivos.
    /// </summary>
    Task<IReadOnlySet<string>> ObtenerPermisosEfectivosAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna todos los permisos del sistema con el estado efectivo para el usuario.
    /// Usado en la UI de edición de usuario.
    /// </summary>
    Task<IReadOnlyList<PermisoConEstadoDto>> ObtenerTodosConEstadoParaUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retorna los IDs de permisos base asignados a un rol.
    /// Usado para calcular deltas reales de overrides al cambiar de rol.
    /// </summary>
    Task<IReadOnlySet<int>> ObtenerPermisoIdsDeRolAsync(
        int rolId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reemplaza todos los overrides del usuario.
    /// Solo se guardan los permisos donde el estado deseado difiere del rol base.
    /// </summary>
    Task GuardarOverridesAsync(
        int usuarioId,
        IEnumerable<PermisoOverrideDto> overrides,
        int creadoPorUsuarioId,
        CancellationToken cancellationToken = default);
}
