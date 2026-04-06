namespace SistemaAranceles.Application.DTOs.Usuarios;

public sealed class ActualizarUsuarioDto
{
    public int Id { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string CorreoInstitucional { get; init; } = string.Empty;
    public string? NuevaContrasena { get; init; }
    public string RolNombre { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
}
