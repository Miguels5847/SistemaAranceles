using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public sealed class QuitarInversionFuturaCommand(
    IRepositorioInversionFutura repositorioInversionFutura,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(
        int activoFijoId,
        int anio,
        int semestre,
        int? usuarioId = null,
        CancellationToken cancellationToken = default)
    {
        var inversion = await repositorioInversionFutura.ObtenerPorActivoPeriodoAsync(
            activoFijoId,
            anio,
            semestre,
            cancellationToken);

        if (inversion is null)
            return;

        repositorioInversionFutura.EliminarLogico(inversion, usuarioId);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}
