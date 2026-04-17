namespace SistemaAranceles.Application.DTOs.Inflacion;

public sealed class ImportacionInflacionResultadoDto
{
    public int TotalFilasProcesadas { get; init; }
    public int FilasCorrectas { get; init; }
    public int FilasOmitidas { get; init; }
    public int FilasConError { get; init; }
    public IReadOnlyList<ImportacionInflacionErrorDto> DetalleErrores { get; init; } = [];
}
