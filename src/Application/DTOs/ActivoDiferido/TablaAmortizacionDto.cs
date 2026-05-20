namespace SistemaAranceles.Application.DTOs.ActivoDiferido;

public sealed class TablaAmortizacionDto
{
    public IReadOnlyList<int> Anios { get; init; } = [];
    public IReadOnlyList<FilaAmortizacionDto> Filas { get; init; } = [];
    public IReadOnlyList<decimal> TotalesPorAnio { get; init; } = [];
    public decimal TotalGeneral { get; init; }
}

public sealed class FilaAmortizacionDto
{
    public int ActivoDiferidoId { get; init; }
    public string NombreRubro { get; init; } = string.Empty;
    public decimal ValorTotal { get; init; }
    public decimal TasaAmortizacion { get; init; }
    /// <summary>Cuota de amortización por año, indexada igual que TablaAmortizacionDto.Anios.</summary>
    public IReadOnlyList<decimal> CuotasPorAnio { get; init; } = [];
    public string ValorTotalDisplay => ValorTotal == 0m ? "$ -" : ValorTotal.ToString("N2");
}
