namespace SistemaAranceles.Application.DTOs.Estudiantes;

public sealed class ProyeccionEstudiantesDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public string CarreraCodigo { get; init; } = string.Empty;
    public int EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public int AnioBase { get; init; }
    public int SemanasPorSemestre { get; init; }
    public DateTime CreadoEn { get; init; }
    public DateTime? ActualizadoEn { get; init; }
    public IReadOnlyList<DetalleProyeccionEstudiantesDto> Detalles { get; init; } = [];
}
