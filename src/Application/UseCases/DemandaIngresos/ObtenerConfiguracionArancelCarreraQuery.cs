using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

public sealed class ObtenerConfiguracionArancelCarreraQuery(IRepositorioConfiguracionArancelCarrera repositorio)
{
    public Task<ConfiguracionArancelCarreraDto?> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
        => repositorio.ObtenerPorCarreraEscenarioAsync(carreraId, escenarioProyeccionId, ct);
}
