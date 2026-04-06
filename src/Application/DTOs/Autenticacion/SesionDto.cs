namespace SistemaAranceles.Application.DTOs.Autenticacion;

public sealed class SesionDto
{
    public int UsuarioId { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string Correo { get; init; } = string.Empty;
    public string RolNombre { get; init; } = string.Empty;
    public string TokenSesion { get; init; } = string.Empty;
}
