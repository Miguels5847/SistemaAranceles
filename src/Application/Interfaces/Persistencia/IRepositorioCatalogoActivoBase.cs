using SistemaAranceles.Domain.Constantes;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioCatalogoActivoBase
{
    Task<IReadOnlyList<CatalogoActivoBase>> ListarActivosAsync(CancellationToken ct = default);
    Task<CatalogoActivoBase?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task AgregarAsync(CatalogoActivoBase item, CancellationToken ct = default);
    void Actualizar(CatalogoActivoBase item);

    /// <summary>Agrega al catálogo global las plantillas faltantes (dedup por descripción). Devuelve cuántas se agregaron. No persiste.</summary>
    Task<int> SembrarPorDefectoAsync(IReadOnlyList<ActivoBasePorDefecto> items, CancellationToken ct = default);
}
