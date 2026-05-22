namespace SistemaAranceles.Application.DTOs.Mantenimiento;

public sealed class ResumenMantenimientoDto
{
    public decimal AlumnosReferenciaServicios { get; init; }
    public decimal TotalServiciosBasicos { get; init; }
    public decimal TotalMantenimiento { get; init; }
    public decimal TotalGeneral => TotalServiciosBasicos + TotalMantenimiento;

    public decimal ValorMensualServiciosBasicosPorAlumno => AlumnosReferenciaServicios <= 0
        ? 0m
        : decimal.Round((TotalServiciosBasicos / 12m) / AlumnosReferenciaServicios, 2);

    public decimal ValorSemestralServiciosBasicosPorAlumno => AlumnosReferenciaServicios <= 0
        ? 0m
        : decimal.Round((TotalServiciosBasicos / 2m) / AlumnosReferenciaServicios, 2);

    public decimal ValorSemestralMantenimientoPorAlumno => AlumnosReferenciaServicios <= 0
        ? 0m
        : decimal.Round((TotalMantenimiento / 2m) / AlumnosReferenciaServicios, 2);

    public string AlumnosReferenciaDisplay => AlumnosReferenciaServicios.ToString("N0");
    public string TotalServiciosDisplay => TotalServiciosBasicos.ToString("N2");
    public string TotalMantenimientoDisplay => TotalMantenimiento.ToString("N2");
    public string TotalGeneralDisplay => TotalGeneral.ToString("N2");
    public string ValorMensualServiciosBasicosPorAlumnoDisplay => ValorMensualServiciosBasicosPorAlumno.ToString("N2");
    public string ValorSemestralServiciosBasicosPorAlumnoDisplay => ValorSemestralServiciosBasicosPorAlumno.ToString("N2");
    public string ValorSemestralMantenimientoPorAlumnoDisplay => ValorSemestralMantenimientoPorAlumno.ToString("N2");

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
    public string AnioDisplay => Anio.ToString();
    public string DemandaDisplay => DemandaPeriodo.ToString("N0");
    public string FactorInflacionDisplay => FactorInflacion.ToString("0.######");
    public string CostoServiciosBasicosDisplay => CostoServiciosBasicos == 0m ? "$ -" : CostoServiciosBasicos.ToString("N2");
    public string CostoMantenimientoDisplay => CostoMantenimiento == 0m ? "$ -" : CostoMantenimiento.ToString("N2");
    public string CostoTotalDisplay => CostoTotal == 0m ? "$ -" : CostoTotal.ToString("N2");
}
