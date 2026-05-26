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

    /// <summary>
    /// % matrícula institucional vigente (Datos Institucionales). Lo inyecta el query al listar
    /// para que el DTO pueda calcular la matrícula efectiva sin volver a consultar la BD.
    /// </summary>
    public decimal PorcentajeMatriculaInstitucional { get; set; }

    public decimal PorcentajeMatriculaEfectivo => UsaPorcentajeMatriculaInstitucional
        ? PorcentajeMatriculaInstitucional
        : PorcentajeMatricula ?? 0m;

    public decimal MatriculaEfectiva => ArancelManual is > 0m
        ? decimal.Round(ArancelManual.Value * PorcentajeMatriculaEfectivo / 100m, 2)
        : 0m;

    public string ArancelManualDisplay => ArancelManual is null ? "-" : $"$ {ArancelManual:N2}";

    public string MatriculaEfectivaDisplay => ArancelManual is > 0m
        ? $"$ {MatriculaEfectiva:N2}"
        : "-";

    public string FuenteMatriculaDisplay => UsaPorcentajeMatriculaInstitucional
        ? $"Institucional {PorcentajeMatriculaInstitucional:0.##}%"
        : $"Manual {(PorcentajeMatricula ?? 0m):0.##}%";

    public string PorcentajeMatriculaDisplay => $"{PorcentajeMatriculaEfectivo:0.##}%";
}
