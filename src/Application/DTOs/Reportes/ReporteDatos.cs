using SistemaAranceles.Application.DTOs.Amortizacion;
using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.DTOs.InversionInicial;

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
