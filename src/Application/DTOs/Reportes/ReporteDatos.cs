using SistemaAranceles.Application.DTOs.Amortizacion;
using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.DTOs.InversionInicial;
using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;

namespace SistemaAranceles.Application.DTOs.Reportes;

/// <summary>
/// Agregadores para los reportes PDF (KAN-46 Fase 1). No duplican datos:
/// agrupan los DTOs existentes de cada módulo más el encabezado común.
/// </summary>
public sealed record ReporteCesDatos(
    string CarreraNombre,
    string EscenarioNombre,
    DateTime GeneradoEn,
    CesDto Ces);

public sealed record ReporteFinancieroDatos(
    string CarreraNombre,
    string EscenarioNombre,
    DateTime GeneradoEn,
    InversionInicialTotalDto Inversion,
    ResumenCapitalTrabajoDto CapitalTrabajo,
    ResumenFinanciamientoDto Financiamiento,
    MatrizCostosGastosDto CostosGastos,
    FlujoFondosDto FlujoFondos,
    IndicadoresFinancierosDto Indicadores);

public sealed record ReporteDemandaDatos(
    string CarreraNombre,
    string EscenarioNombre,
    DateTime GeneradoEn,
    DemandaProyectadaDto Demanda,
    ArancelEfectivoDto ArancelEfectivo,
    IngresosProyectadosDto Ingresos);

/// <summary>
/// KAN-47: reporte por dirección. Toda sección es opcional — la que venga null
/// se imprime con la nota "No existen datos suficientes para generar esta sección."
/// </summary>
public sealed record ReporteDireccionDatos(
    DireccionReporte Direccion,
    string CarreraNombre,
    string EscenarioNombre,
    DateTime GeneradoEn)
{
    public ArancelEfectivoDto? Arancel { get; init; }
    public DemandaProyectadaDto? Demanda { get; init; }
    public IngresosProyectadosDto? Ingresos { get; init; }
    public MaterialesProyectadosDto? Materiales { get; init; }
    public IReadOnlyList<ActivoFijoDto>? ActivosFijos { get; init; }
    public TotalesActivosFijosDto? TotalesActivos { get; init; }
    public InversionInicialTotalDto? Inversion { get; init; }
    public ResumenCapitalTrabajoDto? CapitalTrabajo { get; init; }
    public MatrizDepreciacionDto? Depreciacion { get; init; }
    public ResumenSueldosVistaDto? Sueldos { get; init; }
    public ResumenMantenimientoDto? Mantenimiento { get; init; }
    public AportePlantaCentralCarreraDto? PlantaCentral { get; init; }
    public MatrizInvVinBecasDto? InvVinBecas { get; init; }
    public MatrizCostosGastosDto? CostosGastos { get; init; }
    public ResumenFinanciamientoDto? Financiamiento { get; init; }
    public IndicadoresFinancierosDto? Indicadores { get; init; }
    public PuntoEquilibrioDto? PuntoEquilibrio { get; init; }
    public FlujoFondosDto? FlujoFondos { get; init; }
    public EstadoPerdidasGananciasDto? BalanceProyectado { get; init; }
    public CesDto? Ces { get; init; }
}
