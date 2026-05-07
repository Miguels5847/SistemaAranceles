using SistemaAranceles.Application.UseCases.CargosFacultad;

namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class FilaResumenSueldosDto
{
    public int CargoId { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public bool EsCargoDocente { get; init; }
    public decimal Peso { get; init; }
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
    public decimal GranTotal { get; init; }
}
