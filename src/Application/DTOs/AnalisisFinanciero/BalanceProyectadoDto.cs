namespace SistemaAranceles.Application.DTOs.AnalisisFinanciero;

/// <summary>
/// Balance Proyectado real (hoja "Balance" del Excel, KAN-48): Activos
/// (disponible, realizable, fijo neto), Pasivo (PT/IR por pagar, saldo del
/// préstamo, convenio) y Patrimonio (capital + resultados acumulados), por
/// período semestral. Es 100 % derivado: nada se persiste.
/// </summary>
public sealed class BalanceProyectadoPeriodoDto
{
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;

    // Activos
    public decimal CajaBancos { get; init; }
    public decimal Inventarios { get; init; }
    public decimal ActivoFijoBruto { get; init; }
    public decimal DepreciacionAcumulada { get; init; }
    public decimal ActivoFijoNeto => ActivoFijoBruto - DepreciacionAcumulada;
    public decimal ActivoDiferidoNeto { get; init; }
    public decimal TotalActivo => CajaBancos + Inventarios + ActivoFijoNeto + ActivoDiferidoNeto;

    // Pasivo
    public decimal ParticipacionPorPagar { get; init; }
    public decimal ImpuestoRentaPorPagar { get; init; }
    public decimal PrestamoPorPagar { get; init; }
    public decimal ConvenioPorPagar { get; init; }
    public decimal TotalPasivo => ParticipacionPorPagar + ImpuestoRentaPorPagar + PrestamoPorPagar + ConvenioPorPagar;

    // Patrimonio
    public decimal Capital { get; init; }
    public decimal ResultadosAcumulados { get; init; }
    public decimal TotalPatrimonio => Capital + ResultadosAcumulados;

    public decimal TotalPasivoPatrimonio => TotalPasivo + TotalPatrimonio;
    public decimal Diferencia => TotalActivo - TotalPasivoPatrimonio;
    public bool Cuadra => Math.Abs(Diferencia) < 0.05m;
}

public sealed class BalanceRubroDto
{
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public string TipoFila { get; init; } = "detalle";
    public bool EsSeccion => string.Equals(TipoFila, "seccion", StringComparison.OrdinalIgnoreCase);
    public bool EsTotal => string.Equals(TipoFila, "total", StringComparison.OrdinalIgnoreCase);
    public bool EsResultado => string.Equals(TipoFila, "resultado", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> PeriodosDisplay => Periodos
        .Select(FormatoMatrizAnalisisFinanciero.FormatearMoneda)
        .ToList();
}

public sealed class BalanceProyectadoDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;

    public IReadOnlyList<string> EtiquetasPeriodos { get; init; } = [];
    public IReadOnlyList<BalanceProyectadoPeriodoDto> ValoresPorPeriodo { get; init; } = [];
    public IReadOnlyList<BalanceRubroDto> Filas { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }

    public bool TieneDatos => ValoresPorPeriodo.Count > 0;
    public bool CuadraEnTodosLosPeriodos => ValoresPorPeriodo.All(p => p.Cuadra);
}
