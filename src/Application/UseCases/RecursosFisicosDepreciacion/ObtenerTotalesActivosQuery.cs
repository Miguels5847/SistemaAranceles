using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

/// <summary>
/// Subtotales por categoria y total general — replica la fila TOTAL del bloque 1
/// de la hoja "3 Recursos fisicos" (Excel E62).
/// </summary>
public sealed class ObtenerTotalesActivosQuery(IRepositorioActivoFijo repositorioActivoFijo)
{
    public async Task<TotalesActivosFijosDto> EjecutarAsync(int carreraId, CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(carreraId), "La carrera debe ser mayor a cero.");

        var activos = await repositorioActivoFijo.ListarPorCarreraAsync(carreraId, null, cancellationToken);

        var porCategoria = activos
            .GroupBy(x => x.Categoria)
            .OrderBy(g => g.Key)
            .Select(g => new TotalCategoriaActivosDto
            {
                Categoria = g.Key,
                CategoriaNombre = MapeoActivoFijo.NombreCategoria(g.Key),
                CantidadItems = g.Count(),
                SubtotalValorTotal = g.Sum(x => x.ValorTotal)
            })
            .ToList();

        return new TotalesActivosFijosDto
        {
            PorCategoria = porCategoria,
            TotalGeneral = porCategoria.Sum(x => x.SubtotalValorTotal)
        };
    }
}
