namespace SistemaAranceles.Application.DTOs.AnalisisFinanciero;

public sealed class FlujoFondosPeriodoDto
{
    public int PeriodoOrden { get; init; }
    public int? PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal Ingresos { get; init; }
    public decimal CostosServicios { get; init; }
    public decimal GastosAdministracion { get; init; }
    public decimal GastosVentas { get; init; }
    public decimal OtrosGastos { get; init; }
    public decimal GastosFinancieros { get; init; }
    public decimal UtilidadAntesParticipacionImpuestos { get; init; }
    public decimal ParticipacionTrabajadores { get; init; }
    public decimal UtilidadAntesImpuestos { get; init; }
    public decimal ImpuestoRenta { get; init; }
    public decimal UtilidadPerdidaEjercicio { get; init; }
    public decimal InversionInicial { get; init; }
    public decimal InversionesFuturas { get; init; }
    public decimal Depreciacion { get; init; }
    public decimal AmortizacionActivosDiferidos { get; init; }
    public decimal CapitalTrabajo { get; init; }
    public decimal RecuperacionCapitalTrabajo { get; init; }
    public decimal PagoCredito { get; init; }
    public decimal FlujoNeto { get; init; }
    public decimal FlujoAcumulado { get; init; }
}

public sealed class FlujoFondosRubroDto
{
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public bool EsTotal { get; init; }
    public string TipoFila { get; init; } = "normal";
    public string FormatoValor { get; init; } = FormatoMatrizAnalisisFinanciero.Moneda;
    public bool EsSeccion => string.Equals(TipoFila, "seccion", StringComparison.OrdinalIgnoreCase);
    public bool EsResultado => string.Equals(TipoFila, "resultado", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> PeriodosDisplay => Periodos
        .Select(v => FormatoMatrizAnalisisFinanciero.Formatear(v, FormatoValor))
        .ToList();

    public string TotalDisplay => FormatoMatrizAnalisisFinanciero.Formatear(Total, FormatoValor);
}

public sealed class FlujoFondosDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public IReadOnlyList<string> EtiquetasPeriodos { get; init; } = [];
    public IReadOnlyList<FlujoFondosPeriodoDto> ValoresPorPeriodo { get; init; } = [];
    public IReadOnlyList<FlujoFondosRubroDto> Filas { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }

    public bool TieneDatos => ValoresPorPeriodo.Count > 0;
    public decimal TotalInversionInicial => ValoresPorPeriodo.Sum(p => p.InversionInicial);
    public decimal TotalInversionesFuturas => ValoresPorPeriodo.Sum(p => p.InversionesFuturas);
    public decimal TotalRecuperacionCapitalTrabajo => ValoresPorPeriodo.Sum(p => p.RecuperacionCapitalTrabajo);
    public decimal TotalFlujoNeto => ValoresPorPeriodo.Sum(p => p.FlujoNeto);
    public decimal FlujoAcumuladoFinal => ValoresPorPeriodo.LastOrDefault()?.FlujoAcumulado ?? 0m;

    public string TotalInversionInicialDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalInversionInicial, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TotalInversionesFuturasDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalInversionesFuturas, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TotalRecuperacionCapitalTrabajoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalRecuperacionCapitalTrabajo, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TotalFlujoNetoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalFlujoNeto, FormatoMatrizAnalisisFinanciero.Moneda);
    public string FlujoAcumuladoFinalDisplay => FormatoMatrizAnalisisFinanciero.Formatear(FlujoAcumuladoFinal, FormatoMatrizAnalisisFinanciero.Moneda);
}
