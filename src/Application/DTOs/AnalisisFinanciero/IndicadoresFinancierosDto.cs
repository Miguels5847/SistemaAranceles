namespace SistemaAranceles.Application.DTOs.AnalisisFinanciero;

/// <summary>Detalle de descuento del VAN por período (mapea a hoja "14 VAN").</summary>
public sealed class IndicadorVanPeriodoDto
{
    public int PeriodoOrden { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal FlujoNeto { get; init; }
    public decimal FactorDescuento { get; init; }
    public decimal ValorPresente { get; init; }

    public string FlujoNetoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(FlujoNeto, FormatoMatrizAnalisisFinanciero.Moneda);
    public string FactorDescuentoDisplay => FactorDescuento.ToString("N4");
    public string ValorPresenteDisplay => FormatoMatrizAnalisisFinanciero.Formatear(ValorPresente, FormatoMatrizAnalisisFinanciero.Moneda);
}

public sealed class IndicadoresFinancierosDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;

    // ----- TMR -----
    public bool EsTmrManual { get; init; }
    public decimal TmrPorcentaje { get; init; }
    public decimal TmrTasa { get; init; }
    public decimal TasaInteresFinancieraPorcentaje { get; init; }
    public decimal InflacionPromedioPorcentaje { get; init; }
    public decimal PremioRiesgoPorcentaje { get; init; }
    public int AnioInflacionDesde { get; init; }
    public int AnioInflacionHasta { get; init; }
    public bool TieneDatosInflacion { get; init; }

    // ----- VAN -----
    public decimal Van { get; init; }
    public IReadOnlyList<IndicadorVanPeriodoDto> DetalleVan { get; init; } = [];

    // ----- TIR -----
    public bool EsTirCalculable { get; init; }
    public decimal TirPorcentaje { get; init; }
    public int TirIteraciones { get; init; }

    // ----- Estado -----
    public string EstadoViabilidad { get; init; } = "Sin datos";
    public bool TieneDatos { get; init; }
    public string? MensajeAdvertencia { get; init; }

    // ----- Displays -----
    public string TmrDisplay => $"{TmrPorcentaje:N2} %";
    public string FuenteTmrDisplay => EsTmrManual ? "Manual" : "Calculada";
    public string TmrFormulaDisplay => EsTmrManual
        ? "TMR manual configurada en Datos Institucionales."
        : $"Tasa interés {TasaInteresFinancieraPorcentaje:N2}% + Inflación prom. {InflacionPromedioPorcentaje:N2}% + Premio riesgo {PremioRiesgoPorcentaje:N2}%";
    public string RangoInflacionDisplay => TieneDatosInflacion
        ? $"{AnioInflacionDesde}–{AnioInflacionHasta}"
        : "Sin datos";

    public string VanDisplay => FormatoMatrizAnalisisFinanciero.Formatear(Van, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TirDisplay => EsTirCalculable ? $"{TirPorcentaje:N2} %" : "No calculable";
    public string TasaInteresFinancieraDisplay => $"{TasaInteresFinancieraPorcentaje:N2} %";
    public string InflacionPromedioDisplay => $"{InflacionPromedioPorcentaje:N2} %";
    public string PremioRiesgoDisplay => $"{PremioRiesgoPorcentaje:N2} %";
}
