using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioConfiguracionArancelCarrera
{
    Task<IReadOnlyList<ConfiguracionArancelCarreraDto>> ListarAsync(int? carreraId = null, CancellationToken ct = default);
    Task<ConfiguracionArancelCarreraDto?> ObtenerPorCarreraEscenarioAsync(int carreraId, int? escenarioProyeccionId, CancellationToken ct = default);
    Task<ConfiguracionArancelCarrera?> ObtenerDominioAsync(int id, CancellationToken ct = default);
    Task<int> GuardarAsync(GuardarConfiguracionArancelCarreraDto dto, int? usuarioId, CancellationToken ct = default);
    Task EliminarAsync(int id, int? usuarioId, CancellationToken ct = default);
}
