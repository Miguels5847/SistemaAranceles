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

/// <summary>
/// Hoja de sueldos consolidada (similar a "7 Sueldos" en Excel).
/// </summary>
public sealed class HojaSueldosPeriodoDto
{
    public int PeriodoAcademicoId { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }

    public List<LineaSueldoDto> Lineas { get; init; } = [];

    public decimal TotalGeneral => Lineas.Sum(x => x.CostoTotalSemestre);
    public string TotalGeneralDisplay => TotalGeneral.ToString("C2");
}

/// <summary>
/// Una línea en la hoja de sueldos (cargo).
/// </summary>
public sealed class LineaSueldoDto
{
    public string Origen { get; init; } = string.Empty; // "Facultad" o "Planta Central"
    public string NombreCargo { get; init; } = string.Empty;
    public decimal SueldoBaseMensual { get; init; }
    public decimal CantidadPersonas { get; init; }
    public decimal FactorPonderacion { get; init; } // Peso
    public decimal FactorInflacion { get; init; }
    public decimal CostoBaseSemestral { get; init; }
    public decimal CostoTotalSemestre { get; init; }

    // Para planta central
    public decimal ProporcionAsignacion { get; init; }

    public string SueldoBaseMensualDisplay => SueldoBaseMensual.ToString("C2");
    public string CostoTotalSemestreDisplay => CostoTotalSemestre.ToString("C2");
    public string FactorPonderacionDisplay => FactorPonderacion.ToString("P2");
    public string ProporcionDisplay => (ProporcionAsignacion > 0 ? ProporcionAsignacion : 1m).ToString("P2");
}
