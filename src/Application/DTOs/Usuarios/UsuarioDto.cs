namespace SistemaAranceles.Application.DTOs.Usuarios;

public sealed class UsuarioDto
{
    public int Id { get; init; }
    public string NombreCompleto { get; init; } = string.Empty;
    public string CorreoInstitucional { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public DateTime? UltimoAccesoEn { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}
