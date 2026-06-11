using SistemaAranceles.Application.DTOs.Amortizacion;
using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.DTOs.InversionInicial;
using SistemaAranceles.Application.UseCases.Amortizacion;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.InversionInicial;

namespace SistemaAranceles.Application.UseCases.AnalisisFinanciero;

/// <summary>
/// Balance Proyectado real (KAN-48, hoja "Balance" del Excel). Se DERIVA del
/// Flujo de Fondos (caja), el financiamiento KAN-44B (saldo del préstamo y
/// capital), la inversión inicial (activos, diferidos, capital de trabajo) y
/// los materiales (inventarios), de modo que Activo = Pasivo + Patrimonio
/// cuadra por construcción (salvo céntimos de redondeo).
/// </summary>
public sealed class ObtenerBalanceProyectadoQuery(
    ObtenerFlujoFondosQuery obtenerFlujoFondosQuery,
    ObtenerResumenAmortizacionQuery obtenerResumenFinanciamientoQuery,
    ObtenerInversionInicialTotalQuery obtenerInversionInicialTotalQuery,
    CalcularMaterialesPorPeriodoQuery calcularMaterialesPorPeriodoQuery)
{
    public async Task<BalanceProyectadoDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        FlujoFondosDto? flujoPrecalculado = null,
        ResumenFinanciamientoDto? financiamientoPrecalculado = null,
        InversionInicialTotalDto? inversionPrecalculada = null,
        MaterialesProyectadosDto? materialesPrecalculados = null)
    {
        var flujo = flujoPrecalculado
            ?? await obtenerFlujoFondosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);

        var periodos = flujo.ValoresPorPeriodo.Where(p => p.PeriodoOrden > 0).ToList();
        if (periodos.Count == 0)
        {
            return new BalanceProyectadoDto
            {
                CarreraId = carreraId,
                CarreraNombre = flujo.CarreraNombre,
                EscenarioProyeccionId = escenarioProyeccionId,
                EscenarioNombre = flujo.EscenarioNombre,
                MensajeAdvertencia = flujo.MensajeAdvertencia
                    ?? "No hay Flujo de Fondos para construir el Balance Proyectado."
            };
        }

        var financiamiento = financiamientoPrecalculado
            ?? await obtenerResumenFinanciamientoQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var inversion = inversionPrecalculada
            ?? await obtenerInversionInicialTotalQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var materiales = materialesPrecalculados
            ?? await calcularMaterialesPorPeriodoQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);

        var inventariosPorPeriodo = materiales.Monetarios
            .GroupBy(m => m.PeriodoAcademicoId)
            .ToDictionary(g => g.Key, g => decimal.Round(g.Sum(m => m.Costo), 2));

        var valores = new List<BalanceProyectadoPeriodoDto>(periodos.Count);
        var fijosIniciales = inversion.SubtotalActivosFijos;
        var diferidosIniciales = inversion.ActivosDiferidos;
        var cajaBase = inversion.CapitalTrabajoDosM + inversion.Imprevistos;

        var futurasAcumuladas = 0m;
        var depreciacionAcumulada = 0m;
        var amortDiferidosAcumulada = 0m;
        var resultadosAcumulados = 0m;
        var flujoNetoAcumulado = 0m;

        foreach (var periodo in periodos)
        {
            futurasAcumuladas += periodo.InversionesFuturas;
            depreciacionAcumulada += periodo.Depreciacion;
            amortDiferidosAcumulada += periodo.AmortizacionActivosDiferidos;
            resultadosAcumulados += periodo.UtilidadPerdidaEjercicio;
            flujoNetoAcumulado += periodo.FlujoNeto;

            var inventarios = 0m;
            if (periodo.PeriodoAcademicoId is int periodoAcademicoId)
                inventariosPorPeriodo.TryGetValue(periodoAcademicoId, out inventarios);

            // Caja = capital de trabajo + imprevistos (efectivo inicial) + flujo operativo
            // acumulado + PT/IR del período (devengados, aún por pagar) − stock en inventarios.
            var caja = decimal.Round(
                cajaBase
                + flujoNetoAcumulado
                + periodo.ParticipacionTrabajadores
                + periodo.ImpuestoRenta
                - inventarios,
                2);

            valores.Add(new BalanceProyectadoPeriodoDto
            {
                PeriodoAcademicoId = periodo.PeriodoAcademicoId ?? 0,
                Anio = periodo.Anio,
                NumeroPeriodo = periodo.NumeroPeriodo,
                EtiquetaPeriodo = periodo.EtiquetaPeriodo,
                CajaBancos = caja,
                Inventarios = inventarios,
                ActivoFijoBruto = decimal.Round(fijosIniciales + futurasAcumuladas, 2),
                DepreciacionAcumulada = decimal.Round(depreciacionAcumulada, 2),
                ActivoDiferidoNeto = decimal.Round(diferidosIniciales - amortDiferidosAcumulada, 2),
                ParticipacionPorPagar = periodo.ParticipacionTrabajadores,
                ImpuestoRentaPorPagar = periodo.ImpuestoRenta,
                PrestamoPorPagar = AmortizacionPorPeriodo.SaldoAlCierreDelPeriodo(financiamiento, periodo.NumeroPeriodo),
                ConvenioPorPagar = financiamiento.MontoConvenio,
                Capital = financiamiento.MontoPropio,
                ResultadosAcumulados = decimal.Round(resultadosAcumulados, 2)
            });
        }

        var advertencias = new List<string>();
        if (!string.IsNullOrWhiteSpace(flujo.MensajeAdvertencia))
            advertencias.Add(flujo.MensajeAdvertencia!);
        if (valores.Any(v => !v.Cuadra))
            advertencias.Add("El balance presenta diferencias de cuadre mayores a $0.05 en algún período (revisar redondeos de origen).");

        return new BalanceProyectadoDto
        {
            CarreraId = carreraId,
            CarreraNombre = flujo.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = flujo.EscenarioNombre,
            EtiquetasPeriodos = valores.Select(v => v.EtiquetaPeriodo).ToList(),
            ValoresPorPeriodo = valores,
            Filas = ConstruirFilas(valores),
            MensajeAdvertencia = advertencias.Count == 0 ? null : string.Join(" ", advertencias)
        };
    }

    private const string TipoSeccion = "seccion";
    private const string TipoResultado = "resultado";

    private static IReadOnlyList<BalanceRubroDto> ConstruirFilas(IReadOnlyList<BalanceProyectadoPeriodoDto> valores)
    {
        IReadOnlyList<decimal> V(Func<BalanceProyectadoPeriodoDto, decimal> selector)
            => valores.Select(v => decimal.Round(selector(v), 2)).ToList();

        return
        [
            Fila("ACTIVO", V(_ => 0m), TipoSeccion),
            Fila("Caja y bancos", V(x => x.CajaBancos)),
            Fila("Inventarios (suministros y materiales)", V(x => x.Inventarios)),
            Fila("Activos fijos (costo)", V(x => x.ActivoFijoBruto)),
            Fila("(-) Depreciación acumulada", V(x => -x.DepreciacionAcumulada)),
            Fila("Activo fijo neto", V(x => x.ActivoFijoNeto), TipoResultado),
            Fila("Activo diferido neto", V(x => x.ActivoDiferidoNeto)),
            Fila("TOTAL ACTIVO", V(x => x.TotalActivo), TipoResultado),
            Fila("PASIVO", V(_ => 0m), TipoSeccion),
            Fila("Participación trabajadores por pagar (15%)", V(x => x.ParticipacionPorPagar)),
            Fila("Impuesto a la renta por pagar (25%)", V(x => x.ImpuestoRentaPorPagar)),
            Fila("Préstamo bancario por pagar", V(x => x.PrestamoPorPagar)),
            Fila("Convenio institucional por pagar", V(x => x.ConvenioPorPagar)),
            Fila("TOTAL PASIVO", V(x => x.TotalPasivo), TipoResultado),
            Fila("PATRIMONIO", V(_ => 0m), TipoSeccion),
            Fila("Capital (recursos propios)", V(x => x.Capital)),
            Fila("Resultados acumulados", V(x => x.ResultadosAcumulados)),
            Fila("TOTAL PATRIMONIO", V(x => x.TotalPatrimonio), TipoResultado),
            Fila("TOTAL PASIVO + PATRIMONIO", V(x => x.TotalPasivoPatrimonio), TipoResultado),
            Fila("Diferencia de cuadre", V(x => x.Diferencia), TipoResultado)
        ];
    }

    private static BalanceRubroDto Fila(string concepto, IReadOnlyList<decimal> periodos, string tipoFila = "detalle")
        => new() { Concepto = concepto, Periodos = periodos, TipoFila = tipoFila };
}
