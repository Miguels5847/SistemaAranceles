namespace SistemaAranceles.Application.DTOs.AnalisisFinanciero;

public sealed class DashboardIndicadorFinancieroDto
{
    public string Nombre { get; init; } = string.Empty;
    public string ValorDisplay { get; init; } = string.Empty;
    public string Detalle { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string Modulo { get; init; } = string.Empty;
    public bool EsViable { get; init; }
}

public sealed class DashboardRecomendacionFinancieraDto
{
    public string Prioridad { get; init; } = string.Empty;
    public string Origen { get; init; } = string.Empty;
    public string Mensaje { get; init; } = string.Empty;
}

public sealed class DashboardFinancieroDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public IReadOnlyList<DashboardIndicadorFinancieroDto> Indicadores { get; init; } = [];
    public IReadOnlyList<DashboardRecomendacionFinancieraDto> Recomendaciones { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }
    public DateTime FechaCalculo { get; init; } = DateTime.Now;

    public decimal TotalIngresos { get; init; }
    public decimal TotalCostosGastos { get; init; }
    public decimal UtilidadPerdida { get; init; }
    public decimal FlujoAcumuladoFinal { get; init; }
    public decimal Van { get; init; }
    public decimal TmrPorcentaje { get; init; }
    public bool EsTirCalculable { get; init; }
    public decimal TirPorcentaje { get; init; }
    public decimal ArancelOptimo { get; init; }

    public bool TieneDatos => Indicadores.Count > 0;
    public int IndicadoresViables => Indicadores.Count(i => i.EsViable);
    public int TotalIndicadores => Indicadores.Count;
    public bool EsViable => TieneDatos && Indicadores.All(i => i.EsViable);
    public string EstadoGeneral => !TieneDatos
        ? "Sin datos"
        : EsViable
            ? "Viable"
            : "Atención requerida";
    public decimal PorcentajeViabilidad => TotalIndicadores > 0
        ? decimal.Round(IndicadoresViables * 100m / TotalIndicadores, 2)
        : 0m;

    public string TotalIngresosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalIngresos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TotalCostosGastosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalCostosGastos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string UtilidadPerdidaDisplay => FormatoMatrizAnalisisFinanciero.Formatear(UtilidadPerdida, FormatoMatrizAnalisisFinanciero.Moneda);
    public string FlujoAcumuladoFinalDisplay => FormatoMatrizAnalisisFinanciero.Formatear(FlujoAcumuladoFinal, FormatoMatrizAnalisisFinanciero.Moneda);
    public string VanDisplay => FormatoMatrizAnalisisFinanciero.Formatear(Van, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TmrDisplay => FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(TmrPorcentaje);
    public string TirDisplay => EsTirCalculable
        ? FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(TirPorcentaje)
        : "No calculable";
    public string ArancelOptimoDisplay => ArancelOptimo > 0m
        ? FormatoMatrizAnalisisFinanciero.Formatear(ArancelOptimo, FormatoMatrizAnalisisFinanciero.Moneda)
        : "No calculable";
    public string ViabilidadDisplay => $"{IndicadoresViables}/{TotalIndicadores} indicadores";
    public string PorcentajeViabilidadDisplay => FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(PorcentajeViabilidad);
    public string FechaCalculoDisplay => FechaCalculo.ToString("dd/MM/yyyy HH:mm");
}
