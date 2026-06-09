using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Infrastructure.Servicios;

/// <summary>
/// Adaptador de Recursos -> módulo Estudiantes.
/// Lee la proyección activa de una carrera + escenario y expone el total de estudiantes
/// de un semestre concreto sin persistir nada adicional en Recursos.
/// </summary>
public sealed class ServicioEstudiantesTotales(
    IRepositorioProyeccionEstudiantes repositorioProyeccion) : IServicioEstudiantesTotales
{
    public Task<decimal> ObtenerTotalEstudiantesPorSemestreAsync(
        int carreraId,
        int escenarioProyeccionId,
        int anio,
        int numeroPeriodo,
        CancellationToken ct = default)
        => ServiciosDemandaRecursosHelper.ObtenerTotalEstudiantesAsync(
            repositorioProyeccion,
            carreraId,
            escenarioProyeccionId,
            anio,
            numeroPeriodo,
            ct);
}

/// <summary>
/// Adaptador de Recursos -> lógica consolidada de docentes requeridos.
/// Usa la misma construcción del consolidado que consume Sueldos para que Zoom
/// lea el "Docentes Requeridos" real del semestre inicial.
/// </summary>
public sealed class ServicioDocentesTotales(
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioConfiguracionRetencion repositorioConfiguracion,
    IRepositorioOverrideHorasPeriodo repositorioOverrides) : IServicioDocentesTotales
{
    public async Task<decimal> ObtenerTotalDocentesPorSemestreAsync(
        int carreraId,
        int escenarioProyeccionId,
        int anio,
        int numeroPeriodo,
        CancellationToken ct = default)
    {
        var proyeccion = await ServiciosDemandaRecursosHelper.ObtenerProyeccionAsync(
            repositorioProyeccion,
            carreraId,
            escenarioProyeccionId,
            ct);

        if (proyeccion is null || proyeccion.Detalles.Count == 0)
        {
            return 0m;
        }

        var configuracion = await ServiciosDemandaRecursosHelper.ObtenerConfiguracionAsync(
            repositorioConfiguracion,
            carreraId,
            escenarioProyeccionId,
            ct);

        if (configuracion is null)
        {
            return 0m;
        }

        var overrides = await repositorioOverrides.ListarPorProyeccionAsync(proyeccion.Id, ct);
        var (docOverride, tecOverride) = ServiciosDemandaRecursosHelper.ConstruirArreglosOverride(
            overrides,
            proyeccion);

        var tasaRetencion = configuracion.MetaRetencionPorcentaje ?? configuracion.TasaRetencionPorcentaje;
        var tasaGraduacion = configuracion.MetaGraduacionPorcentaje ?? configuracion.TasaGraduacionPorcentaje;

        var consolidado = ConsolidadorProyeccionEstudiantes.Calcular(
            proyeccion,
            configuracion.ParalelosPeriodo1,
            configuracion.ParalelosPeriodo2,
            tasaRetencion,
            tasaGraduacion,
            horasDocSemestralesOverride: docOverride,
            horasTecSemestralesOverride: tecOverride,
            horasDocSemanaOverride: 18m,
            horasTecSemanaOverride: 40m);

        var periodoRelativo = ServiciosDemandaRecursosHelper.ResolverNumeroPeriodoRelativo(
            proyeccion.AnioBase,
            anio,
            numeroPeriodo);

        if (periodoRelativo <= 0)
        {
            return 0m;
        }

        var filaDocentes = consolidado.DocentesPorPeriodo.FirstOrDefault(x =>
            x.Tipo.Equals("Docentes Requeridos", StringComparison.OrdinalIgnoreCase));

        if (filaDocentes is null || filaDocentes.Periodos.Length < periodoRelativo)
        {
            return 0m;
        }

        return filaDocentes.Periodos[periodoRelativo - 1];
    }

    public async Task<IReadOnlyDictionary<int, decimal>> ObtenerTotalesDocentesPorPeriodoAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var vacio = new Dictionary<int, decimal>();

        var proyeccion = await ServiciosDemandaRecursosHelper.ObtenerProyeccionAsync(
            repositorioProyeccion,
            carreraId,
            escenarioProyeccionId,
            ct);

        if (proyeccion is null || proyeccion.Detalles.Count == 0)
        {
            return vacio;
        }

        var configuracion = await ServiciosDemandaRecursosHelper.ObtenerConfiguracionAsync(
            repositorioConfiguracion,
            carreraId,
            escenarioProyeccionId,
            ct);

        if (configuracion is null)
        {
            return vacio;
        }

        var overrides = await repositorioOverrides.ListarPorProyeccionAsync(proyeccion.Id, ct);
        var (docOverride, tecOverride) = ServiciosDemandaRecursosHelper.ConstruirArreglosOverride(
            overrides,
            proyeccion);

        var tasaRetencion = configuracion.MetaRetencionPorcentaje ?? configuracion.TasaRetencionPorcentaje;
        var tasaGraduacion = configuracion.MetaGraduacionPorcentaje ?? configuracion.TasaGraduacionPorcentaje;

        var consolidado = ConsolidadorProyeccionEstudiantes.Calcular(
            proyeccion,
            configuracion.ParalelosPeriodo1,
            configuracion.ParalelosPeriodo2,
            tasaRetencion,
            tasaGraduacion,
            horasDocSemestralesOverride: docOverride,
            horasTecSemestralesOverride: tecOverride,
            horasDocSemanaOverride: 18m,
            horasTecSemanaOverride: 40m);

        var filaDocentes = consolidado.DocentesPorPeriodo.FirstOrDefault(x =>
            x.Tipo.Equals("Docentes Requeridos", StringComparison.OrdinalIgnoreCase));

        if (filaDocentes is null)
        {
            return vacio;
        }

        var totales = new Dictionary<int, decimal>(filaDocentes.Periodos.Length);
        for (var i = 0; i < filaDocentes.Periodos.Length; i++)
        {
            totales[i + 1] = filaDocentes.Periodos[i];
        }

        return totales;
    }
}

public sealed class ServicioInflacionFactorStub : IServicioInflacionFactor
{
    public Task<decimal> ObtenerFactorAcumuladoAsync(
        int anio, int numeroPeriodo, CancellationToken ct = default)
        => Task.FromResult(1m);
}

internal static class ServiciosDemandaRecursosHelper
{
    public static async Task<decimal> ObtenerTotalEstudiantesAsync(
        IRepositorioProyeccionEstudiantes repositorioProyeccion,
        int carreraId,
        int escenarioProyeccionId,
        int anio,
        int numeroPeriodo,
        CancellationToken ct)
    {
        var proyeccion = await ObtenerProyeccionAsync(repositorioProyeccion, carreraId, escenarioProyeccionId, ct);

        if (proyeccion is null || proyeccion.Detalles.Count == 0)
        {
            return 0m;
        }

        var periodoRelativo = ResolverNumeroPeriodoRelativo(proyeccion.AnioBase, anio, numeroPeriodo);
        if (periodoRelativo <= 0)
        {
            return 0m;
        }

        return decimal.Round(
            proyeccion.Detalles
                .Where(d => d.NumeroPeriodo == periodoRelativo)
                .Sum(d => d.TotalEstudiantes),
            4);
    }

    public static async Task<ProyeccionEstudiantesDto?> ObtenerProyeccionAsync(
        IRepositorioProyeccionEstudiantes repositorioProyeccion,
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken ct)
    {
        if (carreraId <= 0 || escenarioProyeccionId <= 0)
        {
            return null;
        }

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId,
            escenarioProyeccionId,
            ct);

        if (proyeccionId is null or <= 0)
        {
            return null;
        }

        return await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
    }

    public static async Task<ConfiguracionRetencionDto?> ObtenerConfiguracionAsync(
        IRepositorioConfiguracionRetencion repositorioConfiguracion,
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken ct)
    {
        var configuraciones = await repositorioConfiguracion.ListarDtoAsync(ct);
        return configuraciones.FirstOrDefault(c =>
            c.CarreraId == carreraId &&
            c.EscenarioProyeccionId == escenarioProyeccionId);
    }

    public static int ResolverNumeroPeriodoRelativo(int anioBase, int anio, int numeroPeriodo)
    {
        if (numeroPeriodo <= 0 || anio < anioBase)
        {
            return 0;
        }

        return ((anio - anioBase) * 2) + numeroPeriodo;
    }

    public static (decimal[]? doc, decimal[]? prac) ConstruirArreglosOverride(
        IReadOnlyList<OverrideHorasPeriodo> overrides,
        ProyeccionEstudiantesDto proyeccion)
    {
        if (overrides.Count == 0)
        {
            return (null, null);
        }

        var totalPeriodos = proyeccion.Detalles.Select(d => d.NumeroPeriodo).DefaultIfEmpty(0).Max();
        if (totalPeriodos <= 0)
        {
            return (null, null);
        }

        var doc = new decimal[totalPeriodos];
        var prac = new decimal[totalPeriodos];
        var hayDoc = false;
        var hayPrac = false;

        foreach (var o in overrides)
        {
            var idx = o.Periodo - 1;
            if (idx < 0 || idx >= totalPeriodos)
            {
                continue;
            }

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
