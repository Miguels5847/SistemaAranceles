using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CapitalTrabajo;

/// <summary>
/// Construye KAN-29 segun la hoja "6 Capital de trabajo".
/// Bloque A se calcula desde Sueldos Carrera (primer periodo); B/C/D salen de items por carrera.
/// </summary>
public sealed class ObtenerCapitalTrabajoPorCarreraQuery(
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioConfiguracionRetencion repositorioConfiguracion,
    IRepositorioOverrideHorasPeriodo repositorioOverrides,
    IRepositorioItemMaterialInsumo repositorioMateriales,
    GenerarTablaSueldosPeriodoQuery generarTablaSueldosPeriodoQuery)
{
    private const int MesesCapitalTrabajoPorDefecto = 2;
    private const decimal EstudiantesUnidadAcademicaPorDefecto = 285m;
    public const string CategoriaMateriales = "MATERIALES_SUMINISTROS";
    public const string CategoriaAseo = "ASEO_LIMPIEZA";
    public const string CategoriaAccesorios = "ACCESORIOS_MATERIALES";

    public async Task<CapitalTrabajoPorCarreraDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;

        var materiales = await repositorioMateriales.ListarPorCarreraAsync(carreraId, CategoriaMateriales, ct);
        var aseo = await repositorioMateriales.ListarPorCarreraAsync(carreraId, CategoriaAseo, ct);
        var accesorios = await repositorioMateriales.ListarPorCarreraAsync(carreraId, CategoriaAccesorios, ct);

        var periodoBase = await ResolverPeriodoBaseAsync(carreraId, escenarioProyeccionId, ct);
        var gastos = periodoBase is null
            ? []
            : await ObtenerGastosServicioAdministracionAsync(
                carreraId,
                escenarioProyeccionId!.Value,
                periodoBase.PeriodoAcademicoId,
                ct);

        return new CapitalTrabajoPorCarreraDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? string.Empty,
            EscenarioProyeccionId = escenario?.Id,
            EscenarioNombre = escenario?.Nombre ?? string.Empty,
            PeriodoBaseId = periodoBase?.PeriodoAcademicoId,
            PeriodoBaseDisplay = periodoBase?.DisplayText ?? "Sin periodo base",
            MesesCapitalTrabajo = MesesCapitalTrabajoPorDefecto,
            GastosServicioAdministracion = gastos,
            MaterialesSuministros = materiales,
            AseoLimpieza = aseo,
            AccesoriosMateriales = accesorios
        };
    }

    private async Task<PeriodoDisponibleSueldosDto?> ResolverPeriodoBaseAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct)
    {
        if (escenarioProyeccionId is null or <= 0)
            return null;

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId,
            escenarioProyeccionId.Value,
            ct);

        if (proyeccionId is null or <= 0)
            return null;

        var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return null;

        return proyeccion.Detalles
            .GroupBy(x => x.PeriodoAcademicoId)
            .Select(g =>
            {
                var primero = g.First();
                return new PeriodoDisponibleSueldosDto
                {
                    PeriodoAcademicoId = primero.PeriodoAcademicoId,
                    Anio = primero.Anio,
                    NumeroPeriodo = primero.NumeroPeriodo,
                    EtiquetaPeriodo = primero.EtiquetaPeriodo
                };
            })
            .OrderBy(x => x.Anio)
            .ThenBy(x => x.NumeroPeriodo)
            .FirstOrDefault();
    }

    private async Task<IReadOnlyList<FilaGastoServicioAdministracionDto>> ObtenerGastosServicioAdministracionAsync(
        int carreraId,
        int escenarioProyeccionId,
        int periodoAcademicoId,
        CancellationToken ct)
    {
        var consolidado = await ObtenerConsolidadoAsync(carreraId, escenarioProyeccionId, ct);
        var tabla = await generarTablaSueldosPeriodoQuery.EjecutarAsync(
            carreraId,
            escenarioProyeccionId,
            periodoAcademicoId,
            EstudiantesUnidadAcademicaPorDefecto,
            consolidado,
            ct);

        return tabla.Filas
            .Select(f => new FilaGastoServicioAdministracionDto
            {
                Cantidad = f.NumeroPersonas,
                Concepto = f.NombreCargo,
                ValorUnitario = f.SueldoMensual,
                ValorMensual = decimal.Round(f.TotalSemestre / 6m, 2)
            })
            .Where(f => f.Cantidad > 0m || f.ValorMensual > 0m)
            .ToList();
    }

    private async Task<ProyeccionConsolidadaDto?> ObtenerConsolidadoAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken ct)
    {
        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId,
            escenarioProyeccionId,
            ct);

        if (proyeccionId is null or <= 0)
            return null;

        var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return null;

        var configuracion = (await repositorioConfiguracion.ListarDtoAsync(ct))
            .FirstOrDefault(c => c.CarreraId == carreraId && c.EscenarioProyeccionId == escenarioProyeccionId);
        if (configuracion is null)
            return null;

        var overrides = await repositorioOverrides.ListarPorProyeccionAsync(proyeccion.Id, ct);
        var (docOverride, tecOverride) = ConstruirArreglosOverride(overrides, proyeccion);

        return ConsolidadorProyeccionEstudiantes.Calcular(
            proyeccion,
            configuracion.ParalelosPeriodo1,
            configuracion.ParalelosPeriodo2,
            configuracion.MetaRetencionPorcentaje ?? configuracion.TasaRetencionPorcentaje,
            configuracion.MetaGraduacionPorcentaje ?? configuracion.TasaGraduacionPorcentaje,
            horasDocSemestralesOverride: docOverride,
            horasTecSemestralesOverride: tecOverride,
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
}

public sealed class ObtenerResumenCapitalTrabajoQuery(ObtenerCapitalTrabajoPorCarreraQuery capitalTrabajoQuery)
{
    public async Task<ResumenCapitalTrabajoDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default)
    {
        var capitalTrabajo = await capitalTrabajoQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        return capitalTrabajo.Resumen;
    }
}
