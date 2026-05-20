namespace SistemaAranceles.Application.DTOs.Mantenimiento;

public sealed class ResumenMantenimientoDto
{
    public decimal AlumnosReferenciaServicios { get; init; }
    public decimal TotalServiciosBasicos { get; init; }
    public decimal TotalMantenimiento { get; init; }
    public decimal TotalGeneral => TotalServiciosBasicos + TotalMantenimiento;
    public string AlumnosReferenciaDisplay => AlumnosReferenciaServicios.ToString("N0");
    public string TotalServiciosDisplay => TotalServiciosBasicos.ToString("N2");
    public string TotalMantenimientoDisplay => TotalMantenimiento.ToString("N2");
    public string TotalGeneralDisplay => TotalGeneral.ToString("N2");
    public IReadOnlyList<PeriodoMantenimientoDto> Proyeccion { get; init; } = [];
}

public sealed class PeriodoMantenimientoDto
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public decimal DemandaPeriodo { get; init; }
    public decimal FactorInflacion { get; init; }
    public decimal CostoServiciosBasicos { get; init; }
    public decimal CostoMantenimiento { get; init; }
    public decimal CostoTotal => CostoServiciosBasicos + CostoMantenimiento;
    public string DemandaDisplay => DemandaPeriodo.ToString("N0");
    public string FactorInflacionDisplay => FactorInflacion.ToString("0.######");
    public string CostoTotalDisplay => CostoTotal == 0m ? "$ -" : CostoTotal.ToString("N2");
}
