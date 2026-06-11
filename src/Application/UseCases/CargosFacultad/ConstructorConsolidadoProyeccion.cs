using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Construye la proyección consolidada (estudiantes/docentes por período) a partir de la
/// configuración de retención y los overrides de horas — la misma receta que usa la matriz
/// de Costos y Gastos. Sin consolidado, los cargos DOCENTES pagan $0 (no hay personas), por
/// eso toda consulta de sueldos debe poder construirlo cuando el caller no lo provee (KAN-49).
/// </summary>
public static class ConstructorConsolidadoProyeccion
{
    public static async Task<ProyeccionConsolidadaDto?> ConstruirAsync(
        ProyeccionEstudiantesDto proyeccion,
        int carreraId,
        int escenarioProyeccionId,
        IRepositorioConfiguracionRetencion repositorioConfiguracionRetencion,
        IRepositorioOverrideHorasPeriodo repositorioOverrides,
        CancellationToken ct = default)
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

    public static (decimal[]? Docencia, decimal[]? Practica) ConstruirArreglosOverride(
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
