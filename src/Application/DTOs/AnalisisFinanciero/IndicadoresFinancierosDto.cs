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
    public int TirCambiosSigno { get; init; }
    public bool TirPosibleNoUnica { get; init; }

    // ----- Estado -----
    public string EstadoViabilidad { get; init; } = "Sin datos";
    public bool TieneDatos { get; init; }
    public string? MensajeAdvertencia { get; init; }

    // ----- Displays -----
    public string TmrDisplay => FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(TmrPorcentaje);
    public string FuenteTmrDisplay => EsTmrManual
        ? "Manual / tasa mínima de rendimiento"
        : "Calculada / tasa mínima de rendimiento";
    public string TmrFormulaDisplay => EsTmrManual
        ? "TMR manual configurada en Datos Institucionales. Se usa como tasa de descuento para VAN."
        : $"TMR / tasa mínima de rendimiento usada para VAN = (Tasa interés {FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(TasaInteresFinancieraPorcentaje)} × Inflación prom. {FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(InflacionPromedioPorcentaje)} / 100) + Premio riesgo {FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(PremioRiesgoPorcentaje)}";
    public string RangoInflacionDisplay => TieneDatosInflacion
        ? $"{AnioInflacionDesde}–{AnioInflacionHasta}"
        : "Sin datos";

    public string VanDisplay => FormatoMatrizAnalisisFinanciero.Formatear(Van, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TirDisplay => EsTirCalculable
        ? FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(TirPorcentaje)
        : "No calculable";
    public string TirDiagnosticoDisplay
    {
        get
        {
            if (!EsTirCalculable)
                return "No calculable: el flujo no cambia de signo.";
            return TirPosibleNoUnica
                ? "El flujo presenta múltiples cambios de signo. La TIR puede no ser única. Se muestra la TIR normal calculada para mantener compatibilidad con el Excel. La viabilidad financiera debe evaluarse principalmente con el VAN."
                : "Calculable.";
        }
    }
    public string TasaInteresFinancieraDisplay => FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(TasaInteresFinancieraPorcentaje);
    public string InflacionPromedioDisplay => FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(InflacionPromedioPorcentaje);
    public string PremioRiesgoDisplay => FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(PremioRiesgoPorcentaje);
}
