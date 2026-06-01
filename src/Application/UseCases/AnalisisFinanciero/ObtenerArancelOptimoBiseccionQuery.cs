using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Services.Financieros;
using SistemaAranceles.Application.UseCases.ActivoDiferido;
using SistemaAranceles.Application.UseCases.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.InversionInicial;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.AnalisisFinanciero;

public sealed class ObtenerArancelOptimoBiseccionQuery(
    ObtenerMatrizCostosGastosQuery obtenerMatrizCostosGastosQuery,
    ObtenerDemandaProyectadaQuery obtenerDemandaProyectadaQuery,
    ObtenerInversionInicialTotalQuery obtenerInversionInicialTotalQuery,
    ObtenerMatrizInversionesQuery obtenerMatrizInversionesQuery,
    ObtenerMatrizDepreciacionQuery obtenerMatrizDepreciacionQuery,
    ObtenerTablaAmortizacionQuery obtenerTablaAmortizacionQuery,
    ObtenerResumenCapitalTrabajoQuery obtenerResumenCapitalTrabajoQuery,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioInflacionAnual repositorioInflacion,
    IRepositorioConfiguracionArancelCarrera repositorioConfiguracionArancel)
{
    private const decimal ArancelMinimo = 500m;
    private const decimal ArancelMaximo = 5000m;
    private const decimal ToleranciaVan = 1m;
    private const int MaxIteraciones = 60;
    private const int MaxExpansionesRango = 10;
    private const decimal PorcentajeParticipacionTrabajadores = 15m;
    private const decimal PorcentajeImpuestoRenta = 25m;

    public async Task<ArancelOptimoBiseccionDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        MatrizCostosGastosDto? costosPrecalculados = null,
        DemandaProyectadaDto? demandaPrecalculada = null,
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
                "Selecciona un escenario para calcular el arancel óptimo por bisección.");
        }

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
                ConstruirMensaje(advertencias, "No hay Costos y Gastos consolidados para calcular el arancel óptimo."));
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
                ConstruirMensaje(advertencias, "No hay demanda proyectada para calcular el arancel óptimo."));
        }

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        if (datos is null)
            advertencias.Add("No hay Datos Institucionales vigentes; se usaron defaults para matrícula, becas y TMR.");

        var porcentajeMatricula = await ObtenerPorcentajeMatriculaAsync(carreraId, escenarioProyeccionId, datos, ct);
        var porcentajeBecas = datos?.PorcentajeBecasInstitucionales
            ?? DatosInstitucionales.PorcentajeBecasInstitucionalesPorDefecto;
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
        var anioBase = costos.ValoresPorPeriodo.Min(p => p.Anio);
        var amortizacion = await obtenerTablaAmortizacionQuery.EjecutarAsync(carreraId, anioBase, ct);

        var tmr = await ObtenerTmrAsync(costos, datos, advertencias, ct);
        var contexto = ConstruirContextoEvaluacion(
            costos,
            demanda,
            inversionInicial.TotalInversion,
            inversiones.TotalesPorPeriodo
                .Where(p => p.NumeroPeriodo > 1)
                .GroupBy(p => p.NumeroPeriodo)
                .ToDictionary(g => g.Key, g => decimal.Round(g.Sum(x => x.Total), 2)),
            depreciacion.TotalesPorPeriodo
                .GroupBy(p => p.NumeroPeriodo)
                .ToDictionary(g => g.Key, g => decimal.Round(g.Sum(x => x.DepreciacionPeriodo), 2)),
            amortizacion.Anios
                .Select((anio, index) => new
                {
                    Anio = anio,
                    Valor = index < amortizacion.TotalesPorAnio.Count
                        ? decimal.Round(amortizacion.TotalesPorAnio[index] / semestresPorAnio, 2)
                        : 0m
                })
                .ToDictionary(x => x.Anio, x => x.Valor),
            capitalTrabajo.TotalCapitalTrabajo,
            porcentajeMatricula,
            porcentajeBecas,
            tmr.TmrTasa);

        var resultado = CalculadoraArancelOptimoBiseccion.Calcular(new EntradaBiseccionArancel
        {
            ArancelMinimo = ArancelMinimo,
            ArancelMaximo = ArancelMaximo,
            ToleranciaVan = ToleranciaVan,
            MaxIteraciones = MaxIteraciones,
            MaxExpansionesRango = MaxExpansionesRango,
            EvaluarVan = arancel => EvaluarArancel(contexto, arancel).Van
        });
        AgregarAdvertencia(advertencias, resultado.Mensaje);

        if (!resultado.EsCalculable)
        {
            return new ArancelOptimoBiseccionDto
            {
                CarreraId = carreraId,
                CarreraNombre = carrera?.Nombre ?? costos.CarreraNombre,
                EscenarioProyeccionId = escenarioProyeccionId,
                EscenarioNombre = escenario?.Nombre ?? costos.EscenarioNombre,
                Disponible = false,
                Estado = resultado.Estado,
                ArancelMinimo = ArancelMinimo,
                ArancelMaximo = ArancelMaximo,
                ArancelMaximoEvaluado = resultado.ArancelMaximoEvaluado,
                ToleranciaVan = ToleranciaVan,
                IteracionesUsadas = resultado.IteracionesUsadas,
                ExpansionesRango = resultado.ExpansionesRango,
                TmrPorcentaje = tmr.TmrPorcentaje,
                EsTmrManual = tmr.EsManual,
                Van = resultado.Van,
                VanArancelMinimo = resultado.VanMinimo,
                VanArancelMaximo = resultado.VanMaximo,
                MejorArancelEncontrado = resultado.MejorArancel,
                MejorVanEncontrado = resultado.MejorVan,
                EstadoConvergencia = resultado.EstadoConvergencia,
                Iteraciones = resultado.Iteraciones.Select(i => new ArancelOptimoBiseccionIteracionDto
                {
                    Numero = i.Numero,
                    ArancelMinimo = i.ArancelMinimo,
                    ArancelMaximo = i.ArancelMaximo,
                    ArancelMedio = i.ArancelMedio,
                    Van = i.Van
                }).ToList(),
                MensajeAdvertencia = ConstruirMensaje(advertencias)
            };
        }

        var evaluacion = EvaluarArancel(contexto, resultado.ArancelOptimo);
        var tir = CalculadoraTIR.Calcular(evaluacion.Flujos);
        AgregarAdvertencia(advertencias, tir.Mensaje);

        return new ArancelOptimoBiseccionDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? costos.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? costos.EscenarioNombre,
            Disponible = true,
            Estado = resultado.Estado,
            ArancelMinimo = ArancelMinimo,
            ArancelMaximo = ArancelMaximo,
            ArancelMaximoEvaluado = resultado.ArancelMaximoEvaluado,
            ToleranciaVan = ToleranciaVan,
            IteracionesUsadas = resultado.IteracionesUsadas,
            ExpansionesRango = resultado.ExpansionesRango,
            TmrPorcentaje = tmr.TmrPorcentaje,
            EsTmrManual = tmr.EsManual,
            ArancelOptimo = resultado.ArancelOptimo,
            MatriculaOptima = evaluacion.Matricula,
            TotalPorSemestre = resultado.ArancelOptimo + evaluacion.Matricula,
            Van = evaluacion.Van,
            VanArancelMinimo = resultado.VanMinimo,
            VanArancelMaximo = resultado.VanMaximo,
            MejorArancelEncontrado = resultado.MejorArancel,
            MejorVanEncontrado = resultado.MejorVan,
            EstadoConvergencia = resultado.EstadoConvergencia,
            EsTirCalculable = tir.EsCalculable,
            TirPorcentaje = tir.EsCalculable ? decimal.Round(tir.Tir * 100m, 2) : 0m,
            Iteraciones = resultado.Iteraciones.Select(i => new ArancelOptimoBiseccionIteracionDto
            {
                Numero = i.Numero,
                ArancelMinimo = i.ArancelMinimo,
                ArancelMaximo = i.ArancelMaximo,
                ArancelMedio = i.ArancelMedio,
                Van = i.Van
            }).ToList(),
            Periodos = evaluacion.Periodos,
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

    private async Task<decimal> ObtenerPorcentajeMatriculaAsync(
        int carreraId,
        int? escenarioProyeccionId,
        DatosInstitucionales? datos,
        CancellationToken ct)
    {
        var porcentajeInstitucional = datos?.PorcentajeMatriculaDefault
            ?? DatosInstitucionales.PorcentajeMatriculaDefaultPorDefecto;
        var configuracion = await repositorioConfiguracionArancel.ObtenerPorCarreraEscenarioAsync(carreraId, escenarioProyeccionId, ct);
        if (configuracion is null && escenarioProyeccionId is not null)
            configuracion = await repositorioConfiguracionArancel.ObtenerPorCarreraEscenarioAsync(carreraId, null, ct);

        if (configuracion is null || configuracion.UsaPorcentajeMatriculaInstitucional)
            return porcentajeInstitucional;

        return configuracion.PorcentajeMatricula ?? 0m;
    }

    private async Task<ResultadoTmr> ObtenerTmrAsync(
        MatrizCostosGastosDto costos,
        DatosInstitucionales? datos,
        List<string> advertencias,
        CancellationToken ct)
    {
        var tasaInteres = datos?.TasaInteresFinanciera ?? DatosInstitucionales.TasaInteresFinancieraPorDefecto;
        var premioRiesgo = datos?.PremioRiesgo ?? DatosInstitucionales.PremioRiesgoPorDefecto;
        var usarTmrManual = datos?.UsarTmrManual ?? DatosInstitucionales.UsarTmrManualPorDefecto;
        var tmrManual = datos?.TmrManual ?? DatosInstitucionales.TmrManualPorDefecto;
        var (inflacionPromedio, tieneInflacion) = await ObtenerInflacionPromedioAsync(costos, ct);

        if (!usarTmrManual && !tieneInflacion)
            advertencias.Add("No hay registros de inflación para el horizonte; la inflación promedio se tomó como 0%.");

        return CalculadoraTMR.Calcular(new ParametrosTmr
        {
            TasaInteresFinancieraPorcentaje = tasaInteres,
            InflacionPromedioPorcentaje = inflacionPromedio,
            PremioRiesgoPorcentaje = premioRiesgo,
            UsarTmrManual = usarTmrManual,
            TmrManualPorcentaje = tmrManual
        });
    }

    private async Task<(decimal promedio, bool tieneDatos)> ObtenerInflacionPromedioAsync(
        MatrizCostosGastosDto costos,
        CancellationToken ct)
    {
        var anios = costos.ValoresPorPeriodo
            .Where(p => p.Anio > 0)
            .Select(p => p.Anio)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        if (anios.Count == 0)
            return (0m, false);

        var registros = await repositorioInflacion.ListarPorRangoAsync(anios.First(), anios.Last(), ct);
        var porAnio = registros
            .GroupBy(r => r.Anio)
            .Select(g => g.OrderBy(r => r.TipoFuente).First())
            .ToList();

        return porAnio.Count == 0
            ? (0m, false)
            : (decimal.Round(porAnio.Average(r => r.PorcentajeInflacion), 4), true);
    }

    private static ContextoEvaluacionArancel ConstruirContextoEvaluacion(
        MatrizCostosGastosDto costos,
        DemandaProyectadaDto demanda,
        decimal inversionInicial,
        IReadOnlyDictionary<int, decimal> inversionesFuturasPorPeriodo,
        IReadOnlyDictionary<int, decimal> depreciacionPorPeriodo,
        IReadOnlyDictionary<int, decimal> amortizacionPorAnio,
        decimal capitalTrabajo,
        decimal porcentajeMatricula,
        decimal porcentajeBecas,
        decimal tmrTasa)
    {
        var estudiantesPorPeriodo = demanda.PeriodoAcademicoIds
            .Select((periodoId, index) => new
            {
                PeriodoId = periodoId,
                Estudiantes = index < demanda.TotalesPorPeriodo.Count ? demanda.TotalesPorPeriodo[index] : 0m
            })
            .ToDictionary(x => x.PeriodoId, x => x.Estudiantes);

        return new ContextoEvaluacionArancel
        {
            Costos = costos.ValoresPorPeriodo,
            EstudiantesPorPeriodo = estudiantesPorPeriodo,
            InversionInicial = decimal.Round(inversionInicial, 2),
            InversionesFuturasPorPeriodo = inversionesFuturasPorPeriodo,
            DepreciacionPorPeriodo = depreciacionPorPeriodo,
            AmortizacionPorAnio = amortizacionPorAnio,
            CapitalTrabajo = decimal.Round(capitalTrabajo, 2),
            PorcentajeMatricula = porcentajeMatricula,
            PorcentajeBecas = porcentajeBecas,
            TmrTasa = tmrTasa
        };
    }

    private static EvaluacionArancel EvaluarArancel(ContextoEvaluacionArancel contexto, decimal arancel)
    {
        var matricula = decimal.Round(arancel * contexto.PorcentajeMatricula / 100m, 2);
        var precioPorEstudiante = arancel + matricula;
        var flujos = new List<decimal>();
        var periodos = new List<ArancelOptimoBiseccionPeriodoDto>();
        var acumulado = decimal.Round(-contexto.InversionInicial, 2);

        flujos.Add(-contexto.InversionInicial);
        periodos.Add(new ArancelOptimoBiseccionPeriodoDto
        {
            PeriodoOrden = 0,
            EtiquetaPeriodo = "Periodo 0",
            FlujoNeto = -contexto.InversionInicial,
            FlujoAcumulado = acumulado
        });

        for (var i = 0; i < contexto.Costos.Count; i++)
        {
            var costo = contexto.Costos[i];
            contexto.EstudiantesPorPeriodo.TryGetValue(costo.PeriodoAcademicoId, out var estudiantes);
            contexto.InversionesFuturasPorPeriodo.TryGetValue(costo.NumeroPeriodo, out var inversionFutura);
            contexto.DepreciacionPorPeriodo.TryGetValue(costo.NumeroPeriodo, out var depreciacion);
            contexto.AmortizacionPorAnio.TryGetValue(costo.Anio, out var amortizacion);
            var recuperacionCapitalTrabajo = i == contexto.Costos.Count - 1 ? contexto.CapitalTrabajo : 0m;

            var ingresoBruto = decimal.Round(estudiantes * precioPorEstudiante, 2);
            var becas = decimal.Round(ingresoBruto * contexto.PorcentajeBecas / 100m, 2);
            var ingresosNetos = decimal.Round(ingresoBruto - becas, 2);
            var costosYGastos = decimal.Round(costo.TotalCostosGastos - costo.BecasInstitucionales + becas, 2);
            var utilidadAntesParticipacion = decimal.Round(ingresosNetos - costosYGastos, 2);
            var participacion = utilidadAntesParticipacion > 0m
                ? decimal.Round(utilidadAntesParticipacion * PorcentajeParticipacionTrabajadores / 100m, 2)
                : 0m;
            var utilidadAntesImpuestos = decimal.Round(utilidadAntesParticipacion - participacion, 2);
            var impuesto = utilidadAntesImpuestos > 0m
                ? decimal.Round(utilidadAntesImpuestos * PorcentajeImpuestoRenta / 100m, 2)
                : 0m;
            var utilidadEjercicio = decimal.Round(utilidadAntesImpuestos - impuesto, 2);
            var flujoNeto = decimal.Round(
                utilidadEjercicio
                - inversionFutura
                + depreciacion
                + amortizacion
                + recuperacionCapitalTrabajo,
                2);
            acumulado = decimal.Round(acumulado + flujoNeto, 2);
            flujos.Add(flujoNeto);

            periodos.Add(new ArancelOptimoBiseccionPeriodoDto
            {
                PeriodoOrden = costo.NumeroPeriodo,
                PeriodoAcademicoId = costo.PeriodoAcademicoId,
                EtiquetaPeriodo = costo.EtiquetaPeriodo,
                Estudiantes = estudiantes,
                Ingresos = ingresosNetos,
                BecasInstitucionales = becas,
                CostosYGastos = costosYGastos,
                UtilidadPerdidaEjercicio = utilidadEjercicio,
                FlujoNeto = flujoNeto,
                FlujoAcumulado = acumulado
            });
        }

        return new EvaluacionArancel
        {
            Matricula = matricula,
            Flujos = flujos,
            Periodos = periodos,
            Van = CalculadoraVAN.Calcular(flujos, contexto.TmrTasa)
        };
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

    private static ArancelOptimoBiseccionDto Vacio(
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
            Estado = "Sin datos",
            ArancelMinimo = ArancelMinimo,
            ArancelMaximo = ArancelMaximo,
            ArancelMaximoEvaluado = ArancelMaximo,
            ToleranciaVan = ToleranciaVan,
            MensajeAdvertencia = mensaje
        };

    private sealed class ContextoEvaluacionArancel
    {
        public IReadOnlyList<CostoGastoPeriodoDto> Costos { get; init; } = [];
        public IReadOnlyDictionary<int, decimal> EstudiantesPorPeriodo { get; init; } = new Dictionary<int, decimal>();
        public decimal InversionInicial { get; init; }
        public IReadOnlyDictionary<int, decimal> InversionesFuturasPorPeriodo { get; init; } = new Dictionary<int, decimal>();
        public IReadOnlyDictionary<int, decimal> DepreciacionPorPeriodo { get; init; } = new Dictionary<int, decimal>();
        public IReadOnlyDictionary<int, decimal> AmortizacionPorAnio { get; init; } = new Dictionary<int, decimal>();
        public decimal CapitalTrabajo { get; init; }
        public decimal PorcentajeMatricula { get; init; }
        public decimal PorcentajeBecas { get; init; }
        public decimal TmrTasa { get; init; }
    }

    private sealed class EvaluacionArancel
    {
        public decimal Matricula { get; init; }
        public IReadOnlyList<decimal> Flujos { get; init; } = [];
        public IReadOnlyList<ArancelOptimoBiseccionPeriodoDto> Periodos { get; init; } = [];
        public decimal Van { get; init; }
    }
}
