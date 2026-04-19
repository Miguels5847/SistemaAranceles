namespace SistemaAranceles.Application.DTOs.TasaRetencion;

public sealed class ConfiguracionRetencionDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public string CarreraCodigo { get; init; } = string.Empty;
    public int EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public int TotalCiclos { get; init; }
    public decimal TasaRetencionPorcentaje { get; init; }
    public decimal TasaGraduacionPorcentaje { get; init; }
    public decimal EstudiantesPeriodo1 { get; init; }
    public decimal EstudiantesPeriodo2 { get; init; }
    public int ParalelosPeriodo1 { get; init; }
    public int ParalelosPeriodo2 { get; init; }
    public bool TieneCriterioReferencia { get; init; }
    public decimal? MetaRetencionPorcentaje { get; init; }
    public decimal? MetaGraduacionPorcentaje { get; init; }
}
