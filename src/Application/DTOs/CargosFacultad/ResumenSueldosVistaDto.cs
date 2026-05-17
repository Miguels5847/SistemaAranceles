using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;

namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class FilaResumenSueldosDto
{
    public int CargoId { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public bool EsCargoDocente { get; init; }
    public decimal Peso { get; init; }
    public decimal[] PesosPorPeriodo { get; init; } = [];
    public decimal NumeroPersonas { get; init; }
    public decimal[] ValoresPorPeriodo { get; init; } = [];
    public decimal TotalFila { get; init; }
}

public sealed class ResumenSueldosVistaDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public IReadOnlyList<PeriodoDisponibleSueldosDto> Periodos { get; init; } = [];
    public IReadOnlyList<FilaResumenSueldosDto> Filas { get; init; } = [];
    public decimal[] TotalesPorPeriodo { get; init; } = [];
    /// <summary>Suma de TotalesPorPeriodo. Equivale a "Total acumulado 4 años" (cada total semestral = 6 meses).</summary>
    public decimal GranTotal { get; init; }
    /// <summary>Costo semestral del último período proyectado. = TotalesPorPeriodo[^1] (0 si no hay períodos).</summary>
    public decimal TotalSemestralPeriodoFinal { get; init; }
    /// <summary>Etiqueta del último período (ej. "P8-2026"). Vacío si no hay períodos.</summary>
    public string EtiquetaPeriodoFinal { get; init; } = string.Empty;
    // Aporte de la carrera a Planta Central (si está disponible)
    public AportePlantaCentralCarreraDto? PlantaCentralDistribucion { get; init; }

    // GranTotal + Aporte acumulado de Planta Central (si aplica)
    public decimal TotalSueldosMasPlantaCentral { get; init; }
}
