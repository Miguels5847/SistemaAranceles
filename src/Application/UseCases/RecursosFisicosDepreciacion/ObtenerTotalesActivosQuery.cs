using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

/// <summary>
/// Subtotales por categoria y total general — replica la fila TOTAL del bloque 1
/// de la hoja "3 Recursos fisicos" (Excel E62).
/// </summary>
public sealed class ObtenerTotalesActivosQuery(
    IRepositorioActivoFijo repositorioActivoFijo,
    IRepositorioProyeccionEstudiantes repositorioProyeccionEstudiantes,
    IServicioEstudiantesTotales servicioEstudiantesTotales,
    IServicioDocentesTotales servicioDocentesTotales)
{
    public async Task<TotalesActivosFijosDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId = null,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(carreraId), "La carrera debe ser mayor a cero.");

        var activos = await repositorioActivoFijo.ListarPorCarreraAsync(carreraId, null, cancellationToken);
        var contexto = await ResolverContextoCalculoAsync(carreraId, escenarioProyeccionId, cancellationToken);

        var porCategoria = activos
            .GroupBy(x => new
            {
                x.Categoria,
                Nombre = MapeoActivoFijo.NombreCategoria(x.Categoria, x.CategoriaPersonalizada)
            })
            .OrderBy(g => g.Key.Categoria)
            .ThenBy(g => g.Key.Nombre)
            .Select(g => new TotalCategoriaActivosDto
            {
                Categoria = g.Key.Categoria,
                CategoriaNombre = g.Key.Nombre,
                CantidadItems = g.Count(),
                SubtotalValorTotal = g.Sum(x => MapeoActivoFijo.ResolverValorTotalMostrado(
                    x,
                    contexto.UsarCantidadCalculada,
                    contexto.TotalEstudiantesInicial,
                    contexto.TotalDocentesInicial))
            })
            .ToList();

        return new TotalesActivosFijosDto
        {
            PorCategoria = porCategoria,
            TotalGeneral = porCategoria.Sum(x => x.SubtotalValorTotal)
        };
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
