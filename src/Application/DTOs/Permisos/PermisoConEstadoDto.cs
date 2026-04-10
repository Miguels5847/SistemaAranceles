namespace SistemaAranceles.Application.DTOs.Permisos;

/// <summary>
/// Permiso con su estado efectivo para un usuario concreto.
/// Usado en la UI de edición de usuario para mostrar checkboxes.
/// </summary>
public sealed class PermisoConEstadoDto
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string ModuloNombre { get; init; } = string.Empty;
    public string AccionNombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;

    /// <summary>True si el permiso proviene del rol base del usuario.</summary>
    public bool EsDeRolBase { get; init; }

    /// <summary>
    /// Override explícito: true = concedido, false = revocado, null = sin override (usa rol base).
    /// </summary>
    public bool? OverrideConcedido { get; init; }

    /// <summary>Acceso efectivo calculado: override si existe, de lo contrario rol base.</summary>
    public bool TieneAcceso => OverrideConcedido.HasValue ? OverrideConcedido.Value : EsDeRolBase;
}
