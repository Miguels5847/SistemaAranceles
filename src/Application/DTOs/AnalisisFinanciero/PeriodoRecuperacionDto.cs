namespace SistemaAranceles.Application.DTOs.AnalisisFinanciero;

public sealed class PeriodoRecuperacionDetalleDto
{
    public int PeriodoOrden { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal FlujoNeto { get; init; }
    public decimal FlujoAcumulado { get; init; }
    public bool EsPeriodoRecuperacion { get; init; }
    public string Estado { get; init; } = string.Empty;

    public string FlujoNetoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(FlujoNeto, FormatoMatrizAnalisisFinanciero.Moneda);
    public string FlujoAcumuladoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(FlujoAcumulado, FormatoMatrizAnalisisFinanciero.Moneda);
}

public sealed class PeriodoRecuperacionDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public bool Recuperado { get; init; }
    public string Estado { get; init; } = "Sin datos";
    public string PeriodoRecuperacion { get; init; } = "Pendiente";
    public decimal MesesPorPeriodo { get; init; }
    public decimal TotalMeses { get; init; }
    public int Anios { get; init; }
    public int Meses { get; init; }
    public int Dias { get; init; }
    public decimal FlujoFaltanteAnterior { get; init; }
    public decimal ProporcionPeriodo { get; init; }
    public decimal FlujoAcumuladoFinal { get; init; }
    public IReadOnlyList<PeriodoRecuperacionDetalleDto> Detalle { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }

    public bool TieneDatos => Detalle.Count > 0;
    public string TiempoRecuperacionDisplay => Recuperado
        ? $"{Anios} años, {Meses} meses, {Dias} días"
        : "No recuperado";
    public string TotalMesesDisplay => Recuperado ? $"{TotalMeses:N2} meses" : "No recuperado";
    public string FlujoFaltanteAnteriorDisplay => FormatoMatrizAnalisisFinanciero.Formatear(FlujoFaltanteAnterior, FormatoMatrizAnalisisFinanciero.Moneda);
    public string ProporcionPeriodoDisplay => Recuperado ? FormatoMatrizAnalisisFinanciero.Formatear(ProporcionPeriodo, FormatoMatrizAnalisisFinanciero.Porcentaje) : "No recuperado";
    public string FlujoAcumuladoFinalDisplay => FormatoMatrizAnalisisFinanciero.Formatear(FlujoAcumuladoFinal, FormatoMatrizAnalisisFinanciero.Moneda);
}
