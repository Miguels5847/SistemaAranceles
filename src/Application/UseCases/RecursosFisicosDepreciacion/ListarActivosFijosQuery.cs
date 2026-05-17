using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public sealed class ListarActivosFijosQuery(IRepositorioActivoFijo repositorioActivoFijo)
{
    public async Task<IReadOnlyList<ActivoFijoDto>> EjecutarAsync(
        int carreraId,
        CategoriaActivoFijo? categoria = null,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(carreraId), "La carrera debe ser mayor a cero.");

        var activos = await repositorioActivoFijo.ListarPorCarreraAsync(carreraId, categoria, cancellationToken);

        return activos
            .OrderBy(x => x.Categoria)
            .ThenBy(x => x.Descripcion)
            .Select(MapeoActivoFijo.ADto)
            .ToList();
    }
}
