using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public sealed class ListarActivosFijosQuery(
    IRepositorioActivoFijo repositorioActivoFijo,
    IRepositorioProyeccionEstudiantes repositorioProyeccionEstudiantes,
    IServicioEstudiantesTotales servicioEstudiantesTotales,
    IServicioDocentesTotales servicioDocentesTotales)
{
    public async Task<IReadOnlyList<ActivoFijoDto>> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId = null,
        CategoriaActivoFijo? categoria = null,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(carreraId), "La carrera debe ser mayor a cero.");

        var activos = await repositorioActivoFijo.ListarPorCarreraAsync(carreraId, categoria, cancellationToken);
        var contexto = await ResolverContextoCalculoAsync(carreraId, escenarioProyeccionId, cancellationToken);

        return activos
            .OrderBy(x => x.Categoria)
            .ThenBy(x => x.Descripcion)
            .Select(x => MapeoActivoFijo.ADto(
                x,
                contexto.UsarCantidadCalculada,
                contexto.TotalEstudiantesInicial,
                contexto.TotalDocentesInicial))
            .ToList();
    }

    private async Task<ContextoCalculoCantidadInicial> ResolverContextoCalculoAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken cancellationToken)
    {
        if (escenarioProyeccionId is null or <= 0)
        {
            return ContextoCalculoCantidadInicial.SinCalculo;
        }

        var proyeccionId = await repositorioProyeccionEstudiantes.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId,
            escenarioProyeccionId.Value,
            cancellationToken);

        if (proyeccionId is null or <= 0)
        {
            return ContextoCalculoCantidadInicial.SinCalculo;
        }

        var proyeccion = await repositorioProyeccionEstudiantes.ObtenerDtoPorIdAsync(proyeccionId.Value, cancellationToken);
        if (proyeccion is null)
        {
            return ContextoCalculoCantidadInicial.SinCalculo;
        }

        var totalEstudiantesInicial = await servicioEstudiantesTotales.ObtenerTotalEstudiantesPorSemestreAsync(
            carreraId,
            escenarioProyeccionId.Value,
            proyeccion.AnioBase,
            numeroPeriodo: 1,
            ct: cancellationToken);

        var totalDocentesInicial = await servicioDocentesTotales.ObtenerTotalDocentesPorSemestreAsync(
            carreraId,
            escenarioProyeccionId.Value,
            proyeccion.AnioBase,
            numeroPeriodo: 1,
            ct: cancellationToken);

        return new ContextoCalculoCantidadInicial(true, totalEstudiantesInicial, totalDocentesInicial);
    }

    private readonly record struct ContextoCalculoCantidadInicial(
        bool UsarCantidadCalculada,
        decimal TotalEstudiantesInicial,
        decimal TotalDocentesInicial)
    {
        public static ContextoCalculoCantidadInicial SinCalculo => new(false, 0m, 0m);
    }
}
