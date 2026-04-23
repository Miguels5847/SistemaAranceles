using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class ListarSimulacionesRetencionUseCase(IRepositorioSimulacionRetencion repositorioSimulacion)
{
    public Task<IReadOnlyList<ResumenSimulacionRetencionDto>> EjecutarAsync(
        int? carreraId = null,
        int? escenarioProyeccionId = null,
        int? cohorteAnio = null,
        CancellationToken cancellationToken = default)
        => repositorioSimulacion.ListarResumenAsync(carreraId, escenarioProyeccionId, cohorteAnio, cancellationToken);
}
