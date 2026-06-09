using SistemaAranceles.Domain.Constantes;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioServicioMantenimiento
{
    Task<ServicioMantenimiento?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Agrega los rubros faltantes (dedup por tipo+nombre). Si el escenario tiene rubros específicos,
    /// completa esos (son los que la vista muestra y tapan los generales); si no, completa la config
    /// general (escenario NULL). Devuelve cuántos se agregaron. No persiste.
    /// </summary>
    Task<int> SembrarPorDefectoAsync(int carreraId, int? escenarioProyeccionId, IReadOnlyList<ServicioMantenimientoPorDefecto> items, CancellationToken ct = default);

    Task<IReadOnlyList<ServicioMantenimiento>> ListarPorCarreraAsync(
        int carreraId,
        TipoRubroMantenimiento? tipo = null,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default);
    /// <summary>Suma el costo anual de TODOS los tipos en una sola consulta (agrupada por tipo).</summary>
    Task<IReadOnlyDictionary<TipoRubroMantenimiento, decimal>> SumarCostosAnualesPorTipoAsync(
        int carreraId,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default);
    Task AgregarAsync(ServicioMantenimiento servicio, CancellationToken ct = default);
    void Actualizar(ServicioMantenimiento servicio);
    void EliminarLogico(ServicioMantenimiento servicio, int? eliminadoPorUsuarioId);
}
