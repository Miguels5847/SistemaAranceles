namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioItemMaterialInsumo
{
    Task<decimal> SumarCostoMensualPorCategoriaAsync(int carreraId, string categoria, CancellationToken ct = default);
}
