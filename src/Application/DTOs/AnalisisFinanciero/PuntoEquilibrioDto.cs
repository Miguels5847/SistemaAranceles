namespace SistemaAranceles.Application.DTOs.AnalisisFinanciero;

public sealed class PuntoEquilibrioPeriodoDto
{
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public decimal Estudiantes { get; init; }
    public decimal Ingresos { get; init; }
    public decimal CostosVariables { get; init; }
    public decimal CostosFijos { get; init; }
    public decimal IngresoPromedioEstudiante { get; init; }
    public decimal CostoVariablePorEstudiante { get; init; }
    public decimal MargenContribucion { get; init; }
    public decimal PuntoEquilibrioEstudiantes { get; init; }
    public decimal PuntoEquilibrioMonetario { get; init; }
    public bool EsCalculable { get; init; }
    public string Estado { get; init; } = "Sin datos";

    public string EstudiantesDisplay => Estudiantes.ToString("N2");
    public string IngresosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(Ingresos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string CostosVariablesDisplay => FormatoMatrizAnalisisFinanciero.Formatear(CostosVariables, FormatoMatrizAnalisisFinanciero.Moneda);
    public string CostosFijosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(CostosFijos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string IngresoPromedioEstudianteDisplay => FormatoMatrizAnalisisFinanciero.Formatear(IngresoPromedioEstudiante, FormatoMatrizAnalisisFinanciero.Moneda);
    public string CostoVariablePorEstudianteDisplay => FormatoMatrizAnalisisFinanciero.Formatear(CostoVariablePorEstudiante, FormatoMatrizAnalisisFinanciero.Moneda);
    public string MargenContribucionDisplay => FormatoMatrizAnalisisFinanciero.Formatear(MargenContribucion, FormatoMatrizAnalisisFinanciero.Moneda);
    public string PuntoEquilibrioEstudiantesDisplay => EsCalculable ? PuntoEquilibrioEstudiantes.ToString("N2") : "No calculable";
    public string PuntoEquilibrioMonetarioDisplay => EsCalculable
        ? FormatoMatrizAnalisisFinanciero.Formatear(PuntoEquilibrioMonetario, FormatoMatrizAnalisisFinanciero.Moneda)
        : "No calculable";
}

public sealed class PuntoEquilibrioDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public IReadOnlyList<PuntoEquilibrioPeriodoDto> Periodos { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }

    public bool TieneDatos => Periodos.Count > 0;
    public bool TienePeriodosNoCalculables => Periodos.Any(p => !p.EsCalculable);
    public string EstadoGeneral => !TieneDatos
        ? "Sin datos"
        : TienePeriodosNoCalculables
            ? "Con periodos no calculables"
            : "Calculado";

    public decimal TotalIngresos => Periodos.Sum(p => p.Ingresos);
    public decimal TotalCostosVariables => Periodos.Sum(p => p.CostosVariables);
    public decimal TotalCostosFijos => Periodos.Sum(p => p.CostosFijos);
    public decimal PuntoEquilibrioEstudiantesPromedio => Periodos.Any(p => p.EsCalculable)
        ? decimal.Round(Periodos.Where(p => p.EsCalculable).Average(p => p.PuntoEquilibrioEstudiantes), 2)
        : 0m;
    public decimal PuntoEquilibrioMonetarioTotal => Periodos.Where(p => p.EsCalculable).Sum(p => p.PuntoEquilibrioMonetario);
    public decimal MargenPromedioContribucion => Periodos.Any(p => p.EsCalculable)
        ? decimal.Round(Periodos.Where(p => p.EsCalculable).Average(p => p.MargenContribucion), 2)
        : 0m;

    public string TotalIngresosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalIngresos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TotalCostosVariablesDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalCostosVariables, FormatoMatrizAnalisisFinanciero.Moneda);
    public string TotalCostosFijosDisplay => FormatoMatrizAnalisisFinanciero.Formatear(TotalCostosFijos, FormatoMatrizAnalisisFinanciero.Moneda);
    public string PuntoEquilibrioEstudiantesPromedioDisplay => Periodos.Any(p => p.EsCalculable)
        ? PuntoEquilibrioEstudiantesPromedio.ToString("N2")
        : "No calculable";
    public string PuntoEquilibrioMonetarioTotalDisplay => Periodos.Any(p => p.EsCalculable)
        ? FormatoMatrizAnalisisFinanciero.Formatear(PuntoEquilibrioMonetarioTotal, FormatoMatrizAnalisisFinanciero.Moneda)
        : "No calculable";
    public string MargenPromedioContribucionDisplay => Periodos.Any(p => p.EsCalculable)
        ? FormatoMatrizAnalisisFinanciero.Formatear(MargenPromedioContribucion, FormatoMatrizAnalisisFinanciero.Moneda)
        : "No calculable";
}
