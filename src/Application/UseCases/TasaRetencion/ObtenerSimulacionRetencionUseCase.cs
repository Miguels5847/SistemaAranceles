using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class ObtenerSimulacionRetencionUseCase(IRepositorioSimulacionRetencion repositorioSimulacion)
{
    public async Task<SimulacionRetencionDto> EjecutarAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("El Id de simulación debe ser mayor a cero.", nameof(id));

        return await repositorioSimulacion.ObtenerDtoPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la simulación con Id {id}.");
    }
}
