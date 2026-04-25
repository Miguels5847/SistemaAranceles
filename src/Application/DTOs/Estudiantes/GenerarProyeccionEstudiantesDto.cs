namespace SistemaAranceles.Application.DTOs.Estudiantes;

public sealed class GenerarProyeccionEstudiantesDto
{
    public int CarreraId { get; init; }
    public int EscenarioProyeccionId { get; init; }
    public int SimulacionRetencionId { get; init; }
    public int SemanasPorSemestre { get; init; } = 16;
}
