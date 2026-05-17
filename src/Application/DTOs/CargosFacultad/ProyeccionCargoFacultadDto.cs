namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class ProyeccionCargoFacultadDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public int CargoFacultadId { get; init; }
    public int PeriodoAcademicoId { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public string TipoCargo { get; init; } = string.Empty;
    public decimal SueldoBaseMensual { get; init; }
    public decimal CantidadPersonas { get; init; }
    public decimal FactorPonderacion { get; init; }
    public decimal FactorInflacion { get; init; }
    public decimal CostoBaseSemestral { get; init; }
    public decimal CostoTotalSemestre { get; init; }
}