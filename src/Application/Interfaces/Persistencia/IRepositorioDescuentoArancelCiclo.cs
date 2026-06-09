using SistemaAranceles.Application.DTOs.DemandaIngresos;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioDescuentoArancelCiclo
{
    /// <summary>Lista los descuentos activos exactos de la carrera+escenario indicados (sin fallback).</summary>
    Task<IReadOnlyList<DescuentoArancelCicloDto>> ListarPorCarreraEscenarioAsync(
        int carreraId, int? escenarioProyeccionId, CancellationToken ct = default);

    /// <summary>Descuentos efectivos: carrera+escenario; si no hay, cae a la config global carrera+null.</summary>
    Task<IReadOnlyList<DescuentoArancelCicloDto>> ListarEfectivosPorCarreraEscenarioAsync(
        int carreraId, int? escenarioProyeccionId, CancellationToken ct = default);

    Task<int> GuardarAsync(GuardarDescuentoArancelCicloDto dto, int? usuarioId, CancellationToken ct = default);
    Task DesactivarAsync(int id, int? usuarioId, CancellationToken ct = default);
}
