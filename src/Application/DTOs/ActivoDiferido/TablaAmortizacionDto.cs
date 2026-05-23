namespace SistemaAranceles.Application.DTOs.ActivoDiferido;

public sealed class TablaAmortizacionDto
{
    public IReadOnlyList<int> Anios { get; init; } = [];
    public IReadOnlyList<FilaAmortizacionDto> Filas { get; init; } = [];
    public IReadOnlyList<decimal> TotalesPorAnio { get; init; } = [];
    public decimal TotalGeneral { get; init; }
    public decimal TotalValor => Filas.Sum(x => x.ValorTotal);

    public IReadOnlyList<FilaAmortizacionDto> FilasConTotales
    {
        get
        {
            if (Filas.Count == 0)
                return [];

            var filas = Filas.ToList();
            filas.Add(new FilaAmortizacionDto
            {
                NombreRubro = "TOTALES",
                ValorTotal = TotalValor,
                CuotasPorAnio = TotalesPorAnio,
                EsTotal = true
            });
            return filas;
        }
    }
}

public sealed class FilaAmortizacionDto
{
    public int ActivoDiferidoId { get; init; }
    public string NombreRubro { get; init; } = string.Empty;
    public decimal ValorTotal { get; init; }
    public decimal TasaAmortizacion { get; init; }
    public bool EsTotal { get; init; }
    /// <summary>Cuota de amortización por año, indexada igual que TablaAmortizacionDto.Anios.</summary>
    public IReadOnlyList<decimal> CuotasPorAnio { get; init; } = [];
    public string ValorTotalDisplay => ValorTotal == 0m ? "$ -" : ValorTotal.ToString("N2");
    public string TasaDisplay => EsTotal ? string.Empty : (TasaAmortizacion * 100m).ToString("N0") + " %";
}
