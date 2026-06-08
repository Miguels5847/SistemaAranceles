using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// Siembra en una carrera los consumos por defecto de "5. Materiales en Cantidades"
/// (ratio_material_demanda). Asegura primero los items de Capital de Trabajo (para que el ratio
/// pueda vincular su precio) y luego inserta los ratios faltantes. Idempotente.
/// </summary>
public sealed class GenerarRatiosPorDefectoCarreraCommand(
    IRepositorioItemMaterialInsumo repositorioItems,
    IRepositorioRatioMaterialDemanda repositorioRatios)
{
    public async Task<int> EjecutarAsync(int carreraId, CancellationToken ct = default)
    {
        if (carreraId <= 0)
            throw new ArgumentException("Carrera es obligatoria.", nameof(carreraId));

        await repositorioItems.SembrarItemsPorDefectoAsync(carreraId, CatalogoMaterialesPorDefecto.Items, ct);
        return await repositorioRatios.SembrarRatiosPorDefectoAsync(carreraId, CatalogoRatiosPorDefecto.Items, ct);
    }
}
