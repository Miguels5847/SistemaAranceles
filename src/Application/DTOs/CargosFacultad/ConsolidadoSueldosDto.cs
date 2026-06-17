namespace SistemaAranceles.Application.DTOs.CargosFacultad;

/// <summary>
/// Consolidado de sueldos por período que combina cargos de facultad y planta central.
/// </summary>
public sealed class ConsolidadoSueldosPeriodoDto
{
    public int PeriodoAcademicoId { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;

    // Facultad
    public IReadOnlyList<ProyeccionCargoFacultadDto> CargosFacultad { get; init; } = [];
    public decimal TotalFacultad => CargosFacultad.Sum(x => x.CostoTotalSemestre);

    // Planta Central
    public IReadOnlyList<ProyeccionCargoPlantaCentralDto> CargoPlantaCentral { get; init; } = [];
    public decimal TotalPlantaCentral => CargoPlantaCentral.Sum(x => x.CostoTotalSemestre);

    // Consolidado
    public decimal TotalConsolidado => TotalFacultad + TotalPlantaCentral;
    public int CantidadCargosFacultad => CargosFacultad.Count;
    public int CantidadCargosPlantaCentral => CargoPlantaCentral.Count;

    // Formatos para visualización
    public string TotalFacultadDisplay => TotalFacultad.ToString("C2");
    public string TotalPlantaCentralDisplay => TotalPlantaCentral.ToString("C2");
    public string TotalConsolidadoDisplay => TotalConsolidado.ToString("C2");
}
