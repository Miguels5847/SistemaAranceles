using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class ObtenerConfiguracionRetencionUseCase(IRepositorioConfiguracionRetencion repositorioConfiguracion)
{
    public async Task<ConfiguracionRetencionDto> EjecutarAsync(int id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("El Id debe ser mayor a cero.", nameof(id));

        var dto = await repositorioConfiguracion.ObtenerDtoPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la configuración de retención con Id {id}.");

        return dto;
    }
}
