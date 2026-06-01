using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;

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
        MatrizCostosGastosDto? costosPrecalculados = null,
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
                "Selecciona un escenario para calcular P\u00e9rdidas y Ganancias.");
        }

        var ingresos = await calcularIngresosProyectadosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, ingresos.MensajeAdvertencia);

        var costos = costosPrecalculados
            ?? await obtenerMatrizCostosGastosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, factorImprevisto: factorImprevisto);
        AgregarAdvertencia(advertencias, costos.MensajeAdvertencia);

        if (!costos.TieneDatos || costos.ValoresPorPeriodo.Count == 0)
        {
            return Vacio(
                carreraId,
                carrera?.Nombre ?? costos.CarreraNombre,
                escenarioProyeccionId,
                escenario?.Nombre ?? costos.EscenarioNombre,
                ConstruirMensaje(advertencias, "No hay Costos y Gastos consolidados para calcular P\u00e9rdidas y Ganancias."));
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
            Filas = ConstruirFilas(valores, costos.ValoresPorPeriodo),
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
        IReadOnlyList<EstadoPerdidasGananciasPeriodoDto> valores,
        IReadOnlyList<CostoGastoPeriodoDto> costos)
    {
        IReadOnlyList<decimal> V(Func<EstadoPerdidasGananciasPeriodoDto, decimal> selector)
            => valores.Select(selector).ToList();
        IReadOnlyList<decimal> C(Func<CostoGastoPeriodoDto, decimal> selector)
            => costos.Select(selector).ToList();
        IReadOnlyList<decimal> Restar(IReadOnlyList<decimal> minuendo, IReadOnlyList<decimal> sustraendo)
            => minuendo.Select((valor, i) => decimal.Round(valor - sustraendo[i], 2)).ToList();

        var ingresos = V(x => x.Ingresos);
        var costosServicios = C(x => x.CostosServicios);
        var utilidadBruta = Restar(ingresos, costosServicios);

        return
        [
            CrearFila("INGRESOS", ingresos, "seccion", esTotal: true),

            CrearFila("COSTOS POR SERVICIOS", costosServicios, "seccion", esTotal: true),
            CrearFila("Mantenimiento Edificio", C(x => x.MantenimientoEdificio), "detalle", nivel: 1),
            CrearFila("Capacitaci\u00f3n Docente", C(x => x.CapacitacionDocente), "detalle", nivel: 1),
            CrearFila("Internacionalizaci\u00f3n", C(x => x.Internacionalizacion), "detalle", nivel: 1),
            CrearFila("Insumos Pr\u00e1cticas y Laboratorios", C(x => x.InsumosPracticasLaboratorios), "detalle", nivel: 1),
            CrearFila("Tiempo Completo PhD (18 horas)", C(x => x.TiempoCompletoPhd), "detalle", nivel: 1),
            CrearFila("Tiempo Completo Mgs. (18 horas)", C(x => x.TiempoCompletoMgs), "detalle", nivel: 1),
            CrearFila("Medio Tiempo(12 horas)", C(x => x.MedioTiempo), "detalle", nivel: 1),
            CrearFila("Tiempo Parcial (6 h)", C(x => x.TiempoParcial), "detalle", nivel: 1),
            CrearFila("Ocasional Tipo 2 (T\u00e9cnico Docente)", C(x => x.OcasionalTipo2TecnicoDocente), "detalle", nivel: 1),
            CrearFila("Costo del Seguro Estudiantil", C(x => x.CostoSeguroEstudiantil), "detalle", nivel: 1),
            CrearFila("Becas Institucionales 10 % estudiantes", C(x => x.BecasInstitucionales), "detalle", nivel: 1),
            CrearFila("Investigaci\u00f3n 5%", C(x => x.Investigacion), "detalle", nivel: 1),
            CrearFila("Vinculaci\u00f3n 1%", C(x => x.Vinculacion), "detalle", nivel: 1),
            CrearFila("Materiales y Suministros", C(x => x.MaterialesSuministros), "detalle", nivel: 1),
            CrearFila("Depreciaci\u00f3n", C(x => x.Depreciacion), "detalle", nivel: 1),

            CrearFila("UTILIDAD BRUTA", utilidadBruta, "resultado", esTotal: true),

            CrearFila("GASTOS DE ADMINISTRACI\u00d3N", C(x => x.GastosAdministracion), "seccion", esTotal: true),
            CrearFila("Administraci\u00f3n Central", C(x => x.AdministracionCentral), "detalle", nivel: 1),
            CrearFila("Decano", C(x => x.Decano), "detalle", nivel: 1),
            CrearFila("Subdecano", C(x => x.Subdecano), "detalle", nivel: 1),
            CrearFila("Director de Carrera", C(x => x.DirectorCarrera), "detalle", nivel: 1),
            CrearFila("Secretario", C(x => x.Secretario), "detalle", nivel: 1),
            CrearFila("Auxiliar de Secretaria", C(x => x.AuxiliarSecretaria), "detalle", nivel: 1),
            CrearFila("Coordinador", C(x => x.Coordinador), "detalle", nivel: 1),
            CrearFila("Bienestar Estudiantil", C(x => x.BienestarEstudiantil), "detalle", nivel: 1),
            CrearFila("Bibliotecario", C(x => x.Bibliotecario), "detalle", nivel: 1),
            CrearFila("Auxiliar de Servicio", C(x => x.AuxiliarServicio), "detalle", nivel: 1),
            CrearFila("Guardia", C(x => x.Guardia), "detalle", nivel: 1),

            CrearFila("GASTOS DE VENTAS", C(x => x.GastosVentas), "seccion", esTotal: true),
            CrearFila("Publicidad", C(x => x.MarketingComunicacion), "detalle", nivel: 1),

            CrearFila("OTROS GASTOS", C(x => x.OtrosGastos), "seccion", esTotal: true),
            CrearFila("Servicios B\u00e1sicos", C(x => x.ServiciosBasicos), "detalle", nivel: 1),
            CrearFila("Amortizaci\u00f3n de Activos Diferidos", C(x => x.Amortizacion), "detalle", nivel: 1),

            CrearFila("GASTO FINANCIERO", C(x => x.GastoFinanciero), "seccion", esTotal: true),
            CrearFila("Interes", C(x => x.Interes), "detalle", nivel: 1),

            CrearFila("UTILIDAD ANTES DE IMPUESTOS", V(x => x.UtilidadAntesParticipacionImpuestos), "resultado", esTotal: true),
            CrearFila("PARTICIPACI\u00d3N A TRABAJADORES 15%", V(x => x.ParticipacionTrabajadores), "seccion", esTotal: true),
            CrearFila("UTILIDAD ANTES DE IMPUESTOS", V(x => x.UtilidadAntesImpuestos), "resultado", esTotal: true),
            CrearFila("IMPUESTO A LA RENTA 25%", V(x => x.ImpuestoRenta), "seccion", esTotal: true),
            CrearFila("UTILIDAD O PERDIDA DEL EJERCICIO", V(x => x.UtilidadPerdidaEjercicio), "resultado", esTotal: true)
        ];
    }

    private static EstadoPerdidasGananciasRubroDto CrearFila(
        string concepto,
        IReadOnlyList<decimal> valores,
        string tipoFila,
        bool esTotal = false,
        int nivel = 0)
        => new()
        {
            Concepto = concepto,
            Periodos = valores.Select(v => decimal.Round(v, 2)).ToList(),
            Total = decimal.Round(valores.Sum(), 2),
            EsTotal = esTotal,
            TipoFila = tipoFila,
            Nivel = nivel
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
