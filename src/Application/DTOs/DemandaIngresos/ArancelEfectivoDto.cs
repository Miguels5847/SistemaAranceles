namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class ArancelEfectivoDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string ModoCalculoArancel { get; init; } = "Manual";
    public decimal? ArancelEfectivo { get; init; }
    public decimal MatriculaEfectiva { get; init; }
    public decimal PorcentajeMatriculaAplicado { get; init; }
    public string FuenteCalculo { get; init; } = string.Empty;
    public string? MensajeAdvertencia { get; init; }

    public bool EstaResuelto => ArancelEfectivo is > 0m;
    public bool TieneAdvertencia => !string.IsNullOrWhiteSpace(MensajeAdvertencia);
    public string EstadoTexto => TieneAdvertencia ? MensajeAdvertencia! : "Configuracion correcta";

    public string ArancelDisplay => ArancelEfectivo is null
        ? "Pendiente"
        : $"$ {ArancelEfectivo:N2}";
    public string MatriculaDisplay => $"$ {MatriculaEfectiva:N2}";
}
