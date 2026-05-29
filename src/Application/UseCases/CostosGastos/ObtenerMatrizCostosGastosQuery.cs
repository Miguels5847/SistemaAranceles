using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.ActivoDiferido;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Application.UseCases.Mantenimiento;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CostosGastos;

public sealed class ObtenerMatrizCostosGastosQuery(
    ObtenerMatrizInvVinBecasQuery obtenerMatrizInvVinBecasQuery,
    ObtenerDemandaProyectadaQuery obtenerDemandaProyectadaQuery,
    ObtenerResumenMantenimientoQuery obtenerResumenMantenimientoQuery,
    ObtenerMatrizDepreciacionQuery obtenerMatrizDepreciacionQuery,
    ObtenerTablaAmortizacionQuery obtenerTablaAmortizacionQuery,
    GenerarTablaSueldosPeriodoQuery generarTablaSueldosPeriodoQuery,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioConfiguracionRetencion repositorioConfiguracionRetencion,
    IRepositorioOverrideHorasPeriodo repositorioOverrides)
{
    private sealed record RubroBase(string Grupo, string Concepto, IReadOnlyList<decimal> Valores);

    public async Task<MatrizCostosGastosDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        MatrizInvVinBecasDto? invVinBecasPrecalculado = null)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;
        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
            return Vacia(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId, escenario?.Nombre ?? string.Empty, "Selecciona un escenario para consolidar Costos y Gastos.");

        var demanda = await obtenerDemandaProyectadaQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertenciaDocentes);
        if (!demanda.TieneDatos || demanda.EtiquetasPeriodos.Count == 0)
            return Vacia(carreraId, carrera?.Nombre ?? demanda.CarreraNombre, escenarioProyeccionId, demanda.EscenarioNombre, ConstruirMensaje(advertencias, "No hay demanda proyectada para consolidar costos."));

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        if (datos is null)
            advertencias.Add("No hay Datos Institucionales vigentes. Se usaron defaults para porcentajes y rubros institucionales.");

        // Reutiliza la matriz Inv. Vin. Becas si ya fue calculada (evita recomputar Ingresos/arancel).
        var invVinBecas = invVinBecasPrecalculado
            ?? await obtenerMatrizInvVinBecasQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, invVinBecas.MensajeAdvertencia);

        var periodos = ObtenerMatrizInvVinBecasQuery.ConstruirPeriodos(demanda);
        var estudiantes = ObtenerMatrizInvVinBecasQuery.CompletarValores(demanda.TotalesPorPeriodo, periodos.Count);
        var docentes = ObtenerMatrizInvVinBecasQuery.ObtenerDocentesRequeridosPorPeriodo(demanda, periodos.Count);
        var factores = invVinBecas.ValoresPorPeriodo.Count == periodos.Count
            ? invVinBecas.ValoresPorPeriodo.Select(v => v.FactorInflacion).ToList()
            : Enumerable.Repeat(1m, periodos.Count).ToList();

        var mantenimiento = await obtenerResumenMantenimientoQuery.EjecutarAsync(carreraId, escenarioProyeccionId.Value, ct);
        var mantenimientoPorPeriodo = mantenimiento.Proyeccion.ToDictionary(p => p.NumeroPeriodo);

        var depreciacion = await obtenerMatrizDepreciacionQuery.EjecutarAsync(carreraId, escenarioProyeccionId.Value, null, ct);
        var depreciacionPorPeriodo = depreciacion.TotalesPorPeriodo.ToDictionary(p => p.NumeroPeriodo);

        var anioBase = periodos.Min(p => p.Anio);
        var amortizacion = await obtenerTablaAmortizacionQuery.EjecutarAsync(carreraId, anioBase, ct);
        var amortizacionPorAnio = amortizacion.Anios
            .Select((anio, index) => new
            {
                Anio = anio,
                Valor = index < amortizacion.TotalesPorAnio.Count ? amortizacion.TotalesPorAnio[index] : 0m
            })
            .ToDictionary(x => x.Anio, x => x.Valor);

        var proyeccion = await ObtenerProyeccionAsync(carreraId, escenarioProyeccionId.Value, ct);
        var consolidado = proyeccion is null
            ? null
            : await ObtenerConsolidadoAsync(proyeccion, carreraId, escenarioProyeccionId.Value, ct);

        var valores = new List<CostoGastoPeriodoDto>();
        var semestresPorAnio = datos?.SemestresPorAnio is > 0 ? datos.SemestresPorAnio : DatosInstitucionales.SemestresPorAnioPorDefecto;
        var estudiantesUniversidad = datos?.NumeroEstudiantesUniversidad ?? 0;
        var docentesUniversidad = datos?.NumeroDocentesUniversidad ?? 0;
        var porcentajeImprevistos = datos?.PorcentajeImprevistosInversion ?? DatosInstitucionales.PorcentajeImprevistosInversionPorDefecto;

        for (var i = 0; i < periodos.Count; i++)
        {
            var periodo = periodos[i];
            mantenimientoPorPeriodo.TryGetValue(periodo.NumeroPeriodo, out var mant);
            depreciacionPorPeriodo.TryGetValue(periodo.NumeroPeriodo, out var dep);

            var sueldos = await generarTablaSueldosPeriodoQuery.EjecutarAsync(
                carreraId,
                escenarioProyeccionId.Value,
                periodo.PeriodoAcademicoId,
                estudiantesUniversidad,
                consolidado,
                ct);
            var sueldosDocentes = decimal.Round(sueldos.Filas.Where(f => f.EsCargoDocente).Sum(f => f.TotalSemestre), 2);
            var sueldosAdministrativos = decimal.Round(sueldos.Filas.Where(f => !f.EsCargoDocente).Sum(f => f.TotalSemestre), 2);

            var capacitacion = datos is not null && docentesUniversidad > 0
                ? decimal.Round((datos.PresupuestoAnualCapacitacion / semestresPorAnio) * factores[i] / docentesUniversidad * docentes[i], 2)
                : 0m;
            var seguro = datos is not null && estudiantesUniversidad > 0
                ? decimal.Round((datos.PolizaSeguroEstudiantilAnual / semestresPorAnio) * factores[i] / estudiantesUniversidad * estudiantes[i], 2)
                : 0m;
            var marketing = datos is not null && estudiantesUniversidad > 0
                ? decimal.Round((datos.PresupuestoAnualMarketing / semestresPorAnio) * factores[i] / estudiantesUniversidad * estudiantes[i], 2)
                : 0m;
            var amortizacionPeriodo = amortizacionPorAnio.TryGetValue(periodo.Anio, out var amortizacionAnual)
                ? decimal.Round(amortizacionAnual / semestresPorAnio, 2)
                : 0m;
            var invPeriodo = invVinBecas.ValoresPorPeriodo.FirstOrDefault(v => v.PeriodoAcademicoId == periodo.PeriodoAcademicoId);
            // Becas Institucionales = misma serie que 9 Inv. Vin. Becas (origen Demanda/Ingresos Proyectados).
            var becasInstitucionales = decimal.Round(invPeriodo?.BecasInstitucionales ?? 0m, 2);

            var subtotalSinImprevistos =
                (mant?.CostoMantenimiento ?? 0m)
                + capacitacion
                + sueldosDocentes
                + seguro
                + becasInstitucionales
                + (invPeriodo?.Investigacion ?? 0m)
                + (invPeriodo?.Vinculacion ?? 0m)
                + (dep?.DepreciacionPeriodo ?? 0m)
                + sueldosAdministrativos
                + marketing
                + (mant?.CostoServiciosBasicos ?? 0m)
                + amortizacionPeriodo;
            var imprevistos = decimal.Round(subtotalSinImprevistos * porcentajeImprevistos / 100m, 2);

            valores.Add(new CostoGastoPeriodoDto
            {
                PeriodoAcademicoId = periodo.PeriodoAcademicoId,
                Anio = periodo.Anio,
                NumeroPeriodo = periodo.NumeroPeriodo,
                EtiquetaPeriodo = periodo.Etiqueta,
                Mantenimiento = decimal.Round(mant?.CostoMantenimiento ?? 0m, 2),
                CapacitacionDocente = capacitacion,
                SueldosDocentes = sueldosDocentes,
                SeguroEstudiantil = seguro,
                BecasInstitucionales = becasInstitucionales,
                Investigacion = invPeriodo?.Investigacion ?? 0m,
                Vinculacion = invPeriodo?.Vinculacion ?? 0m,
                Depreciacion = dep?.DepreciacionPeriodo ?? 0m,
                GastosAdministracion = sueldosAdministrativos,
                MarketingComunicacion = marketing,
                ServiciosBasicos = decimal.Round(mant?.CostoServiciosBasicos ?? 0m, 2),
                AmortizacionActivosDiferidos = amortizacionPeriodo,
                ImprevistosRecargo = imprevistos,
                GastoFinanciero = 0m,
                TotalBecasGobierno = invPeriodo?.TotalBecasGobierno ?? 0m
            });
        }

        var rubros = ConstruirRubrosBase(valores);
        return new MatrizCostosGastosDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? demanda.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? demanda.EscenarioNombre,
            Periodos = periodos,
            ValoresPorPeriodo = valores,
            ProyeccionCostosGastos = ConstruirFilasCostos(rubros, valores.Select(v => v.TotalCostosGastos).ToList()),
            Ponderacion = ConstruirPonderacion(rubros, valores.Select(v => v.TotalCostosGastos).ToList()),
            DescontadoBecasGobierno = ConstruirDescontado(rubros, valores),
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

    private async Task<ProyeccionEstudiantesDto?> ObtenerProyeccionAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken ct)
    {
        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(carreraId, escenarioProyeccionId, ct);
        return proyeccionId is null or <= 0
            ? null
            : await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
    }

    private async Task<ProyeccionConsolidadaDto?> ObtenerConsolidadoAsync(
        ProyeccionEstudiantesDto proyeccion,
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken ct)
    {
        var configuracion = (await repositorioConfiguracionRetencion.ListarDtoAsync(ct))
            .FirstOrDefault(c => c.CarreraId == carreraId && c.EscenarioProyeccionId == escenarioProyeccionId);
        if (configuracion is null)
            return null;

        var overrides = await repositorioOverrides.ListarPorProyeccionAsync(proyeccion.Id, ct);
        var (horasDocencia, horasPractica) = ConstruirArreglosOverride(overrides, proyeccion);

        return ConsolidadorProyeccionEstudiantes.Calcular(
            proyeccion,
            configuracion.ParalelosPeriodo1,
            configuracion.ParalelosPeriodo2,
            configuracion.MetaRetencionPorcentaje ?? configuracion.TasaRetencionPorcentaje,
            configuracion.MetaGraduacionPorcentaje ?? configuracion.TasaGraduacionPorcentaje,
            horasDocSemestralesOverride: horasDocencia,
            horasTecSemestralesOverride: horasPractica,
            horasDocSemanaOverride: 18m,
            horasTecSemanaOverride: 40m);
    }

    private static (decimal[]? doc, decimal[]? prac) ConstruirArreglosOverride(
        IReadOnlyList<OverrideHorasPeriodo> overrides,
        ProyeccionEstudiantesDto proyeccion)
    {
        if (overrides.Count == 0)
            return (null, null);

        var totalPeriodos = proyeccion.Detalles.Select(d => d.NumeroPeriodo).DefaultIfEmpty(0).Max();
        if (totalPeriodos <= 0)
            return (null, null);

        var doc = new decimal[totalPeriodos];
        var prac = new decimal[totalPeriodos];
        var hayDoc = false;
        var hayPrac = false;

        foreach (var o in overrides)
        {
            var idx = o.Periodo - 1;
            if (idx < 0 || idx >= totalPeriodos)
                continue;

            if (o.HorasDocencia is { } hd)
            {
                doc[idx] = hd;
                hayDoc = true;
            }

            if (o.HorasPractica is { } hp)
            {
                prac[idx] = hp;
                hayPrac = true;
            }
        }

        return (hayDoc ? doc : null, hayPrac ? prac : null);
    }

    private static IReadOnlyList<RubroBase> ConstruirRubrosBase(IReadOnlyList<CostoGastoPeriodoDto> valores)
    {
        IReadOnlyList<decimal> V(Func<CostoGastoPeriodoDto, decimal> selector)
            => valores.Select(selector).Select(v => decimal.Round(v, 2)).ToList();

        return
        [
            new("1. Costos por servicios", "Mantenimiento", V(x => x.Mantenimiento)),
            new("1. Costos por servicios", "Capacitación docente", V(x => x.CapacitacionDocente)),
            new("1. Costos por servicios", "Sueldos docentes", V(x => x.SueldosDocentes)),
            new("1. Costos por servicios", "Seguro estudiantil", V(x => x.SeguroEstudiantil)),
            new("1. Costos por servicios", "Becas institucionales", V(x => x.BecasInstitucionales)),
            new("1. Costos por servicios", "Investigación", V(x => x.Investigacion)),
            new("1. Costos por servicios", "Vinculación", V(x => x.Vinculacion)),
            new("1. Costos por servicios", "Depreciación", V(x => x.Depreciacion)),
            new("2. Gastos de administración", "Sueldos administrativos", V(x => x.GastosAdministracion)),
            new("3. Gastos de ventas", "Marketing y comunicación", V(x => x.MarketingComunicacion)),
            new("4. Otros gastos", "Servicios básicos", V(x => x.ServiciosBasicos)),
            new("4. Otros gastos", "Amortización activos diferidos", V(x => x.AmortizacionActivosDiferidos)),
            new("4. Otros gastos", "Imprevistos / recargo", V(x => x.ImprevistosRecargo)),
            new("5. Gasto financiero", "Intereses préstamo", V(x => x.GastoFinanciero))
        ];
    }

    private static IReadOnlyList<CostoGastoRubroDto> ConstruirFilasCostos(
        IReadOnlyList<RubroBase> rubros,
        IReadOnlyList<decimal> totales)
    {
        var filas = new List<CostoGastoRubroDto>();
        string? grupoActual = null;
        foreach (var rubro in rubros)
        {
            if (!string.Equals(grupoActual, rubro.Grupo, StringComparison.Ordinal))
            {
                grupoActual = rubro.Grupo;
                filas.Add(CrearEncabezado(grupoActual, totales.Count));
            }

            filas.Add(CrearFila(rubro.Grupo, rubro.Concepto, rubro.Valores));
        }

        filas.Add(CrearFila("6. Total Costos y Gastos", "Total Costos y Gastos", totales, esTotal: true));
        return filas;
    }

    private static IReadOnlyList<PonderacionCostoGastoDto> ConstruirPonderacion(
        IReadOnlyList<RubroBase> rubros,
        IReadOnlyList<decimal> totales)
    {
        var filas = new List<PonderacionCostoGastoDto>();
        var totalGeneral = totales.Sum();
        string? grupoActual = null;
        foreach (var rubro in rubros)
        {
            if (!string.Equals(grupoActual, rubro.Grupo, StringComparison.Ordinal))
            {
                grupoActual = rubro.Grupo;
                filas.Add(new PonderacionCostoGastoDto
                {
                    Grupo = grupoActual,
                    Concepto = grupoActual,
                    Periodos = Enumerable.Repeat(0m, totales.Count).ToList(),
                    EsEncabezadoGrupo = true
                });
            }

            var valores = rubro.Valores
                .Select((valor, i) => totales[i] > 0m ? decimal.Round(valor / totales[i], 6) : 0m)
                .ToList();
            filas.Add(new PonderacionCostoGastoDto
            {
                Grupo = rubro.Grupo,
                Concepto = rubro.Concepto,
                Periodos = valores,
                Total = totalGeneral > 0m ? decimal.Round(rubro.Valores.Sum() / totalGeneral, 6) : 0m
            });
        }

        filas.Add(new PonderacionCostoGastoDto
        {
            Grupo = "6. Total Costos y Gastos",
            Concepto = "Total ponderación",
            Periodos = totales.Select(t => t > 0m ? 1m : 0m).ToList(),
            Total = totalGeneral > 0m ? 1m : 0m,
            EsTotal = true
        });
        return filas;
    }

    private static IReadOnlyList<CostoGastoRubroDto> ConstruirDescontado(
        IReadOnlyList<RubroBase> rubros,
        IReadOnlyList<CostoGastoPeriodoDto> valoresPeriodo)
    {
        var totales = valoresPeriodo.Select(v => v.TotalCostosGastos).ToList();
        var becasGobierno = valoresPeriodo.Select(v => v.TotalBecasGobierno).ToList();
        var filas = new List<CostoGastoRubroDto>();
        var totalesDescontados = Enumerable.Repeat(0m, valoresPeriodo.Count).ToArray();
        string? grupoActual = null;

        foreach (var rubro in rubros)
        {
            if (!string.Equals(grupoActual, rubro.Grupo, StringComparison.Ordinal))
            {
                grupoActual = rubro.Grupo;
                filas.Add(CrearEncabezado(grupoActual, valoresPeriodo.Count));
            }

            var descontados = rubro.Valores
                .Select((valor, i) =>
                {
                    var ponderacion = totales[i] > 0m ? valor / totales[i] : 0m;
                    var descontado = decimal.Round(valor - becasGobierno[i] * ponderacion, 2);
                    totalesDescontados[i] += descontado;
                    return descontado;
                })
                .ToList();
            filas.Add(CrearFila(rubro.Grupo, rubro.Concepto, descontados));
        }

        filas.Add(CrearFila("6. Total Costos y Gastos", "Total descontado becas gobierno", totalesDescontados, esTotal: true));
        return filas;
    }

    private static CostoGastoRubroDto CrearEncabezado(string grupo, int periodos)
        => new()
        {
            Grupo = grupo,
            Concepto = grupo,
            Periodos = Enumerable.Repeat(0m, periodos).ToList(),
            EsEncabezadoGrupo = true
        };

    private static CostoGastoRubroDto CrearFila(
        string grupo,
        string concepto,
        IReadOnlyList<decimal> valores,
        bool esTotal = false)
        => new()
        {
            Grupo = grupo,
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

    private static MatrizCostosGastosDto Vacia(
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
