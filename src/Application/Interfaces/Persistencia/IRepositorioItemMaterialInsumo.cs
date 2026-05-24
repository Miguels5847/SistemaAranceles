using SistemaAranceles.Application.DTOs.CapitalTrabajo;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioItemMaterialInsumo
{
    Task<IReadOnlyList<ItemCapitalTrabajoDto>> ListarPorCarreraAsync(
        int carreraId,
        string? categoria = null,
        CancellationToken ct = default);

    Task GuardarAsync(GuardarItemCapitalTrabajoDto dto, CancellationToken ct = default);

    Task EliminarAsync(int id, CancellationToken ct = default);

    Task<decimal> SumarCostoMensualPorCategoriaAsync(int carreraId, string categoria, CancellationToken ct = default);
}
