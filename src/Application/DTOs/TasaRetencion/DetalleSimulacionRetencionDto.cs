namespace SistemaAranceles.Application.DTOs.TasaRetencion;

public sealed class DetalleSimulacionRetencionDto
{
    public int Id { get; init; }
    public int Ciclo { get; init; }
    public decimal EstudiantesInicio { get; init; }
    public decimal EstudiantesRetenidos { get; init; }
    public decimal EstudiantesReprobados { get; init; }
    public decimal EstudiantesGraduados { get; init; }
    public int AnioAcademico { get; init; }
    public decimal TasaRetencionCicloPorcentaje { get; init; }
    public decimal TasaGraduacionCicloPorcentaje { get; init; }
}
