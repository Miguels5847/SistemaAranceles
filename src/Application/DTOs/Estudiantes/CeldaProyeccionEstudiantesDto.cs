namespace SistemaAranceles.Application.DTOs.Estudiantes;

public sealed record CeldaProyeccionEstudiantesDto(
    int NumeroPeriodo,
    int NumeroCiclo,
    int CantidadParalelos,
    decimal TotalEstudiantes);
