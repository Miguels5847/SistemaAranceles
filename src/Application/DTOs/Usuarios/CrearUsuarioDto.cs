namespace SistemaAranceles.Application.DTOs.Usuarios;

public sealed class CrearUsuarioDto
{
    public string NombreCompleto { get; init; } = string.Empty;
    public string CorreoInstitucional { get; init; } = string.Empty;
    public string Contrasena { get; init; } = string.Empty;
    public int RolId { get; init; }
    public string RolNombre { get; init; } = string.Empty;
}
