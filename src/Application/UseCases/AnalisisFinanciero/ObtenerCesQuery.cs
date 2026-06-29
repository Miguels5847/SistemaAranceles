using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.InversionInicial;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.AnalisisFinanciero;

/// <summary>
/// Arma los cuadros regulatorios del CES (hojas Excel "INF CES" y "CES") reutilizando los módulos
/// existentes. INF CES clasifica el costo de la 1ª cohorte en las 4 funciones sustantivas;
/// CES son los parámetros de justificación del arancel; más una distribución referencial de costos.
/// </summary>
public sealed class ObtenerCesQuery(
    ObtenerMatrizCostosGastosQuery obtenerMatrizCostosGastosQuery,
    ObtenerDemandaProyectadaQuery obtenerDemandaProyectadaQuery,
    CalcularIngresosProyectadosQuery calcularIngresosProyectadosQuery,
    ObtenerCostoCarreraQuery obtenerCostoCarreraQuery,
    ObtenerArancelEfectivoQuery obtenerArancelEfectivoQuery,
    ObtenerArancelOptimoBiseccionQuery obtenerArancelOptimoBiseccionQuery,
    ObtenerInversionInicialTotalQuery obtenerInversionInicialTotalQuery,
    ObtenerMatrizInversionesQuery obtenerMatrizInversionesQuery,
    CalcularMaterialesPorPeriodoQuery calcularMaterialesPorPeriodoQuery,
    GenerarResumenSueldosQuery generarResumenSueldosQuery,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioDatosInstitucionales repositorioDatos)
{
    public async Task<CesDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        MatrizCostosGastosDto? costosPrecalculados = null,
        DemandaProyectadaDto? demandaPrecalculada = null,
        MatrizInversionesDto? inversionesPrecalculada = null,
        CostoCarreraResultadoDto? costoCarreraPrecalculado = null,
        ArancelEfectivoDto? arancelVigentePrecalculado = null,
        ArancelOptimoBiseccionDto? arancelOptimoPrecalculado = null,
        decimal factorImprevisto = 1.05m)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;

        CesDto Vacio(string mensaje) => new()
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? string.Empty,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? string.Empty,
            MensajeAdvertencia = mensaje
        };

        if (escenarioProyeccionId is null or <= 0)
            return Vacio("Selecciona un escenario para generar los cuadros CES.");

        var matriz = costosPrecalculados
            ?? await obtenerMatrizCostosGastosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, factorImprevisto: factorImprevisto);
        if (!matriz.TieneDatos || matriz.ValoresPorPeriodo.Count == 0)
            return Vacio("No hay Costos y Gastos consolidados para generar los cuadros CES.");

        var demanda = demandaPrecalculada
            ?? await obtenerDemandaProyectadaQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var semestresPorAnio = datos?.SemestresPorAnio is > 0 ? datos.SemestresPorAnio : DatosInstitucionales.SemestresPorAnioPorDefecto;
        var mesesPorPeriodo = semestresPorAnio > 0 ? 12m / semestresPorAnio : 6m;

        var ingresos = await calcularIngresosProyectadosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var costoCarrera = costoCarreraPrecalculado
            ?? await obtenerCostoCarreraQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, matrizPrecalculada: matriz, factorImprevisto: factorImprevisto);
        var arancelVigente = arancelVigentePrecalculado
            ?? await obtenerArancelEfectivoQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var inversiones = inversionesPrecalculada
            ?? await obtenerMatrizInversionesQuery.EjecutarAsync(carreraId, escenarioProyeccionId.Value, null, ct);
        var inversionInicial = await obtenerInversionInicialTotalQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var materiales = await calcularMaterialesPorPeriodoQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var resumenSueldos = await generarResumenSueldosQuery.EjecutarAsync(
            carreraId, escenarioProyeccionId.Value, datos?.NumeroEstudiantesUniversidad ?? 0m, cancellationToken: ct);

        // --- Agregados por rubro (= columna J del Excel "10 Costos y Gastos": SUM(B:I)) ---
        decimal Sum(Func<CostoGastoPeriodoDto, decimal> s) => decimal.Round(matriz.ValoresPorPeriodo.Sum(s), 2);
        var admin = Sum(p => p.GastosAdministracion);
        var docentes = Sum(p => p.TiempoCompletoPhd + p.TiempoCompletoMgs + p.MedioTiempo + p.TiempoParcial + p.OcasionalTipo2TecnicoDocente);
        var internac = Sum(p => p.Internacionalizacion);
        var capacitacion = Sum(p => p.CapacitacionDocente);
        var mantenimiento = Sum(p => p.MantenimientoEdificio);
        var materialesRubro = Sum(p => p.MaterialesSuministros);
        var marketing = Sum(p => p.GastosVentas);
        var serviciosBasicos = Sum(p => p.ServiciosBasicos);
        var seguro = Sum(p => p.CostoSeguroEstudiantil);
        var depreciacion = Sum(p => p.Depreciacion);
        var investigacion = Sum(p => p.Investigacion);
        var vinculacion = Sum(p => p.Vinculacion);
        var becas = decimal.Round(ingresos.CeldasPlanas.Sum(c => c.Becas), 2);
        var bienes = decimal.Round(materialesRubro + marketing + serviciosBasicos + mantenimiento, 2);
        var otros = decimal.Round(seguro + depreciacion, 2);

        // Inversión (INF CES filas 41-43).
        var infraestructura = decimal.Round(inversionInicial.ActivosDiferidos, 2);
        var equipamiento = decimal.Round(
            inversionInicial.SubtotalActivosFijos
            + inversiones.TotalesPorPeriodo.Sum(t => t.Total)
            + materiales.TotalCosto,
            2);
        var bibliotecas = 0m;
        var subtotalInversion = decimal.Round(infraestructura + equipamiento + bibliotecas, 2); // F44

        var subtotalCorrientes = decimal.Round(
            admin + docentes + internac + capacitacion + bienes + becas + otros + investigacion + vinculacion, 2); // F39
        // Total carrera para ratios (= J40 = Σ TotalCostosGastos + becas).
        var totalCarrera = decimal.Round(matriz.TotalGeneral + becas, 2);
        var totalGeneral = decimal.Round(subtotalCorrientes + subtotalInversion, 2); // F45

        // Totales por función (columnas) del bloque (B45..E45).
        var provision = decimal.Round(admin + docentes + bienes + becas + otros + infraestructura + equipamiento + bibliotecas, 2);
        var fomento = investigacion;
        var vinculacionFn = decimal.Round(internac + vinculacion, 2);
        var otrosFn = capacitacion;

        // --- Helpers de formato ---
        static string Money(decimal d) => FormatoMatrizAnalisisFinanciero.FormatearMoneda(d);
        static string MoneyOrEmpty(decimal d) => d == 0m ? string.Empty : FormatoMatrizAnalisisFinanciero.FormatearMoneda(d);
        string Pct(decimal num, decimal den) => den == 0m
            ? string.Empty
            : FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(decimal.Round(num / den * 100m, 2));

        CesInfFilaDto Fila(string concepto, decimal? prov, decimal? fom, decimal? vin, decimal? otr, decimal total, decimal? pctNum, string tipo = "detalle")
            => new()
            {
                Concepto = concepto,
                ProvisionDisplay = prov is { } p ? MoneyOrEmpty(p) : string.Empty,
                FomentoDisplay = fom is { } f ? MoneyOrEmpty(f) : string.Empty,
                VinculacionDisplay = vin is { } v ? MoneyOrEmpty(v) : string.Empty,
                OtrosDisplay = otr is { } o ? MoneyOrEmpty(o) : string.Empty,
                TotalDisplay = Money(total),
                PorcentajeDisplay = pctNum is { } n ? Pct(n, subtotalCorrientes) : string.Empty,
                TipoFila = tipo
            };

        var infCes = new List<CesInfFilaDto>
        {
            Fila("Gastos corrientes", null, null, null, null, 0m, null, "seccion"),
            Fila("Gastos en personal administrativo", admin, 0m, 0m, 0m, admin, admin),
            Fila("Gastos en personal académico", docentes, 0m, internac, capacitacion, decimal.Round(docentes + internac + capacitacion, 2), decimal.Round(docentes + internac + capacitacion, 2)),
            Fila("Bienes y servicios de consumo", bienes, 0m, 0m, 0m, bienes, bienes),
            Fila("Becas y ayudas financieras", becas, 0m, 0m, 0m, becas, becas),
            Fila("Otros", otros, investigacion, vinculacion, 0m, decimal.Round(otros + investigacion + vinculacion, 2), decimal.Round(otros + investigacion + vinculacion, 2)),
            Fila("Subtotal corrientes", null, null, null, null, subtotalCorrientes, null, "total"),
            Fila("Inversión", null, null, null, null, 0m, null, "seccion"),
            Fila("Infraestructura", infraestructura, 0m, 0m, 0m, infraestructura, null),
            Fila("Equipamiento", equipamiento, 0m, 0m, 0m, equipamiento, null),
            Fila("Bibliotecas", bibliotecas, 0m, 0m, 0m, bibliotecas, null),
            Fila("Subtotal inversión", null, null, null, null, subtotalInversion, null, "total"),
            new()
            {
                Concepto = "TOTAL",
                ProvisionDisplay = Money(provision),
                FomentoDisplay = Money(fomento),
                VinculacionDisplay = Money(vinculacionFn),
                OtrosDisplay = Money(otrosFn),
                TotalDisplay = Money(totalGeneral),
                PorcentajeDisplay = string.Empty,
                TipoFila = "resultado"
            },
            new()
            {
                Concepto = "% por función",
                ProvisionDisplay = Pct(provision, totalGeneral),
                FomentoDisplay = Pct(fomento, totalGeneral),
                VinculacionDisplay = Pct(vinculacionFn, totalGeneral),
                OtrosDisplay = Pct(otrosFn, totalGeneral),
                TotalDisplay = Pct(totalGeneral, totalGeneral),
                PorcentajeDisplay = string.Empty,
                TipoFila = "resultado"
            }
        };

        // --- C5: sueldo mensual promedio de profesores a tiempo completo (PhD + Mgs) ---
        var docentesTc = resumenSueldos.Filas
            .Where(f => f.EsCargoDocente && f.NombreCargo.Contains("Tiempo Completo", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var costoTc = docentesTc.Sum(f => f.TotalFila);
        var personasTc = docentesTc.Sum(f => f.NumeroPersonas);
        var nPeriodos = matriz.ValoresPorPeriodo.Count;
        var sueldoPromedio = personasTc > 0m && nPeriodos > 0 && mesesPorPeriodo > 0m
            ? decimal.Round(costoTc / personasTc / (nPeriodos * mesesPorPeriodo), 2)
            : 0m;

        var arancelPropuesto = costoCarrera.ArancelSugeridoSemestre;
        var matriculaPropuesta = costoCarrera.MatriculaSugerida;
        var arancelVigenteValor = arancelVigente.ArancelEfectivo ?? 0m;
        var estudiantesCohorte = demanda.TotalesPorPeriodo.Count > 0 ? demanda.TotalesPorPeriodo[0] : 0m;

        // El CES reporta el arancel FINAL = óptimo financiero (VAN=0), el que cubre todos los costos
        // (no el costo referencial, que sería un valor menor). El costo referencial se muestra aparte
        // como "Costo por Semestre". Si el óptimo no converge, cae al costo para no dejar el cuadro vacío.
        var optimo = arancelOptimoPrecalculado
            ?? await obtenerArancelOptimoBiseccionQuery.EjecutarAsync(
                carreraId, escenarioProyeccionId, ct,
                costosPrecalculados: matriz,
                demandaPrecalculada: demanda,
                inversionesPrecalculada: inversiones,
                factorImprevisto: factorImprevisto);
        var arancelCes = optimo.Disponible ? optimo.ArancelOptimo : arancelPropuesto;
        var matriculaCes = optimo.Disponible ? optimo.MatriculaOptima : matriculaPropuesta;
        var totalCes = optimo.Disponible ? optimo.TotalPorSemestre : costoCarrera.TotalPorSemestre;

        var parametros = new List<CesParametroFilaDto>
        {
            new() { Parametro = "Pago al personal académico", Criterio = "Remuneración mensual promedio de profesores a tiempo completo (referencial)", ValorDisplay = Money(sueldoPromedio) },
            new() { Parametro = "Gasto académico / gasto total", Criterio = "Personal académico respecto del gasto total de la carrera", ValorDisplay = Pct(docentes, totalCarrera), EsResultado = true },
            new() { Parametro = "Gasto académico / gasto total en personal", Criterio = "Personal académico respecto del gasto total en personal", ValorDisplay = Pct(docentes, decimal.Round(docentes + admin, 2)) },
            new() { Parametro = "Profesores PhD y Mgs. / total", Criterio = "Relación de profesores con PhD y maestría", ValorDisplay = FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(100m) },
            new() { Parametro = "Gasto investigación / gasto total", Criterio = "Investigación y desarrollo respecto del gasto total", ValorDisplay = Pct(investigacion, totalCarrera) },
            new() { Parametro = "Gasto vinculación / gasto total", Criterio = "Vinculación respecto del gasto total", ValorDisplay = Pct(vinculacion, totalCarrera) },
            new() { Parametro = "Costo de servicio educativo", Criterio = "Comprendido dentro de la matrícula en función del arancel", ValorDisplay = FormatoMatrizAnalisisFinanciero.FormatearPorcentajeValor(100m) },
            new() { Parametro = "Infraestructura e inversión / gasto total", Criterio = "Gasto en infraestructura y otras inversiones académicas", ValorDisplay = Pct(subtotalInversion, totalCarrera), EsResultado = true },
            new() { Parametro = "Arancel vigente (semestre)", Criterio = "Arancel cobrado en el último período (configurado)", ValorDisplay = Money(arancelVigenteValor) },
            new() { Parametro = "Matrícula vigente (semestre)", Criterio = "10% del arancel vigente", ValorDisplay = Money(decimal.Round(arancelVigenteValor * 0.1m, 2)) },
            new() { Parametro = "Arancel propuesto (semestre)", Criterio = "Arancel por costo de la carrera", ValorDisplay = Money(arancelPropuesto), EsResultado = true },
            new() { Parametro = "Matrícula propuesta (semestre)", Criterio = "10% del arancel propuesto", ValorDisplay = Money(matriculaPropuesta) },
            new() { Parametro = "N.º de estudiantes (1ª cohorte)", Criterio = "Estudiantes para aperturar la primera cohorte", ValorDisplay = decimal.Round(estudiantesCohorte, 0).ToString("N0") }
        };

        // --- Distribución referencial: % del 100% del costo de la carrera por categoría ---
        CesDistribucionFilaDto Dist(string cat, decimal monto, string tipo = "detalle")
            => new() { Categoria = cat, MontoDisplay = Money(monto), PorcentajeDisplay = Pct(monto, totalCarrera), TipoFila = tipo };

        var distribucion = new List<CesDistribucionFilaDto>
        {
            Dist("Personal académico (docentes)", docentes),
            Dist("Personal administrativo", admin),
            Dist("Bienes y servicios de consumo", bienes),
            Dist("Becas y ayudas financieras", becas),
            Dist("Investigación", investigacion),
            Dist("Vinculación", decimal.Round(internac + vinculacion, 2)),
            Dist("Otros (seguro, depreciación, capacitación)", decimal.Round(otros + capacitacion, 2)),
            Dist("Total corriente", subtotalCorrientes, "total"),
            Dist("Infraestructura e inversión académica", subtotalInversion, "referencial")
        };

        return new CesDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? matriz.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? matriz.EscenarioNombre,
            InfCes = infCes,
            ArancelPorSemestreDisplay = Money(arancelCes),
            CostoPorSemestreDisplay = Money(arancelPropuesto),
            CostoDeLaCarreraDisplay = Money(costoCarrera.CostoCarreraCompleta),
            CostoPorEstudianteDisplay = Money(costoCarrera.Periodos.Count > 0 ? costoCarrera.Periodos[0].CostoPorEstudiante : 0m),
            MatriculaDisplay = Money(matriculaCes),
            TotalPorSemestreDisplay = Money(totalCes),
            Parametros = parametros,
            Distribucion = distribucion,
            MensajeAdvertencia = matriz.MensajeAdvertencia
        };
    }
}
