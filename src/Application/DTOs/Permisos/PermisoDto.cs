namespace SistemaAranceles.Application.DTOs.Permisos;

public sealed class PermisoDto
{
    public int Id { get; init; }
    public string Codigo { get; init; } = string.Empty;
    public string ModuloNombre { get; init; } = string.Empty;
    public string AccionNombre { get; init; } = string.Empty;
    public string Descripcion { get; init; } = string.Empty;
}
