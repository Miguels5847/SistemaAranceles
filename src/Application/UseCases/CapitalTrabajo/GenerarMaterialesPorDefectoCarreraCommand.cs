using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;

namespace SistemaAranceles.Application.UseCases.CapitalTrabajo;

/// <summary>
/// Siembra en una carrera los materiales/suministros del catálogo por defecto (B/C/D).
/// Idempotente: solo inserta los que faltan. Devuelve cuántos se crearon.
/// </summary>
public sealed class GenerarMaterialesPorDefectoCarreraCommand(IRepositorioItemMaterialInsumo repositorio)
{
    public async Task<int> EjecutarAsync(int carreraId, CancellationToken ct = default)
    {
        if (carreraId <= 0)
            throw new ArgumentException("Carrera es obligatoria.", nameof(carreraId));

        return await repositorio.SembrarItemsPorDefectoAsync(carreraId, CatalogoMaterialesPorDefecto.Items, ct);
    }
}
