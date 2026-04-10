namespace SistemaAranceles.Application.DTOs.Permisos;

/// <summary>
/// Override de permiso para un usuario: difiere del acceso que le da su rol base.
/// Solo se envían los permisos donde el estado deseado ≠ estado del rol base.
/// </summary>
public sealed class PermisoOverrideDto
{
    public int PermisoId { get; init; }

    /// <summary>True = conceder (aunque el rol no lo tenga). False = revocar (aunque el rol lo tenga).</summary>
    public bool Concedido { get; init; }
}
