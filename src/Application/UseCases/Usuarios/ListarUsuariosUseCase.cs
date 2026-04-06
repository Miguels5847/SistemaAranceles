using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class ListarUsuariosUseCase(IRepositorioUsuario repositorioUsuario)
{
    public async Task<IReadOnlyList<UsuarioDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await repositorioUsuario.ListarAsync(cancellationToken);

        var dtos = new List<UsuarioDto>();
        foreach (var u in usuarios)
        {
            var roles = await repositorioUsuario.ObtenerRolesDelUsuarioAsync(u.Id, cancellationToken);
            dtos.Add(new UsuarioDto
            {
                Id = u.Id,
                NombreCompleto = u.NombreCompleto,
                CorreoInstitucional = u.CorreoInstitucional.ToString(),
                Estado = u.Estado.ToString(),
                UltimoAccesoEn = u.UltimoAccesoEn,
                Roles = roles
            });
        }

        return dtos;
    }
}
