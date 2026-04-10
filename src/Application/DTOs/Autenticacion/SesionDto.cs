namespace SistemaAranceles.Application.DTOs.Autenticacion;

public sealed class SesionDto
{
    public int UsuarioId { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string RolNombre { get; init; } = string.Empty;
    public string TokenSesion { get; init; } = string.Empty;

    /// <summary>
    /// Códigos de permiso efectivos cargados al login.
    /// Vacío si la carga falló (el sistema usará fallback por rol).
    /// </summary>
    public IReadOnlySet<string> PermisosEfectivos { get; init; } = new HashSet<string>();
}
