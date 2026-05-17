namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class PeriodoAcademicoCatalogDto
{
    public int Id { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string DisplayText => $"{Anio}-P{NumeroPeriodo}: {EtiquetaPeriodo}";
}
