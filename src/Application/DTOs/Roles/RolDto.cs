namespace SistemaAranceles.Application.DTOs.Roles;

public sealed class RolDto
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
}
