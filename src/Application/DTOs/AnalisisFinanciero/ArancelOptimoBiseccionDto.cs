namespace SistemaAranceles.Application.DTOs.AnalisisFinanciero;

public sealed class ArancelOptimoBiseccionIteracionDto
{
    public int Numero { get; init; }
    public decimal ArancelMinimo { get; init; }
    public decimal ArancelMaximo { get; init; }
    public decimal ArancelMedio { get; init; }
    public decimal Van { get; init; }

    public string ArancelMinimoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(ArancelMinimo, FormatoMatrizAnalisisFinanciero.Moneda);
    public string ArancelMaximoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(ArancelMaximo, FormatoMatrizAnalisisFinanciero.Moneda);
    public string ArancelMedioDisplay => FormatoMatrizAnalisisFinanciero.Formatear(ArancelMedio, FormatoMatrizAnalisisFinanciero.Moneda);
    public string VanDisplay => FormatoMatrizAnalisisFinanciero.Formatear(Van, FormatoMatrizAnalisisFinanciero.Moneda);
}

public sealed class ArancelOptimoBiseccionPeriodoDto
{
    public int PeriodoOrden { get; init; }
    public int? PeriodoAcademicoId { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal Estudiantes { get; init; }
    public decimal Ingresos { get; init; }
    public decimal BecasInstitucionales { get; init; }
    public decimal CostosYGastos { get; init; }
    public decimal UtilidadPerdidaEjercicio { get; init; }
    public decimal FlujoNeto { get; init; }
    public decimal FlujoAcumulado { get; init; }

    public string EstudiantesDisplay => Estudiantes.ToString("N2");
    public string IngresosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(Ingresos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string BecasInstitucionalesDisplay => FormatoMatrizAnalisisFinanciero.Formatear(BecasInstitucionales, FormatoMatrizAnalisisFinanciero.Moneda);
    public string CostosYGastosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(CostosYGastos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string UtilidadPerdidaEjercicioDisplay => FormatoMatrizAnalisisFinanciero.Formatear(UtilidadPerdidaEjercicio, FormatoMatrizAnalisisFinanciero.Moneda);
    public string FlujoNetoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(FlujoNeto, FormatoMatrizAnalisisFinanciero.Moneda);
    public string FlujoAcumuladoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(FlujoAcumulado, FormatoMatrizAnalisisFinanciero.Moneda);
}

public sealed class ArancelOptimoBiseccionDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public bool Disponible { get; init; }
    public string Estado { get; init; } = "Sin datos";
    public decimal ArancelMinimo { get; init; }
    public decimal ArancelMaximo { get; init; }
    public decimal ArancelMaximoEvaluado { get; init; }
    public decimal ToleranciaVan { get; init; }
    public int IteracionesUsadas { get; init; }
    public int ExpansionesRango { get; init; }
    public decimal TmrPorcentaje { get; init; }
    public bool EsTmrManual { get; init; }
    public decimal ArancelOptimo { get; init; }
    public decimal MatriculaOptima { get; init; }
    public decimal TotalPorSemestre { get; init; }
    public decimal Van { get; init; }
    public decimal VanArancelMinimo { get; init; }
    public decimal VanArancelMaximo { get; init; }
    public decimal MejorArancelEncontrado { get; init; }
    public decimal MejorVanEncontrado { get; init; }
    public string EstadoConvergencia { get; init; } = "Sin datos";
    public bool EsTirCalculable { get; init; }
    public decimal TirPorcentaje { get; init; }
    public string? MensajeAdvertencia { get; init; }
    public IReadOnlyList<ArancelOptimoBiseccionIteracionDto> Iteraciones { get; init; } = [];
    public IReadOnlyList<ArancelOptimoBiseccionPeriodoDto> Periodos { get; init; } = [];

    public bool TieneDatos => Periodos.Count > 0 || Iteraciones.Count > 0 || Disponible;
    public string TmrDisplay => $"{TmrPorcentaje:N2} %";
    public string FuenteTmrDisplay => EsTmrManual ? "Manual" : "Calculada";
    public string ArancelOptimoDisplay => Disponible
        ? FormatoMatrizAnalisisFinanciero.Formatear(ArancelOptimo, FormatoMatrizAnalisisFinanciero.Moneda)
        : "No calculable";
    public string MatriculaOptimaDisplay => Disponible
        ? FormatoMatrizAnalisisFinanciero.Formatear(MatriculaOptima, FormatoMatrizAnalisisFinanciero.Moneda)
        : "No calculable";
    public string TotalPorSemestreDisplay => Disponible
        ? FormatoMatrizAnalisisFinanciero.Formatear(TotalPorSemestre, FormatoMatrizAnalisisFinanciero.Moneda)
        : "No calculable";
    public string VanDisplay => FormatoMatrizAnalisisFinanciero.Formatear(Van, FormatoMatrizAnalisisFinanciero.Moneda);
    public string VanArancelMinimoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(VanArancelMinimo, FormatoMatrizAnalisisFinanciero.Moneda);
    public string VanArancelMaximoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(VanArancelMaximo, FormatoMatrizAnalisisFinanciero.Moneda);
    public string ArancelMaximoEvaluadoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(ArancelMaximoEvaluado > 0m ? ArancelMaximoEvaluado : ArancelMaximo, FormatoMatrizAnalisisFinanciero.Moneda);
    public string MejorArancelEncontradoDisplay => MejorArancelEncontrado > 0m
        ? FormatoMatrizAnalisisFinanciero.Formatear(MejorArancelEncontrado, FormatoMatrizAnalisisFinanciero.Moneda)
        : "Sin datos";
    public string MejorVanEncontradoDisplay => FormatoMatrizAnalisisFinanciero.Formatear(MejorVanEncontrado, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TirDisplay => EsTirCalculable ? $"{TirPorcentaje:N2} %" : "No calculable";
    public string RangoArancelDisplay => $"{FormatoMatrizAnalisisFinanciero.Formatear(ArancelMinimo, FormatoMatrizAnalisisFinanciero.Moneda)} - {FormatoMatrizAnalisisFinanciero.Formatear(ArancelMaximo, FormatoMatrizAnalisisFinanciero.Moneda)}";
    public string RangoEvaluadoDisplay => $"{FormatoMatrizAnalisisFinanciero.Formatear(ArancelMinimo, FormatoMatrizAnalisisFinanciero.Moneda)} - {ArancelMaximoEvaluadoDisplay}";
    public string ToleranciaDisplay => $"± {FormatoMatrizAnalisisFinanciero.Formatear(ToleranciaVan, FormatoMatrizAnalisisFinanciero.Moneda)}";
}
