using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class ListarUsuariosUseCase(IRepositorioUsuario repositorioUsuario)
{
    public async Task<IReadOnlyList<UsuarioDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await repositorioUsuario.ListarAsync(cancellationToken);

        var dtos = new List<UsuarioDto>();
        foreach (var u in usuarios)
        {
            IReadOnlyList<string> roles;
            try
            {
                roles = [];
                for (var intento = 1; intento <= 2; intento++)
                {
                    try
                    {
                        using var ctsRoles = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        ctsRoles.CancelAfter(TimeSpan.FromSeconds(20));
                        roles = await repositorioUsuario.ObtenerRolesDelUsuarioAsync(u.Id, ctsRoles.Token);
                        break;
                    }
                    catch (OperationCanceledException) when (intento == 1)
                    {
                        Trace.WriteLine($"[{DateTime.UtcNow:O}] ListarUsuariosUseCase: timeout transitorio en roles para usuario Id={u.Id} (intento 1). Reintentando.");
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"[{DateTime.UtcNow:O}] ListarUsuariosUseCase: error obteniendo roles para usuario Id={u.Id} -> {ex.Message}. Se continuará con roles vacíos.");
                roles = [];
            }

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
