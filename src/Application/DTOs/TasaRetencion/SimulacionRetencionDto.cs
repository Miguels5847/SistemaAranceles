namespace SistemaAranceles.Application.DTOs.TasaRetencion;

public sealed class SimulacionRetencionDto
{
    public int Id { get; init; }
    public int ConfiguracionRetencionId { get; init; }
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public string CarreraCodigo { get; init; } = string.Empty;
    public int EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public decimal TasaRetencionConfigurada { get; init; }
    public decimal TasaGraduacionConfigurada { get; init; }
    public int CohorteAnio { get; init; }
    public DateTime FechaSimulacion { get; init; }
    public decimal RetencionPorcentajeFinal { get; init; }
    public decimal GraduacionPorcentajeFinal { get; init; }
    public decimal EstudiantesTotalesInicio { get; init; }
    public decimal EstudiantesRetenidos { get; init; }
    public decimal EstudiantesGraduados { get; init; }
    public IReadOnlyList<DetalleSimulacionRetencionDto> Detalles { get; init; } = [];
}
