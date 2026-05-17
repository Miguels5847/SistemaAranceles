namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class GuardarProyeccionCargoFacultadDto
{
    public int? Id { get; init; }
    public int CargoFacultadId { get; init; }
    public int PeriodoAcademicoId { get; init; }
    public decimal CantidadPersonas { get; init; }
    public decimal EstudiantesCarreraPeriodo { get; init; }
    public decimal EstudiantesUnidadAcademica { get; init; }
    public decimal FactorInflacion { get; init; } = 1m;
    public decimal ValorBaseDecimoCuartoSemestral { get; init; } = 450m;
}