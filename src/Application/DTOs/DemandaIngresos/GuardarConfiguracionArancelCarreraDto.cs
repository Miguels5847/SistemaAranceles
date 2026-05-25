namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class GuardarConfiguracionArancelCarreraDto
{
    public int? Id { get; init; }
    public int CarreraId { get; init; }
    public int? EscenarioProyeccionId { get; init; }
    public string ModoCalculoArancel { get; init; } = "Manual";
    public decimal? ArancelManual { get; init; }
    public decimal? PorcentajeMatricula { get; init; }
    public bool UsaPorcentajeMatriculaInstitucional { get; init; } = true;
}
