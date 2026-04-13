using SistemaAranceles.Application.DTOs.Permisos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.Permisos;

public sealed class ObtenerPermisosEfectivosUsuarioUseCase(IRepositorioPermiso repositorioPermiso)
{
    public Task<IReadOnlyList<PermisoConEstadoDto>> EjecutarAsync(
        int usuarioId,
        CancellationToken cancellationToken = default)
        => repositorioPermiso.ObtenerTodosConEstadoParaUsuarioAsync(usuarioId, cancellationToken);
}
