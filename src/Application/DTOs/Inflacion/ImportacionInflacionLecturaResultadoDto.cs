namespace SistemaAranceles.Application.DTOs.Inflacion;

public sealed class ImportacionInflacionLecturaResultadoDto
{
    public IReadOnlyList<ImportacionInflacionFilaDto> Filas { get; init; } = [];
    public IReadOnlyList<ImportacionInflacionErrorDto> Errores { get; init; } = [];
}
