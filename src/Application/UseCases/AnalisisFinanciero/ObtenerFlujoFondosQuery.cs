using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.ActivoDiferido;
using SistemaAranceles.Application.UseCases.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.InversionInicial;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

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
        ResumenCapitalTrabajoDto? capitalTrabajoPrecalculado = null,
        decimal factorImprevisto = 1.05m)
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
            ?? await obtenerEstadoPerdidasGananciasQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, factorImprevisto: factorImprevisto);
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
            // No se recupera capital de trabajo (convención del Excel).
            var pagoCredito = 0m;
            var flujoNeto = decimal.Round(
                periodo.UtilidadPerdidaEjercicio
                - inversionFutura
                + depreciacionPeriodo
                + amortizacionPeriodo
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
                RecuperacionCapitalTrabajo = 0m,
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
            EtiquetasPeriodos = valores.Select(v => v.PeriodoOrden == 0 ? "0" : v.EtiquetaPeriodo).ToList(),
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
            CrearFila("INGRESOS", V(x => x.Ingresos), esTotal: true, tipoFila: "seccion"),
            CrearFila("COSTOS POR SERVICIOS", V(x => x.CostosServicios), esTotal: true, tipoFila: "seccion"),
            CrearFila("GASTOS DE ADMINISTRACION", V(x => x.GastosAdministracion), esTotal: true, tipoFila: "seccion"),
            CrearFila("GASTOS DE VENTAS", V(x => x.GastosVentas), esTotal: true, tipoFila: "seccion"),
            CrearFila("OTROS GASTOS", V(x => x.OtrosGastos), esTotal: true, tipoFila: "seccion"),
            CrearFila("GASTOS FINANCIEROS", V(x => x.GastosFinancieros), esTotal: true, tipoFila: "seccion"),
            CrearFila("UTILIDAD ANTES DE IMPUESTOS Y PARTICIPACION A TRABAJADORES", V(x => x.UtilidadAntesParticipacionImpuestos), esTotal: true, tipoFila: "resultado"),
            CrearFila("PARTICIPACION A TRABAJADORES 15%", V(x => x.ParticipacionTrabajadores)),
            CrearFila("UTILIDAD ANTES DE IMPUESTOS", V(x => x.UtilidadAntesImpuestos), esTotal: true, tipoFila: "resultado"),
            CrearFila("IMPUESTO A LA RENTA 25%", V(x => x.ImpuestoRenta)),
            CrearFila("UTILIDAD O PERDIDA DEL EJERCICIO", V(x => x.UtilidadPerdidaEjercicio), esTotal: true, tipoFila: "resultado"),
            CrearFila("INVERSION", V(x => x.PeriodoOrden == 0 ? -x.InversionInicial : -x.InversionesFuturas)),
            CrearFila("DEPRECIACION", V(x => x.Depreciacion)),
            CrearFila("AMORTIZACION DE ACTIVOS DIFERIDOS", V(x => x.AmortizacionActivosDiferidos)),
            CrearFila("RECUPERACION DEL CAPITAL DE TRABAJO", V(x => x.RecuperacionCapitalTrabajo)),
            CrearFila("PAGO DEL CREDITO", V(x => x.PagoCredito)),
            CrearFila("FLUJO DE FONDOS NETO EN USO", V(x => x.FlujoNeto), esTotal: true, tipoFila: "resultado")
        ];
    }

    private static FlujoFondosRubroDto CrearFila(
        string concepto,
        IReadOnlyList<decimal> valores,
        decimal? totalOverride = null,
        bool esTotal = false,
        string tipoFila = "normal")
        => new()
        {
            Concepto = concepto,
            Periodos = valores.Select(v => decimal.Round(v, 2)).ToList(),
            Total = decimal.Round(totalOverride ?? valores.Sum(), 2),
            EsTotal = esTotal,
            TipoFila = tipoFila
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
