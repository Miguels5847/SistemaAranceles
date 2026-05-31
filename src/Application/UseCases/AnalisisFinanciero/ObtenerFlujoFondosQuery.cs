using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.UseCases.ActivoDiferido;
using SistemaAranceles.Application.UseCases.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.InversionInicial;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.AnalisisFinanciero;

public sealed class ObtenerFlujoFondosQuery(
    ObtenerEstadoPerdidasGananciasQuery obtenerEstadoPerdidasGananciasQuery,
    ObtenerInversionInicialTotalQuery obtenerInversionInicialTotalQuery,
    ObtenerMatrizInversionesQuery obtenerMatrizInversionesQuery,
    ObtenerMatrizDepreciacionQuery obtenerMatrizDepreciacionQuery,
    ObtenerTablaAmortizacionQuery obtenerTablaAmortizacionQuery,
    ObtenerResumenCapitalTrabajoQuery obtenerResumenCapitalTrabajoQuery,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioDatosInstitucionales repositorioDatos)
{
    public async Task<FlujoFondosDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        EstadoPerdidasGananciasDto? estadoPrecalculado = null,
        MatrizInversionesDto? inversionesPrecalculada = null,
        ResumenCapitalTrabajoDto? capitalTrabajoPrecalculado = null)
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
                "Selecciona un escenario para calcular Flujo de Fondos.");
        }

        var estado = estadoPrecalculado
            ?? await obtenerEstadoPerdidasGananciasQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, estado.MensajeAdvertencia);

        if (!estado.TieneDatos || estado.ValoresPorPeriodo.Count == 0)
        {
            return Vacio(
                carreraId,
                carrera?.Nombre ?? estado.CarreraNombre,
                escenarioProyeccionId,
                escenario?.Nombre ?? estado.EscenarioNombre,
                ConstruirMensaje(advertencias, "No hay Estado P&G para calcular Flujo de Fondos."));
        }

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var semestresPorAnio = datos?.SemestresPorAnio is > 0
            ? datos.SemestresPorAnio
            : DatosInstitucionales.SemestresPorAnioPorDefecto;

        var capitalTrabajo = capitalTrabajoPrecalculado
            ?? await obtenerResumenCapitalTrabajoQuery.EjecutarAsync(
                carreraId,
                escenarioProyeccionId,
                ct);
        var inversionInicial = await obtenerInversionInicialTotalQuery.EjecutarAsync(
            carreraId,
            escenarioProyeccionId,
            ct,
            capitalTrabajoPrecalculado: capitalTrabajo);
        var inversiones = inversionesPrecalculada
            ?? await obtenerMatrizInversionesQuery.EjecutarAsync(
                carreraId,
                escenarioProyeccionId.Value,
                null,
                ct);
        var depreciacion = await obtenerMatrizDepreciacionQuery.EjecutarAsync(
            carreraId,
            escenarioProyeccionId.Value,
            null,
            ct,
            inversionesPrecalculada: inversiones);
        var anioBase = estado.ValoresPorPeriodo.Min(p => p.Anio);
        var amortizacion = await obtenerTablaAmortizacionQuery.EjecutarAsync(carreraId, anioBase, ct);

        var inversionesFuturasPorPeriodo = inversiones.TotalesPorPeriodo
            .Where(p => p.NumeroPeriodo > 1)
            .GroupBy(p => p.NumeroPeriodo)
            .ToDictionary(g => g.Key, g => decimal.Round(g.Sum(x => x.Total), 2));
        var depreciacionPorPeriodo = depreciacion.TotalesPorPeriodo
            .GroupBy(p => p.NumeroPeriodo)
            .ToDictionary(g => g.Key, g => decimal.Round(g.Sum(x => x.DepreciacionPeriodo), 2));
        var amortizacionPorAnio = amortizacion.Anios
            .Select((anio, index) => new
            {
                Anio = anio,
                Valor = index < amortizacion.TotalesPorAnio.Count
                    ? decimal.Round(amortizacion.TotalesPorAnio[index] / semestresPorAnio, 2)
                    : 0m
            })
            .ToDictionary(x => x.Anio, x => x.Valor);

        var valores = new List<FlujoFondosPeriodoDto>();
        var acumulado = 0m;
        var totalInversionInicial = decimal.Round(inversionInicial.TotalInversion, 2);
        var totalCapitalTrabajo = decimal.Round(capitalTrabajo.TotalCapitalTrabajo, 2);

        acumulado = decimal.Round(acumulado - totalInversionInicial, 2);
        valores.Add(new FlujoFondosPeriodoDto
        {
            PeriodoOrden = 0,
            EtiquetaPeriodo = "Periodo 0",
            InversionInicial = totalInversionInicial,
            CapitalTrabajo = totalCapitalTrabajo,
            FlujoNeto = -totalInversionInicial,
            FlujoAcumulado = acumulado
        });

        for (var i = 0; i < estado.ValoresPorPeriodo.Count; i++)
        {
            var periodo = estado.ValoresPorPeriodo[i];
            inversionesFuturasPorPeriodo.TryGetValue(periodo.NumeroPeriodo, out var inversionFutura);
            depreciacionPorPeriodo.TryGetValue(periodo.NumeroPeriodo, out var depreciacionPeriodo);
            amortizacionPorAnio.TryGetValue(periodo.Anio, out var amortizacionPeriodo);
            var recuperacionCapitalTrabajo = i == estado.ValoresPorPeriodo.Count - 1 ? totalCapitalTrabajo : 0m;
            var pagoCredito = 0m;
            var flujoNeto = decimal.Round(
                periodo.UtilidadPerdidaEjercicio
                - inversionFutura
                + depreciacionPeriodo
                + amortizacionPeriodo
                + recuperacionCapitalTrabajo
                - pagoCredito,
                2);
            acumulado = decimal.Round(acumulado + flujoNeto, 2);

            valores.Add(new FlujoFondosPeriodoDto
            {
                PeriodoOrden = periodo.NumeroPeriodo,
                PeriodoAcademicoId = periodo.PeriodoAcademicoId,
                Anio = periodo.Anio,
                NumeroPeriodo = periodo.NumeroPeriodo,
                EtiquetaPeriodo = periodo.EtiquetaPeriodo,
                Ingresos = periodo.Ingresos,
                CostosServicios = periodo.CostosServicios,
                GastosAdministracion = periodo.GastosAdministracion,
                GastosVentas = periodo.GastosVentas,
                OtrosGastos = periodo.OtrosGastos,
                GastosFinancieros = periodo.GastosFinancieros,
                UtilidadAntesParticipacionImpuestos = periodo.UtilidadAntesParticipacionImpuestos,
                ParticipacionTrabajadores = periodo.ParticipacionTrabajadores,
                UtilidadAntesImpuestos = periodo.UtilidadAntesImpuestos,
                ImpuestoRenta = periodo.ImpuestoRenta,
                UtilidadPerdidaEjercicio = periodo.UtilidadPerdidaEjercicio,
                InversionesFuturas = inversionFutura,
                Depreciacion = depreciacionPeriodo,
                AmortizacionActivosDiferidos = amortizacionPeriodo,
                RecuperacionCapitalTrabajo = recuperacionCapitalTrabajo,
                PagoCredito = pagoCredito,
                FlujoNeto = flujoNeto,
                FlujoAcumulado = acumulado
            });
        }

        return new FlujoFondosDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? estado.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? estado.EscenarioNombre,
            EtiquetasPeriodos = valores.Select(v => v.EtiquetaPeriodo).ToList(),
            ValoresPorPeriodo = valores,
            Filas = ConstruirFilas(valores),
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

    private static IReadOnlyList<FlujoFondosRubroDto> ConstruirFilas(
        IReadOnlyList<FlujoFondosPeriodoDto> valores)
    {
        IReadOnlyList<decimal> V(Func<FlujoFondosPeriodoDto, decimal> selector)
            => valores.Select(selector).ToList();

        return
        [
            CrearFila("Ingresos", V(x => x.Ingresos)),
            CrearFila("Costos por servicios", V(x => x.CostosServicios)),
            CrearFila("Gastos de administración", V(x => x.GastosAdministracion)),
            CrearFila("Gastos de ventas", V(x => x.GastosVentas)),
            CrearFila("Otros gastos", V(x => x.OtrosGastos)),
            CrearFila("Gastos financieros", V(x => x.GastosFinancieros)),
            CrearFila("Utilidad antes de participación e impuestos", V(x => x.UtilidadAntesParticipacionImpuestos), esTotal: true),
            CrearFila("Participación trabajadores", V(x => x.ParticipacionTrabajadores)),
            CrearFila("Utilidad antes de impuestos", V(x => x.UtilidadAntesImpuestos), esTotal: true),
            CrearFila("Impuesto a la renta", V(x => x.ImpuestoRenta)),
            CrearFila("Utilidad o pérdida del ejercicio", V(x => x.UtilidadPerdidaEjercicio), esTotal: true),
            CrearFila("Inversión inicial", V(x => x.InversionInicial)),
            CrearFila("Inversiones futuras", V(x => x.InversionesFuturas)),
            CrearFila("Depreciación", V(x => x.Depreciacion)),
            CrearFila("Amortización activos diferidos", V(x => x.AmortizacionActivosDiferidos)),
            CrearFila("Capital de trabajo", V(x => x.CapitalTrabajo)),
            CrearFila("Recuperación capital trabajo", V(x => x.RecuperacionCapitalTrabajo)),
            CrearFila("Pago crédito", V(x => x.PagoCredito)),
            CrearFila("Flujo de fondos neto", V(x => x.FlujoNeto), esTotal: true),
            CrearFila("Flujo acumulado", V(x => x.FlujoAcumulado), totalOverride: valores.LastOrDefault()?.FlujoAcumulado ?? 0m, esTotal: true)
        ];
    }

    private static FlujoFondosRubroDto CrearFila(
        string concepto,
        IReadOnlyList<decimal> valores,
        decimal? totalOverride = null,
        bool esTotal = false)
        => new()
        {
            Concepto = concepto,
            Periodos = valores.Select(v => decimal.Round(v, 2)).ToList(),
            Total = decimal.Round(totalOverride ?? valores.Sum(), 2),
            EsTotal = esTotal
        };

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

    private static FlujoFondosDto Vacio(
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
