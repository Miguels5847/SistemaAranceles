using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Carreras que tienen al menos una proyección de estudiantes guardada.
/// </summary>
public sealed class ListarCarrerasConProyeccionQuery(
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioCarrera repositorioCarrera)
{
    public async Task<IReadOnlyList<Carrera>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var resumenes = await repositorioProyeccion.ListarResumenAsync(null, null, cancellationToken);
        if (resumenes.Count == 0)
            return [];

        var idsConProyeccion = resumenes.Select(r => r.CarreraId).Distinct().ToHashSet();
        var carreras = await repositorioCarrera.ListarAsync();
        return carreras
            .Where(c => idsConProyeccion.Contains(c.Id))
            .OrderBy(c => c.Nombre)
            .ToList();
    }
}

/// <summary>
/// Escenarios de proyección de una carrera que tienen una proyección de estudiantes asociada.
/// </summary>
public sealed class ListarEscenariosConProyeccionPorCarreraQuery(
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioEscenarioProyeccion repositorioEscenario)
{
    public async Task<IReadOnlyList<EscenarioProyeccion>> EjecutarAsync(
        int carreraId,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            return [];

        var resumenes = await repositorioProyeccion.ListarResumenAsync(carreraId, null, cancellationToken);
        if (resumenes.Count == 0)
            return [];

        var idsEscenario = resumenes.Select(r => r.EscenarioProyeccionId).Distinct().ToHashSet();
        var escenarios = await repositorioEscenario.ListarAsync(cancellationToken);
        return escenarios
            .Where(e => idsEscenario.Contains(e.Id))
            .OrderByDescending(e => e.EsPredeterminado)
            .ThenBy(e => e.Nombre)
            .ToList();
    }
}

/// <summary>
/// Períodos disponibles en la proyección de estudiantes de una carrera+escenario.
/// </summary>
public sealed class ListarPeriodosDeProyeccionEstudiantesQuery(
    IRepositorioProyeccionEstudiantes repositorioProyeccion)
{
    public async Task<IReadOnlyList<PeriodoDisponibleSueldosDto>> EjecutarAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0 || escenarioProyeccionId <= 0)
            return [];

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId,
            escenarioProyeccionId,
            cancellationToken);

        if (proyeccionId is null or 0)
            return [];

        var detalle = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, cancellationToken);
        if (detalle is null)
            return [];

        return detalle.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .Select(g =>
            {
                var primero = g.First();
                return new PeriodoDisponibleSueldosDto
                {
                    PeriodoAcademicoId = primero.PeriodoAcademicoId,
                    Anio = primero.Anio,
                    NumeroPeriodo = primero.NumeroPeriodo,
                    EtiquetaPeriodo = primero.EtiquetaPeriodo,
                };
            })
            .OrderBy(p => p.Anio)
            .ThenBy(p => p.NumeroPeriodo)
            .ToList();
    }
}

public sealed class PeriodoDisponibleSueldosDto
{
    public int PeriodoAcademicoId { get; init; }
    public int Anio { get; init; }
    public int NumeroPeriodo { get; init; }
    public string EtiquetaPeriodo { get; init; } = string.Empty;
    public string DisplayText => $"{Anio}-P{NumeroPeriodo}: {EtiquetaPeriodo}";
}
