namespace SistemaAranceles.Application.DTOs.TasaRetencion;

public sealed class CrearConfiguracionRetencionDto
{
    public int CarreraId { get; init; }
    public int EscenarioProyeccionId { get; init; }
    public int TotalCiclos { get; init; }
    public decimal TasaRetencionPorcentaje { get; init; }
    public decimal TasaGraduacionPorcentaje { get; init; }
    public decimal EstudiantesPeriodo1 { get; init; }
    public decimal EstudiantesPeriodo2 { get; init; }
    public int ParalelosPeriodo1 { get; init; }
    public int ParalelosPeriodo2 { get; init; }
}
