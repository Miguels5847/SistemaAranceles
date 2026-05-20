namespace SistemaAranceles.Application.DTOs.Mantenimiento;

public sealed class ResumenMantenimientoDto
{
    public decimal TotalServiciosBasicos { get; init; }
    public decimal TotalMantenimiento { get; init; }
    public decimal TotalGeneral => TotalServiciosBasicos + TotalMantenimiento;
    public string TotalServiciosDisplay => TotalServiciosBasicos.ToString("N2");
    public string TotalMantenimientoDisplay => TotalMantenimiento.ToString("N2");
    public string TotalGeneralDisplay => TotalGeneral.ToString("N2");
    public IReadOnlyList<PeriodoMantenimientoDto> Proyeccion { get; init; } = [];
}

public sealed class PeriodoMantenimientoDto
{
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public decimal CostoServiciosBasicos { get; init; }
    public decimal CostoMantenimiento { get; init; }
    public decimal CostoTotal => CostoServiciosBasicos + CostoMantenimiento;
    public string CostoTotalDisplay => CostoTotal == 0m ? "$ -" : CostoTotal.ToString("N2");
}
