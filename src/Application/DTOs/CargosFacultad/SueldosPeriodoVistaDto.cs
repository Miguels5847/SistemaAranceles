namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class SueldosPeriodoVistaDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;

    public decimal EstudiantesUA { get; init; }
    public decimal EstudiantesCarrera { get; init; }
    public decimal InflacionAcumulada { get; init; } = 1m;
    public decimal InflacionPeriodoPorcentaje { get; init; }
    public decimal ValorBaseDecimoCuartoAnual { get; init; }

    public IReadOnlyList<FilaSueldoPeriodoDto> Filas { get; init; } = [];
    public decimal TotalSemestrePeriodo { get; init; }
}
