using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.AnalisisFinanciero;

public sealed class ObtenerEstadoPerdidasGananciasQuery(
    CalcularIngresosProyectadosQuery calcularIngresosProyectadosQuery,
    ObtenerMatrizCostosGastosQuery obtenerMatrizCostosGastosQuery,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario)
{
    private const decimal PorcentajeParticipacionTrabajadoresPorDefecto = 15m;
    private const decimal PorcentajeImpuestoRentaPorDefecto = 25m;

    public async Task<EstadoPerdidasGananciasDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        MatrizCostosGastosDto? costosPrecalculados = null)
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
                "Selecciona un escenario para calcular Pérdidas y Ganancias.");
        }

        var ingresos = await calcularIngresosProyectadosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, ingresos.MensajeAdvertencia);

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
                ConstruirMensaje(advertencias, "No hay Costos y Gastos consolidados para calcular Pérdidas y Ganancias."));
        }

        if (ingresos.CeldasPlanas.Count == 0)
            advertencias.Add("No hay Ingresos Proyectados para esta carrera/escenario; los ingresos se calcularon en 0.");

        var ingresosNetosPorPeriodo = ingresos.CeldasPlanas
            .GroupBy(c => c.PeriodoAcademicoId)
            .ToDictionary(g => g.Key, g => decimal.Round(g.Sum(c => c.IngresoNeto), 2));

        var valores = costos.ValoresPorPeriodo
            .Select(costo =>
            {
                ingresosNetosPorPeriodo.TryGetValue(costo.PeriodoAcademicoId, out var ingresoPeriodo);
                return ConstruirPeriodo(costo, ingresoPeriodo);
            })
            .ToList();

        var etiquetas = costos.Periodos.Count > 0
            ? costos.Periodos.Select(p => p.Etiqueta).ToList()
            : valores.Select(v => v.EtiquetaPeriodo).ToList();

        return new EstadoPerdidasGananciasDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? costos.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? costos.EscenarioNombre,
            PorcentajeParticipacionTrabajadoresAplicado = PorcentajeParticipacionTrabajadoresPorDefecto,
            PorcentajeImpuestoRentaAplicado = PorcentajeImpuestoRentaPorDefecto,
            EtiquetasPeriodos = etiquetas,
            ValoresPorPeriodo = valores,
            Filas = ConstruirFilas(valores),
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

    private static EstadoPerdidasGananciasPeriodoDto ConstruirPeriodo(
        CostoGastoPeriodoDto costo,
        decimal ingresos)
    {
        var costosServicios = decimal.Round(costo.CostosServicios, 2);
        var gastosAdministracion = decimal.Round(costo.GastosAdministracion, 2);
        var gastosVentas = decimal.Round(costo.GastosVentas, 2);
        var otrosGastos = decimal.Round(costo.OtrosGastos, 2);
        var gastosFinancieros = decimal.Round(costo.GastoFinanciero, 2);
        var utilidadAntesParticipacion = decimal.Round(
            ingresos - costosServicios - gastosAdministracion - gastosVentas - otrosGastos - gastosFinancieros,
            2);
        var participacion = utilidadAntesParticipacion > 0m
            ? decimal.Round(utilidadAntesParticipacion * PorcentajeParticipacionTrabajadoresPorDefecto / 100m, 2)
            : 0m;
        var utilidadAntesImpuestos = decimal.Round(utilidadAntesParticipacion - participacion, 2);
        var impuesto = utilidadAntesImpuestos > 0m
            ? decimal.Round(utilidadAntesImpuestos * PorcentajeImpuestoRentaPorDefecto / 100m, 2)
            : 0m;

        return new EstadoPerdidasGananciasPeriodoDto
        {
            PeriodoAcademicoId = costo.PeriodoAcademicoId,
            Anio = costo.Anio,
            NumeroPeriodo = costo.NumeroPeriodo,
            EtiquetaPeriodo = costo.EtiquetaPeriodo,
            Ingresos = ingresos,
            CostosServicios = costosServicios,
            GastosAdministracion = gastosAdministracion,
            GastosVentas = gastosVentas,
            OtrosGastos = otrosGastos,
            GastosFinancieros = gastosFinancieros,
            UtilidadAntesParticipacionImpuestos = utilidadAntesParticipacion,
            ParticipacionTrabajadores = participacion,
            UtilidadAntesImpuestos = utilidadAntesImpuestos,
            ImpuestoRenta = impuesto,
            UtilidadPerdidaEjercicio = decimal.Round(utilidadAntesImpuestos - impuesto, 2)
        };
    }

    private static IReadOnlyList<EstadoPerdidasGananciasRubroDto> ConstruirFilas(
        IReadOnlyList<EstadoPerdidasGananciasPeriodoDto> valores)
    {
        IReadOnlyList<decimal> V(Func<EstadoPerdidasGananciasPeriodoDto, decimal> selector)
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
            CrearFila("Participación trabajadores 15%", V(x => x.ParticipacionTrabajadores)),
            CrearFila("Utilidad antes de impuestos", V(x => x.UtilidadAntesImpuestos), esTotal: true),
            CrearFila("Impuesto a la renta 25%", V(x => x.ImpuestoRenta)),
            CrearFila("Utilidad o pérdida del ejercicio", V(x => x.UtilidadPerdidaEjercicio), esTotal: true)
        ];
    }

    private static EstadoPerdidasGananciasRubroDto CrearFila(
        string concepto,
        IReadOnlyList<decimal> valores,
        bool esTotal = false)
        => new()
        {
            Concepto = concepto,
            Periodos = valores.Select(v => decimal.Round(v, 2)).ToList(),
            Total = decimal.Round(valores.Sum(), 2),
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

    private static EstadoPerdidasGananciasDto Vacio(
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
            PorcentajeParticipacionTrabajadoresAplicado = PorcentajeParticipacionTrabajadoresPorDefecto,
            PorcentajeImpuestoRentaAplicado = PorcentajeImpuestoRentaPorDefecto,
            MensajeAdvertencia = mensaje
        };
}
