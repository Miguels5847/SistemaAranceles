using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class ListarConfiguracionesRetencionUseCase(IRepositorioConfiguracionRetencion repositorioConfiguracion)
{
    public async Task<IReadOnlyList<ConfiguracionRetencionDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        return await repositorioConfiguracion.ListarDtoAsync(cancellationToken);
    }
}
