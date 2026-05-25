namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class ConfiguracionArancelCarreraDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public string CarreraCodigo { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public string ModoCalculoArancel { get; init; } = "Manual";
    public decimal? ArancelManual { get; init; }
    public decimal? PorcentajeMatricula { get; init; }
    public bool UsaPorcentajeMatriculaInstitucional { get; init; } = true;
    public bool EstaActivo { get; init; } = true;
    public DateTime CreadoEn { get; init; }
    public DateTime? ActualizadoEn { get; init; }

    public string ArancelManualDisplay => ArancelManual is null ? "-" : $"$ {ArancelManual:N2}";
    public string PorcentajeMatriculaDisplay => PorcentajeMatricula is null
        ? "Institucional"
        : $"{PorcentajeMatricula:0.##}%";
}
