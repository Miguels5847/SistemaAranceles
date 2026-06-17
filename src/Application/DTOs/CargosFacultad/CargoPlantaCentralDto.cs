namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class CargoPlantaCentralDto
{
    public int Id { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public decimal SueldoMensualTotal { get; init; }
    public decimal TotalSemestral => SueldoMensualTotal * 6;
    public string TotalSemestralDisplay => TotalSemestral.ToString("C2");
}

public sealed class GuardarCargoPlantaCentralDto
{
    public int? Id { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public decimal SueldoMensualTotal { get; init; }
}

public sealed class ProyeccionCargoPlantaCentralDto
{
    public int Id { get; init; }
    public int CargoPlantaCentralId { get; init; }
    public int CarreraId { get; init; }
    public int PeriodoAcademicoId { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public decimal SueldoMensualTotal { get; init; }
    public decimal ProporcionAsignacion { get; init; }
    public decimal CostoTotalSemestre { get; init; }
    public string CostoTotalSemestreDisplay => CostoTotalSemestre.ToString("C2");
    public string ProporcionDisplay => ProporcionAsignacion.ToString("P2");
}
