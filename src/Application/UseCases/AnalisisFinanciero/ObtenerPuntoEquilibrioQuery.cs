using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Services.Financieros;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;

namespace SistemaAranceles.Application.UseCases.AnalisisFinanciero;

public sealed class ObtenerPuntoEquilibrioQuery(
    ObtenerEstadoPerdidasGananciasQuery obtenerEstadoPerdidasGananciasQuery,
    ObtenerMatrizCostosGastosQuery obtenerMatrizCostosGastosQuery,
    ObtenerDemandaProyectadaQuery obtenerDemandaProyectadaQuery,
    CalcularMaterialesPorPeriodoQuery calcularMaterialesPorPeriodoQuery,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario)
{
    public async Task<PuntoEquilibrioDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        EstadoPerdidasGananciasDto? estadoPrecalculado = null,
        MatrizCostosGastosDto? costosPrecalculados = null,
        DemandaProyectadaDto? demandaPrecalculada = null,
        MaterialesProyectadosDto? materialesPrecalculados = null)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;
        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
        {
            return Vacio(
                carreraId,
                carrera?.Nombre ?? string.Empty,
                escenarioProyeccionId,
                escenario?.Nombre ?? string.Empty,
                "Selecciona un escenario para calcular el Punto de Equilibrio.");
        }

        var costos = costosPrecalculados
            ?? await obtenerMatrizCostosGastosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, costos.MensajeAdvertencia);

        if (!costos.TieneDatos || costos.ValoresPorPeriodo.Count == 0)
        {
            return Vacio(
                carreraId,
                carrera?.Nombre ?? costos.CarreraNombre,
                escenarioProyeccionId,
                escenario?.Nombre ?? costos.EscenarioNombre,
                ConstruirMensaje(advertencias, "No hay Costos y Gastos consolidados para calcular el Punto de Equilibrio."));
        }

        var demanda = demandaPrecalculada
            ?? await obtenerDemandaProyectadaQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertenciaDocentes);

        if (!demanda.TieneDatos || demanda.PeriodoAcademicoIds.Count == 0)
        {
            return Vacio(
                carreraId,
                carrera?.Nombre ?? demanda.CarreraNombre,
                escenarioProyeccionId,
                escenario?.Nombre ?? demanda.EscenarioNombre,
                ConstruirMensaje(advertencias, "No hay demanda proyectada para calcular el Punto de Equilibrio."));
        }

        var estado = estadoPrecalculado
            ?? await obtenerEstadoPerdidasGananciasQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, estado.MensajeAdvertencia);
        if (!estado.TieneDatos)
            advertencias.Add("No hay Estado P&G calculado; los ingresos del Punto de Equilibrio se tomaron en 0.");

        var materiales = materialesPrecalculados
            ?? await calcularMaterialesPorPeriodoQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, materiales.MensajeAdvertencia);

        var ingresosPorPeriodo = estado.ValoresPorPeriodo
            .GroupBy(p => p.PeriodoAcademicoId)
            .ToDictionary(g => g.Key, g => decimal.Round(g.Sum(p => p.Ingresos), 2));
        var estudiantesPorPeriodo = demanda.PeriodoAcademicoIds
            .Select((periodoId, index) => new
            {
                PeriodoId = periodoId,
                Estudiantes = index < demanda.TotalesPorPeriodo.Count ? demanda.TotalesPorPeriodo[index] : 0m
            })
            .ToDictionary(x => x.PeriodoId, x => x.Estudiantes);
        var materialesPorPeriodo = materiales.Monetarios
            .GroupBy(m => m.PeriodoAcademicoId)
            .ToDictionary(g => g.Key, g => decimal.Round(g.Sum(m => m.Costo), 2));

        var entradas = costos.ValoresPorPeriodo
            .Select(costo =>
            {
                ingresosPorPeriodo.TryGetValue(costo.PeriodoAcademicoId, out var ingresos);
                estudiantesPorPeriodo.TryGetValue(costo.PeriodoAcademicoId, out var estudiantes);
                materialesPorPeriodo.TryGetValue(costo.PeriodoAcademicoId, out var materialesVariables);

                var variablesConsolidadas = CalcularCostosVariablesConsolidados(costo);
                var costosVariables = decimal.Round(variablesConsolidadas + materialesVariables, 2);
                var costosFijos = decimal.Round(Math.Max(costo.TotalCostosGastos - variablesConsolidadas, 0m), 2);

                return new EntradaPuntoEquilibrioPeriodo
                {
                    PeriodoAcademicoId = costo.PeriodoAcademicoId,
                    Anio = costo.Anio,
                    NumeroPeriodo = costo.NumeroPeriodo,
                    EtiquetaPeriodo = costo.EtiquetaPeriodo,
                    Estudiantes = estudiantes,
                    Ingresos = ingresos,
                    CostosVariables = costosVariables,
                    CostosFijos = costosFijos
                };
            })
            .ToList();

        var periodos = CalculadoraPuntoEquilibrio.Calcular(entradas)
            .Select(p => new PuntoEquilibrioPeriodoDto
            {
                PeriodoAcademicoId = p.PeriodoAcademicoId,
                Anio = p.Anio,
                NumeroPeriodo = p.NumeroPeriodo,
                EtiquetaPeriodo = p.EtiquetaPeriodo,
                Estudiantes = p.Estudiantes,
                Ingresos = p.Ingresos,
                CostosVariables = p.CostosVariables,
                CostosFijos = p.CostosFijos,
                IngresoPromedioEstudiante = p.IngresoPromedioEstudiante,
                CostoVariablePorEstudiante = p.CostoVariablePorEstudiante,
                MargenContribucion = p.MargenContribucion,
                PuntoEquilibrioEstudiantes = p.PuntoEquilibrioEstudiantes,
                PuntoEquilibrioMonetario = p.PuntoEquilibrioMonetario,
                EsCalculable = p.EsCalculable,
                Estado = p.Estado
            })
            .ToList();

        return new PuntoEquilibrioDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? costos.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? costos.EscenarioNombre,
            Periodos = periodos,
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

    private static decimal CalcularCostosVariablesConsolidados(CostoGastoPeriodoDto costo)
    {
        // Clasificacion KAN-42: rubros por estudiante/prorrateados por estudiantes.
        // Sueldos, mantenimiento, depreciacion, amortizacion, financiero e imprevistos quedan como fijos.
        return decimal.Round(
            costo.SeguroEstudiantil
            + costo.BecasInstitucionales
            + costo.Investigacion
            + costo.Vinculacion
            + costo.MarketingComunicacion,
            2);
    }

    private static void AgregarAdvertencia(List<string> advertencias, string? mensaje)
    {
        if (!string.IsNullOrWhiteSpace(mensaje))
            advertencias.Add(mensaje.Trim());
    }

    private static string? ConstruirMensaje(List<string> advertencias, string? adicional = null)
    {
        if (!string.IsNullOrWhiteSpace(adicional))
            advertencias.Add(adicional);

        return advertencias.Count == 0
            ? null
            : string.Join(" ", advertencias.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static PuntoEquilibrioDto Vacio(
        int carreraId,
        string carreraNombre,
        int? escenarioId,
        string escenarioNombre,
        string? mensaje)
        => new()
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EscenarioProyeccionId = escenarioId,
            EscenarioNombre = escenarioNombre,
            MensajeAdvertencia = mensaje
        };
}
