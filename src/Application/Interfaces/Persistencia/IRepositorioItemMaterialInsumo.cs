using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Domain.Constantes;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioItemMaterialInsumo
{
    Task<IReadOnlyList<ItemCapitalTrabajoDto>> ListarPorCarreraAsync(
        int carreraId,
        string? categoria = null,
        CancellationToken ct = default);

    Task GuardarAsync(GuardarItemCapitalTrabajoDto dto, CancellationToken ct = default);

    /// <summary>
    /// Inserta los ítems del catálogo que aún no existan para la carrera (dedup por nombre).
    /// Idempotente: reejecutar no duplica. Devuelve cuántos se insertaron.
    /// </summary>
    Task<int> SembrarItemsPorDefectoAsync(
        int carreraId,
        IReadOnlyList<MaterialPorDefecto> items,
        CancellationToken ct = default);

    Task EliminarAsync(int id, CancellationToken ct = default);

    Task<decimal> SumarCostoMensualPorCategoriaAsync(int carreraId, string categoria, CancellationToken ct = default);
}
