using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Estudiantes;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

public sealed class ObtenerDemandaProyectadaQuery(
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioConfiguracionRetencion repositorioConfiguracionRetencion,
    IRepositorioOverrideHorasPeriodo repositorioOverrideHorasPeriodo)
{
    public async Task<DemandaProyectadaDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        ProyeccionEstudiantesDto? proyeccionPrecalculada = null)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;

        DemandaProyectadaDto Vacio(string mensaje) => new()
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? string.Empty,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? string.Empty,
            MensajeAdvertencia = mensaje
        };

        if (escenarioProyeccionId is null or <= 0)
            return Vacio("Selecciona un escenario para ver la demanda proyectada.");

        var proyeccion = proyeccionPrecalculada;
        if (proyeccion is null)
        {
            var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
                carreraId,
                escenarioProyeccionId.Value,
                ct);

            if (proyeccionId is null or <= 0)
                return Vacio("No existe proyección generada para esta carrera y escenario. Genérela primero en Proyección de Estudiantes.");

            proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
        }

        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return Vacio("La proyección de estudiantes existe, pero no tiene detalles. Genérela nuevamente en Proyección de Estudiantes.");

        var periodos = proyeccion.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .Select(g =>
            {
                var primero = g.First();
                return new
                {
                    primero.PeriodoAcademicoId,
                    primero.Anio,
                    primero.NumeroPeriodo,
                    primero.EtiquetaPeriodo
                };
            })
            .OrderBy(p => p.Anio)
            .ThenBy(p => p.NumeroPeriodo)
            .ToList();

        var ciclos = proyeccion.Detalles
            .Select(d => d.NumeroCiclo)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        var filas = ciclos.Select(ciclo =>
        {
            var valores = periodos.Select(p => proyeccion.Detalles
                    .FirstOrDefault(d => d.NumeroCiclo == ciclo && d.PeriodoAcademicoId == p.PeriodoAcademicoId)
                    ?.TotalEstudiantes ?? 0m)
                .ToList();

            return new DemandaCicloFilaDto
            {
                NumeroCiclo = ciclo,
                Periodos = valores
            };
        }).ToList();

        var totales = periodos
            .Select(p => proyeccion.Detalles
                .Where(d => d.PeriodoAcademicoId == p.PeriodoAcademicoId)
                .Sum(d => d.TotalEstudiantes))
            .ToList();

        var (docentes, advertenciaDocentes) = await ConstruirDocentesPorPeriodoAsync(
            proyeccion,
            carreraId,
            escenarioProyeccionId.Value,
            periodos.Count,
            ct);

        return new DemandaProyectadaDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? proyeccion.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? proyeccion.EscenarioNombre,
            PeriodoAcademicoIds = periodos.Select(p => p.PeriodoAcademicoId).ToList(),
            EtiquetasPeriodos = periodos.Select(p => p.EtiquetaPeriodo).ToList(),
            AniosPeriodos = periodos.Select(p => p.Anio).ToList(),
            NumerosPeriodos = periodos.Select(p => p.NumeroPeriodo).ToList(),
            Filas = filas,
            TotalesPorPeriodo = totales,
            DocentesPorPeriodo = docentes,
            MensajeAdvertenciaDocentes = advertenciaDocentes
        };
    }

    private async Task<(IReadOnlyList<DemandaDocenteFilaDto> filas, string? advertencia)> ConstruirDocentesPorPeriodoAsync(
        ProyeccionEstudiantesDto proyeccion,
        int carreraId,
        int escenarioProyeccionId,
        int totalPeriodos,
        CancellationToken ct)
    {
        var configuraciones = await repositorioConfiguracionRetencion.ListarDtoAsync(ct);
        var configuracion = configuraciones.FirstOrDefault(c =>
            c.CarreraId == carreraId && c.EscenarioProyeccionId == escenarioProyeccionId);

        if (configuracion is null)
            return ([], "No existe configuración de retención para calcular docentes necesarios.");

        var overrides = await repositorioOverrideHorasPeriodo.ListarPorProyeccionAsync(proyeccion.Id, ct);
        var (horasDocencia, horasPractica) = ConstruirArreglosOverride(overrides, proyeccion);

        var tasaRet = configuracion.MetaRetencionPorcentaje ?? configuracion.TasaRetencionPorcentaje;
        var tasaGrad = configuracion.MetaGraduacionPorcentaje ?? configuracion.TasaGraduacionPorcentaje;

        var consolidado = ConsolidadorProyeccionEstudiantes.Calcular(
            proyeccion,
            configuracion.ParalelosPeriodo1,
            configuracion.ParalelosPeriodo2,
            tasaRet,
            tasaGrad,
            horasDocSemestralesOverride: horasDocencia,
            horasTecSemestralesOverride: horasPractica);

        var filas = consolidado.DocentesPorPeriodo
            .Select(f => new DemandaDocenteFilaDto
            {
                Tipo = f.Tipo,
                Periodos = f.Periodos.Take(totalPeriodos).Select(v => (decimal)v).ToList(),
                Total = f.Total
            })
            .ToList();

        return (filas, null);
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
