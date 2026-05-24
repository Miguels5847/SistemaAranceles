namespace SistemaAranceles.Application.DTOs.ActivoDiferido;

public sealed class ActivoDiferidoDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string NombreRubro { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public decimal TasaAmortizacionAnual { get; init; }
    public decimal CuotaAnual { get; init; }
    public string ValorDisplay => Valor == 0m ? "$ -" : Valor.ToString("N2");
    public string CuotaAnualDisplay => CuotaAnual == 0m ? "$ -" : CuotaAnual.ToString("N2");
    public string TasaDisplay => (TasaAmortizacionAnual * 100m).ToString("N0") + " %";
}

public sealed class CrearActivoDiferidoDto
{
    public int CarreraId { get; init; }
    public string NombreRubro { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public decimal TasaAmortizacionAnual { get; init; } = 0.20m;
}

public sealed class ActualizarActivoDiferidoDto
{
    public int Id { get; init; }
    public string NombreRubro { get; init; } = string.Empty;
    public decimal Valor { get; init; }
    public decimal TasaAmortizacionAnual { get; init; }
}
