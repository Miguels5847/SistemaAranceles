namespace SistemaAranceles.Application.DTOs.Inflacion;

public sealed class ImportacionInflacionErrorDto
{
    public int Fila { get; init; }
    public string Mensaje { get; init; } = string.Empty;
    public string? Valor { get; init; }
}
