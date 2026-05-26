using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

public sealed class ObtenerDemandaProyectadaQuery(
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario)
{
    public async Task<DemandaProyectadaDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
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

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId,
            escenarioProyeccionId.Value,
            ct);

        if (proyeccionId is null or <= 0)
            return Vacio("No existe proyeccion generada para esta carrera y escenario. Generela primero en Proyeccion de Estudiantes.");

        var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return Vacio("La proyeccion de estudiantes existe, pero no tiene detalles. Generela nuevamente en Proyeccion de Estudiantes.");

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

        return new DemandaProyectadaDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? proyeccion.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? proyeccion.EscenarioNombre,
            EtiquetasPeriodos = periodos.Select(p => p.EtiquetaPeriodo).ToList(),
            AniosPeriodos = periodos.Select(p => p.Anio).ToList(),
            NumerosPeriodos = periodos.Select(p => p.NumeroPeriodo).ToList(),
            Filas = filas,
            TotalesPorPeriodo = totales
        };
    }
}
