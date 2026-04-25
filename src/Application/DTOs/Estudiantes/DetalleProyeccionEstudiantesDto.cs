namespace SistemaAranceles.Application.DTOs.Estudiantes;

public sealed class DetalleProyeccionEstudiantesDto
{
    public int Id { get; init; }
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public int NumeroCiclo { get; init; }
    public int CantidadParalelos { get; init; }
    public decimal TotalEstudiantes { get; init; }
}
