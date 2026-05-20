using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioInversionFutura
{
    Task<InversionFutura?> ObtenerPorActivoPeriodoAsync(
        int activoFijoId,
        int anio,
        int semestre,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InversionFutura>> ListarPorActivosAsync(
        IReadOnlyCollection<int> activoFijoIds,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(InversionFutura inversion, CancellationToken cancellationToken = default);

    void Actualizar(InversionFutura inversion);

    void EliminarLogico(InversionFutura inversion, int? eliminadoPorUsuarioId = null);
}
