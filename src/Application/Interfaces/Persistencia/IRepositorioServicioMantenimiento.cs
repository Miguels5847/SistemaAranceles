using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioServicioMantenimiento
{
    Task<ServicioMantenimiento?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ServicioMantenimiento>> ListarPorCarreraAsync(
        int carreraId,
        TipoRubroMantenimiento? tipo = null,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default);
    Task<decimal> SumarCostoAnualPorTipoAsync(
        int carreraId,
        TipoRubroMantenimiento tipo,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default);
    Task AgregarAsync(ServicioMantenimiento servicio, CancellationToken ct = default);
    void Actualizar(ServicioMantenimiento servicio);
    void EliminarLogico(ServicioMantenimiento servicio, int? eliminadoPorUsuarioId);
}
