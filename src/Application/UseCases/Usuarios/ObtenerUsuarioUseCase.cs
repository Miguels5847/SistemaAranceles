using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class ObtenerUsuarioUseCase(IRepositorioUsuario repositorioUsuario)
{
    public async Task<UsuarioDto?> EjecutarAsync(int id, CancellationToken cancellationToken = default)
    {
        var usuario = await repositorioUsuario.ObtenerPorIdAsync(id, cancellationToken);
        if (usuario is null)
        {
            return null;
        }

        var roles = await repositorioUsuario.ObtenerRolesDelUsuarioAsync(id, cancellationToken);

        return new UsuarioDto
        {
            Id = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            CorreoInstitucional = usuario.CorreoInstitucional.ToString(),
            Estado = usuario.Estado.ToString(),
            UltimoAccesoEn = usuario.UltimoAccesoEn,
            Roles = roles
        };
    }
}
