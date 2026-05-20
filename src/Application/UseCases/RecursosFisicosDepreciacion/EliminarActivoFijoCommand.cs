using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

/// <summary>
/// Borrado logico (soft delete): no rompe calculos historicos de depreciacion.
/// </summary>
public sealed class EliminarActivoFijoCommand(
    IRepositorioActivoFijo repositorioActivoFijo,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(int id, int? eliminadoPorUsuarioId, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser mayor a cero.");

        var activo = await repositorioActivoFijo.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontro el activo fijo con Id {id}.");

        repositorioActivoFijo.EliminarLogico(activo, eliminadoPorUsuarioId);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}
