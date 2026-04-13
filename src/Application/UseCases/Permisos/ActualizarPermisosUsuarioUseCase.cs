using SistemaAranceles.Application.DTOs.Permisos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.Permisos;

public sealed class ActualizarPermisosUsuarioUseCase(IRepositorioPermiso repositorioPermiso)
{
    public Task EjecutarAsync(
        int usuarioId,
        IEnumerable<PermisoOverrideDto> overrides,
        int ejecutadoPorUsuarioId,
        CancellationToken cancellationToken = default)
        => repositorioPermiso.GuardarOverridesAsync(usuarioId, overrides, ejecutadoPorUsuarioId, cancellationToken);
}
