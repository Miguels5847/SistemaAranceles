namespace SistemaAranceles.Application.DTOs.CapitalTrabajo;

public sealed class ResumenCapitalTrabajoDto
{
    public decimal SubtotalCargos { get; init; }
    public decimal SubtotalMateriales { get; init; }
    public decimal SubtotalAseo { get; init; }
    public decimal SubtotalAccesorios { get; init; }
    public decimal TotalMensual => SubtotalCargos + SubtotalMateriales + SubtotalAseo + SubtotalAccesorios;
    public decimal TotalDosMeses => decimal.Round(TotalMensual * 2m, 2);
    public string TotalMensualDisplay => TotalMensual.ToString("N2");
    public string TotalDosMesesDisplay => TotalDosMeses.ToString("N2");
}
