using System.Globalization;
using System.Text;
using SistemaAranceles.Application.DTOs.CargosFacultad;
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
using SistemaAranceles.Application.UseCases.SueldosPlantaCentral;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CostosGastos;

public sealed class ObtenerMatrizCostosGastosQuery(
    ObtenerMatrizInvVinBecasQuery obtenerMatrizInvVinBecasQuery,
    ObtenerDemandaProyectadaQuery obtenerDemandaProyectadaQuery,
    ObtenerResumenMantenimientoQuery obtenerResumenMantenimientoQuery,
    ObtenerMatrizDepreciacionQuery obtenerMatrizDepreciacionQuery,
    ObtenerTablaAmortizacionQuery obtenerTablaAmortizacionQuery,
    GenerarTablaSueldosPeriodoQuery generarTablaSueldosPeriodoQuery,
    GenerarResumenSueldosQuery generarResumenSueldosQuery,
    CalcularMaterialesPorPeriodoQuery calcularMaterialesPorPeriodoQuery,
    CalcularAportePlantaCentralCarreraQuery calcularAportePlantaCentralCarreraQuery,
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
        MatrizInvVinBecasDto? invVinBecasPrecalculado = null,
        DemandaProyectadaDto? demandaPrecalculada = null,
        decimal factorImprevisto = 1.05m,
        IReadOnlyDictionary<int, decimal>? becasInstitucionalesPorPeriodo = null,
        ProyeccionEstudiantesDto? proyeccionPrecalculada = null)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;
        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
            return Vacia(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId, escenario?.Nombre ?? string.Empty, "Selecciona un escenario para consolidar Costos y Gastos.");
        if (factorImprevisto <= 0m)
            return Vacia(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId, escenario?.Nombre ?? string.Empty, "El factor imprevisto debe ser mayor a 0.");

        var demanda = demandaPrecalculada
            ?? await obtenerDemandaProyectadaQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertenciaDocentes);
        if (!demanda.TieneDatos || demanda.EtiquetasPeriodos.Count == 0)
            return Vacia(carreraId, carrera?.Nombre ?? demanda.CarreraNombre, escenarioProyeccionId, demanda.EscenarioNombre, ConstruirMensaje(advertencias, "No hay demanda proyectada para consolidar costos."));

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        if (datos is null)
            advertencias.Add("No hay Datos Institucionales vigentes. Se usaron defaults para porcentajes y rubros institucionales.");

        // Reutiliza la matriz Inv. Vin. Becas si ya fue calculada (evita recomputar Ingresos/arancel).
        var invVinBecas = invVinBecasPrecalculado
            ?? await obtenerMatrizInvVinBecasQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, demandaPrecalculada: demanda, becasInstitucionalesPorPeriodo: becasInstitucionalesPorPeriodo);
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

        var materiales = await calcularMaterialesPorPeriodoQuery.EjecutarAsync(carreraId, escenarioProyeccionId.Value, ct);
        AgregarAdvertencia(advertencias, materiales.MensajeAdvertencia);
        var materialesPorPeriodo = materiales.Monetarios
            .GroupBy(m => m.PeriodoAcademicoId)
            .ToDictionary(g => g.Key, g => decimal.Round(g.Sum(m => m.Costo), 2));

        var aportePlantaCentralPorPeriodo = await ObtenerAportePlantaCentralPorPeriodoAsync(
            carreraId,
            escenarioProyeccionId.Value,
            advertencias,
            ct);

        var anioBase = periodos.Min(p => p.Anio);
        var amortizacion = await obtenerTablaAmortizacionQuery.EjecutarAsync(carreraId, anioBase, ct);
        var amortizacionPorAnio = amortizacion.Anios
            .Select((anio, index) => new
            {
                Anio = anio,
                Valor = index < amortizacion.TotalesPorAnio.Count ? amortizacion.TotalesPorAnio[index] : 0m
            })
            .ToDictionary(x => x.Anio, x => x.Valor);

        var proyeccion = proyeccionPrecalculada ?? await ObtenerProyeccionAsync(carreraId, escenarioProyeccionId.Value, ct);
        var consolidado = proyeccion is null
            ? null
            : await ObtenerConsolidadoAsync(proyeccion, carreraId, escenarioProyeccionId.Value, ct);

        var estudiantesUnidadAcademicaSueldos = ConfiguracionSueldosCarrera.EstudiantesUnidadAcademicaPorDefecto;
        var resumenSueldos = await generarResumenSueldosQuery.EjecutarAsync(
            carreraId,
            escenarioProyeccionId.Value,
            estudiantesUnidadAcademicaSueldos,
            consolidado,
            ct);

        // Contexto de sueldos cargado una sola vez (proyección, cargos, inflación) para calcular
        // cada período en memoria y evitar N+1 de consultas dentro del bucle de períodos.
        var contextoSueldos = await generarTablaSueldosPeriodoQuery.PrepararContextoAsync(
            carreraId,
            escenarioProyeccionId.Value,
            ct);

        var valores = new List<CostoGastoPeriodoDto>();
        var semestresPorAnio = datos?.SemestresPorAnio is > 0 ? datos.SemestresPorAnio : DatosInstitucionales.SemestresPorAnioPorDefecto;
        var estudiantesUniversidad = datos?.NumeroEstudiantesUniversidad ?? 0;
        var docentesUniversidad = datos?.NumeroDocentesUniversidad ?? 0;

        for (var i = 0; i < periodos.Count; i++)
        {
            var periodo = periodos[i];
            mantenimientoPorPeriodo.TryGetValue(periodo.NumeroPeriodo, out var mant);
            depreciacionPorPeriodo.TryGetValue(periodo.NumeroPeriodo, out var dep);

            var sueldos = contextoSueldos is null
                ? new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodo.PeriodoAcademicoId }
                : generarTablaSueldosPeriodoQuery.GenerarParaPeriodo(
                    contextoSueldos,
                    carreraId,
                    periodo.PeriodoAcademicoId,
                    estudiantesUnidadAcademicaSueldos,
                    consolidado);
            var tiempoCompletoPhd = AplicarFactor(ObtenerTotalSueldoPorConcepto(sueldos.Filas, true, "Tiempo Completo PhD", "TC PhD"), factorImprevisto);
            var tiempoCompletoMgs = AplicarFactor(ObtenerTotalSueldoPorConcepto(sueldos.Filas, true, "Tiempo Completo Mgs", "TC Mgs", "TC Mgs."), factorImprevisto);
            var medioTiempo = AplicarFactor(ObtenerTotalSueldoPorConcepto(sueldos.Filas, true, "Medio Tiempo"), factorImprevisto);
            var tiempoParcial = AplicarFactor(ObtenerTotalSueldoPorConcepto(sueldos.Filas, true, "Tiempo Parcial", "Docente Tiempo Parcial"), factorImprevisto);
            var ocasionalTipo2 = AplicarFactor(ObtenerTotalSueldoPorConcepto(sueldos.Filas, true, "Ocasional Tipo 2", "Técnico Docente", "Tecnico Docente"), factorImprevisto);
            var sueldosDocentes = tiempoCompletoPhd + tiempoCompletoMgs + medioTiempo + tiempoParcial + ocasionalTipo2;

            aportePlantaCentralPorPeriodo.TryGetValue(periodo.PeriodoAcademicoId, out var administracionCentral);
            var decano = ObtenerValorResumenSueldo(resumenSueldos, "Decano", i, advertencias);
            var subdecano = ObtenerValorResumenSueldo(resumenSueldos, "Subdecano", i, advertencias);
            var directorCarrera = ObtenerValorResumenSueldo(resumenSueldos, "Director de Carrera", i, advertencias);
            var secretario = ObtenerValorResumenSueldo(resumenSueldos, "Secretario", i, advertencias);
            var auxiliarSecretaria = ObtenerValorResumenSueldo(resumenSueldos, "Auxiliar de Secretaria", i, advertencias);
            var coordinador = ObtenerValorResumenSueldo(resumenSueldos, "Coordinador", i, advertencias);
            var bienestarEstudiantil = ObtenerValorResumenSueldo(resumenSueldos, "Bienestar Estudiantil", i, advertencias);
            var bibliotecario = ObtenerValorResumenSueldo(resumenSueldos, "Bibliotecario", i, advertencias);
            var auxiliarServicio = ObtenerValorResumenSueldo(resumenSueldos, "Auxiliar de Servicio", i, advertencias);
            var guardia = ObtenerValorResumenSueldo(resumenSueldos, "Guardia", i, advertencias);

            var capacitacion = datos is not null && docentesUniversidad > 0
                ? decimal.Round((datos.PresupuestoAnualCapacitacion / semestresPorAnio) * factores[i] / docentesUniversidad * docentes[i], 2)
                : 0m;
            var internacionalizacion = datos is not null && estudiantesUniversidad > 0
                ? decimal.Round((datos.PresupuestoAnualInternacionalizacion / semestresPorAnio) * factores[i] / estudiantesUniversidad * estudiantes[i], 2)
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
            var investigacion = decimal.Round((invPeriodo?.Investigacion ?? 0m) * factorImprevisto, 2);
            var vinculacion = decimal.Round((invPeriodo?.Vinculacion ?? 0m) * factorImprevisto, 2);
            materialesPorPeriodo.TryGetValue(periodo.PeriodoAcademicoId, out var materialesPeriodo);
            materialesPeriodo = AplicarFactor(materialesPeriodo, factorImprevisto);
            var gastoFinanciero = decimal.Round(0m * factorImprevisto, 2);
            var mantenimientoEdificio = decimal.Round(mant?.CostoMantenimiento ?? 0m, 2);
            var serviciosBasicos = decimal.Round(mant?.CostoServiciosBasicos ?? 0m, 2);
            var depreciacionPeriodo = decimal.Round(dep?.DepreciacionPeriodo ?? 0m, 2);

            valores.Add(new CostoGastoPeriodoDto
            {
                PeriodoAcademicoId = periodo.PeriodoAcademicoId,
                Anio = periodo.Anio,
                NumeroPeriodo = periodo.NumeroPeriodo,
                EtiquetaPeriodo = periodo.Etiqueta,
                Mantenimiento = mantenimientoEdificio,
                MantenimientoEdificio = mantenimientoEdificio,
                CapacitacionDocente = capacitacion,
                Internacionalizacion = internacionalizacion,
                InsumosPracticasLaboratorios = 0m,
                SueldosDocentes = sueldosDocentes,
                TiempoCompletoPhd = tiempoCompletoPhd,
                TiempoCompletoMgs = tiempoCompletoMgs,
                MedioTiempo = medioTiempo,
                TiempoParcial = tiempoParcial,
                OcasionalTipo2TecnicoDocente = ocasionalTipo2,
                SeguroEstudiantil = seguro,
                CostoSeguroEstudiantil = seguro,
                BecasInstitucionales = becasInstitucionales,
                Investigacion = investigacion,
                Vinculacion = vinculacion,
                MaterialesSuministros = materialesPeriodo,
                Depreciacion = depreciacionPeriodo,
                AdministracionCentral = decimal.Round(administracionCentral, 2),
                Decano = decano,
                Subdecano = subdecano,
                DirectorCarrera = directorCarrera,
                Secretario = secretario,
                AuxiliarSecretaria = auxiliarSecretaria,
                Coordinador = coordinador,
                BienestarEstudiantil = bienestarEstudiantil,
                Bibliotecario = bibliotecario,
                AuxiliarServicio = auxiliarServicio,
                Guardia = guardia,
                MarketingComunicacion = marketing,
                ServiciosBasicos = serviciosBasicos,
                AmortizacionActivosDiferidos = amortizacionPeriodo,
                Amortizacion = amortizacionPeriodo,
                ImprevistosRecargo = 0m,
                Interes = gastoFinanciero,
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

    private async Task<IReadOnlyDictionary<int, decimal>> ObtenerAportePlantaCentralPorPeriodoAsync(
        int carreraId,
        int escenarioProyeccionId,
        List<string> advertencias,
        CancellationToken ct)
    {
        try
        {
            var aporte = await calcularAportePlantaCentralCarreraQuery.EjecutarAsync(
                carreraId,
                escenarioProyeccionId,
                ct);

            return aporte.Periodos
                .GroupBy(p => p.PeriodoAcademicoId)
                .ToDictionary(
                    g => g.Key,
                    g => decimal.Round(g.Sum(p => p.AporteSemestral), 2));
        }
        catch (Exception ex)
        {
            advertencias.Add($"No se pudo calcular Administración Central; se mostrará 0. {Detalle(ex)}");
            return new Dictionary<int, decimal>();
        }
    }

    private static decimal AplicarFactor(decimal valor, decimal factor)
        => decimal.Round(valor * factor, 2);

    private static decimal ObtenerTotalSueldoPorConcepto(
        IReadOnlyList<FilaSueldoPeriodoDto> filas,
        bool? esCargoDocente,
        params string[] nombres)
    {
        var busquedas = nombres
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(NormalizarTokens)
            .Where(t => t.Count > 0)
            .ToList();

        if (busquedas.Count == 0)
            return 0m;

        var total = filas
            .Where(f => esCargoDocente is null || f.EsCargoDocente == esCargoDocente.Value)
            .Where(f =>
            {
                var tokensCargo = NormalizarTokens(f.NombreCargo);
                return busquedas.Any(busqueda => busqueda.All(tokensCargo.Contains));
            })
            .Sum(f => f.TotalSemestre);

        return decimal.Round(total, 2);
    }

    private static decimal ObtenerValorResumenSueldo(
        ResumenSueldosVistaDto resumen,
        string nombreCargo,
        int periodoIndex,
        List<string> advertencias)
    {
        var fila = resumen.Filas.FirstOrDefault(f => CoincideNombreCargo(f.NombreCargo, nombreCargo));
        if (fila is null)
        {
            advertencias.Add($"No se encontr\u00f3 el cargo '{nombreCargo}' en Resumen Sueldos Personal; se usar\u00e1 0 en Costos y Gastos.");
            return 0m;
        }

        if (periodoIndex < 0 || fila.ValoresPorPeriodo.Length <= periodoIndex)
        {
            advertencias.Add($"Resumen Sueldos Personal no tiene valor de per\u00edodo para '{nombreCargo}'; se usar\u00e1 0 en Costos y Gastos.");
            return 0m;
        }

        return decimal.Round(fila.ValoresPorPeriodo[periodoIndex], 2);
    }

    private static bool CoincideNombreCargo(string nombreA, string nombreB)
    {
        var normalizadoA = NormalizarTexto(nombreA);
        var normalizadoB = NormalizarTexto(nombreB);

        if (normalizadoA.Length == 0 || normalizadoB.Length == 0)
            return false;

        if (string.Equals(normalizadoA, normalizadoB, StringComparison.Ordinal))
            return true;

        if (EsOcasionalTipo2(normalizadoA) && EsOcasionalTipo2(normalizadoB))
            return true;

        var tokensA = NormalizarTokens(normalizadoA);
        var tokensB = NormalizarTokens(normalizadoB);
        return tokensA.IsSubsetOf(tokensB) || tokensB.IsSubsetOf(tokensA);
    }

    private static bool EsOcasionalTipo2(string textoNormalizado)
        => textoNormalizado.Contains("ocasional tipo 2", StringComparison.Ordinal)
           || textoNormalizado.Contains("tecnico docente", StringComparison.Ordinal);

    private static HashSet<string> NormalizarTokens(string texto)
        => NormalizarTexto(texto)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static string NormalizarTexto(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalizado.Length);

        foreach (var c in normalizado)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;

            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }

        return sb.ToString().Normalize(NormalizationForm.FormC).Trim();
    }

    private static IReadOnlyList<RubroBase> ConstruirRubrosBase(IReadOnlyList<CostoGastoPeriodoDto> valores)
    {
        IReadOnlyList<decimal> V(Func<CostoGastoPeriodoDto, decimal> selector)
            => valores.Select(selector).Select(v => decimal.Round(v, 2)).ToList();

        return
        [
            new("1. Costos por servicios", "Mantenimiento Edificio", V(x => x.MantenimientoEdificio)),
            new("1. Costos por servicios", "Capacitación Docente", V(x => x.CapacitacionDocente)),
            new("1. Costos por servicios", "Internacionalización", V(x => x.Internacionalizacion)),
            new("1. Costos por servicios", "Insumos Prácticas y Laboratorios", V(x => x.InsumosPracticasLaboratorios)),
            new("1. Costos por servicios", "Tiempo Completo PhD (18 horas)", V(x => x.TiempoCompletoPhd)),
            new("1. Costos por servicios", "Tiempo Completo Mgs. (18 horas)", V(x => x.TiempoCompletoMgs)),
            new("1. Costos por servicios", "Medio Tiempo(12 horas)", V(x => x.MedioTiempo)),
            new("1. Costos por servicios", "Tiempo Parcial (6 h)", V(x => x.TiempoParcial)),
            new("1. Costos por servicios", "Ocasional Tipo 2 (Técnico Docente)", V(x => x.OcasionalTipo2TecnicoDocente)),
            new("1. Costos por servicios", "Costo del Seguro Estudiantil", V(x => x.CostoSeguroEstudiantil)),
            new("1. Costos por servicios", "Becas Institucionales 10 % estudiantes", V(x => x.BecasInstitucionales)),
            new("1. Costos por servicios", "Investigación 5%", V(x => x.Investigacion)),
            new("1. Costos por servicios", "Vinculación 1%", V(x => x.Vinculacion)),
            new("1. Costos por servicios", "Materiales y Suministros", V(x => x.MaterialesSuministros)),
            new("1. Costos por servicios", "Depreciación", V(x => x.Depreciacion)),
            new("2. Gastos de administración", "Administración Central", V(x => x.AdministracionCentral)),
            new("2. Gastos de administración", "Decano", V(x => x.Decano)),
            new("2. Gastos de administración", "Subdecano", V(x => x.Subdecano)),
            new("2. Gastos de administración", "Director de Carrera", V(x => x.DirectorCarrera)),
            new("2. Gastos de administración", "Secretario", V(x => x.Secretario)),
            new("2. Gastos de administración", "Auxiliar de Secretaria", V(x => x.AuxiliarSecretaria)),
            new("2. Gastos de administración", "Coordinador", V(x => x.Coordinador)),
            new("2. Gastos de administración", "Bienestar Estudiantil", V(x => x.BienestarEstudiantil)),
            new("2. Gastos de administración", "Bibliotecario", V(x => x.Bibliotecario)),
            new("2. Gastos de administración", "Auxiliar de Servicio", V(x => x.AuxiliarServicio)),
            new("2. Gastos de administración", "Guardia", V(x => x.Guardia)),
            new("3. Gastos de ventas", "Marketing y Comunicación", V(x => x.MarketingComunicacion)),
            new("4. Otros gastos", "Servicios Básicos", V(x => x.ServiciosBasicos)),
            new("4. Otros gastos", "Amortizacion", V(x => x.Amortizacion)),
            new("5. Gasto financiero", "Interes", V(x => x.Interes))
        ];
    }

    private static IReadOnlyList<CostoGastoRubroDto> ConstruirFilasCostos(
        IReadOnlyList<RubroBase> rubros,
        IReadOnlyList<decimal> totales)
    {
        var filas = new List<CostoGastoRubroDto>();
        foreach (var grupo in rubros.GroupBy(r => r.Grupo))
        {
            var rubrosGrupo = grupo.ToList();
            filas.Add(CrearFilaGrupo(grupo.Key, SumarPorPeriodo(rubrosGrupo.Select(r => r.Valores), totales.Count)));

            foreach (var rubro in rubrosGrupo)
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
        foreach (var grupo in rubros.GroupBy(r => r.Grupo))
        {
            var rubrosGrupo = grupo.ToList();
            var subtotalesGrupo = SumarPorPeriodo(rubrosGrupo.Select(r => r.Valores), totales.Count);
            filas.Add(new PonderacionCostoGastoDto
            {
                Grupo = grupo.Key,
                Concepto = grupo.Key,
                Periodos = subtotalesGrupo
                    .Select((valor, i) => totales[i] > 0m ? decimal.Round(valor / totales[i], 6) : 0m)
                    .ToList(),
                Total = totalGeneral > 0m ? decimal.Round(subtotalesGrupo.Sum() / totalGeneral, 6) : 0m,
                EsEncabezadoGrupo = true
            });

            foreach (var rubro in rubrosGrupo)
            {
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

        foreach (var grupo in rubros.GroupBy(r => r.Grupo))
        {
            var rubrosGrupo = grupo.ToList();
            var filasGrupo = new List<CostoGastoRubroDto>(rubrosGrupo.Count);

            foreach (var rubro in rubrosGrupo)
            {
                var descontados = rubro.Valores
                    .Select((valor, i) =>
                    {
                        var ponderacion = totales[i] > 0m ? valor / totales[i] : 0m;
                        var descontado = decimal.Round(valor - becasGobierno[i] * ponderacion, 2);
                        totalesDescontados[i] += descontado;
                        return descontado;
                    })
                    .ToList();
                filasGrupo.Add(CrearFila(rubro.Grupo, rubro.Concepto, descontados));
            }

            filas.Add(CrearFilaGrupo(grupo.Key, SumarPorPeriodo(filasGrupo.Select(f => f.Periodos), valoresPeriodo.Count)));
            filas.AddRange(filasGrupo);
        }

        filas.Add(CrearFila("6. Total Costos y Gastos", "Total descontado becas gobierno", totalesDescontados, esTotal: true));
        return filas;
    }

    private static CostoGastoRubroDto CrearFilaGrupo(string grupo, IReadOnlyList<decimal> valores)
        => new()
        {
            Grupo = grupo,
            Concepto = grupo,
            Periodos = valores.Select(v => decimal.Round(v, 2)).ToList(),
            Total = decimal.Round(valores.Sum(), 2),
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

    private static IReadOnlyList<decimal> SumarPorPeriodo(IEnumerable<IReadOnlyList<decimal>> filas, int cantidadPeriodos)
    {
        var totales = new decimal[cantidadPeriodos];
        foreach (var fila in filas)
        {
            for (var i = 0; i < cantidadPeriodos; i++)
                totales[i] += i < fila.Count ? fila[i] : 0m;
        }

        return totales.Select(v => decimal.Round(v, 2)).ToList();
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

    private static string Detalle(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;

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
