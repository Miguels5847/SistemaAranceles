namespace SistemaAranceles.Application.DTOs.AnalisisFinanciero;

public static class FormatoMatrizAnalisisFinanciero
{
    public const string Moneda = "moneda";
    public const string Porcentaje = "porcentaje";
    public const string Entero = "entero";
    public const string Decimal = "decimal";

    public static string Formatear(decimal valor, string formato)
        => formato switch
        {
            Porcentaje => FormatearPorcentajeFraccion(valor),
            Entero => valor.ToString("N0"),
            Decimal => valor.ToString("N2"),
            _ => FormatearMoneda(valor)
        };

    public static string FormatearMoneda(decimal valor)
    {
        var absoluto = Math.Abs(valor);
        return valor < 0m
            ? $"$ ({absoluto:N2})"
            : $"$ {absoluto:N2}";
    }

    public static string FormatearPorcentajeFraccion(decimal valor)
        => QuitarEspaciosPorcentaje(valor.ToString("P2"));

    public static string FormatearPorcentajeValor(decimal porcentaje)
        => QuitarEspaciosPorcentaje($"{porcentaje:N2}%");

    private static string QuitarEspaciosPorcentaje(string valor)
        => valor.Replace(" ", string.Empty).Replace("\u00A0", string.Empty);
}

public sealed class EstadoPerdidasGananciasPeriodoDto
{
    public int PeriodoAcademicoId { get; init; }
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
}

public sealed class EstadoPerdidasGananciasRubroDto
{
    public string Concepto { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public string FormatoValor { get; init; } = FormatoMatrizAnalisisFinanciero.Moneda;
    public bool EsTotal { get; init; }
    public string TipoFila { get; init; } = "detalle";
    public int Nivel { get; init; }
    public bool EsSeccion => string.Equals(TipoFila, "seccion", StringComparison.OrdinalIgnoreCase);
    public bool EsDetalle => string.Equals(TipoFila, "detalle", StringComparison.OrdinalIgnoreCase);
    public bool EsResultado => string.Equals(TipoFila, "resultado", StringComparison.OrdinalIgnoreCase);

    public IReadOnlyList<string> PeriodosDisplay => Periodos
        .Select(v => FormatoMatrizAnalisisFinanciero.Formatear(v, FormatoValor))
        .ToList();

    public string TotalDisplay => FormatoMatrizAnalisisFinanciero.Formatear(Total, FormatoValor);
}

public sealed class EstadoPerdidasGananciasDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public decimal PorcentajeParticipacionTrabajadoresAplicado { get; init; }
    public decimal PorcentajeImpuestoRentaAplicado { get; init; }
    public IReadOnlyList<string> EtiquetasPeriodos { get; init; } = [];
    public IReadOnlyList<EstadoPerdidasGananciasPeriodoDto> ValoresPorPeriodo { get; init; } = [];
    public IReadOnlyList<EstadoPerdidasGananciasRubroDto> Filas { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }

    public bool TieneDatos => ValoresPorPeriodo.Count > 0;
    public decimal TotalIngresos => ValoresPorPeriodo.Sum(p => p.Ingresos);
    public decimal TotalCostosYGastos => ValoresPorPeriodo.Sum(p =>
        p.CostosServicios + p.GastosAdministracion + p.GastosVentas + p.OtrosGastos + p.GastosFinancieros);
    public decimal TotalUtilidadPerdidaEjercicio => ValoresPorPeriodo.Sum(p => p.UtilidadPerdidaEjercicio);

    public string TotalIngresosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalIngresos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TotalCostosYGastosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalCostosYGastos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TotalUtilidadPerdidaEjercicioDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalUtilidadPerdidaEjercicio, FormatoMatrizAnalisisFinanciero.Moneda);
    public string PorcentajeParticipacionDisplay => FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(PorcentajeParticipacionTrabajadoresAplicado);
    public string PorcentajeImpuestoRentaDisplay => FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(PorcentajeImpuestoRentaAplicado);
}
