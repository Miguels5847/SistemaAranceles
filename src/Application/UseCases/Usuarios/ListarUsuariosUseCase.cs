using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class ListarUsuariosUseCase(IRepositorioUsuario repositorioUsuario)
{
    public async Task<IReadOnlyList<UsuarioDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var usuarios = await repositorioUsuario.ListarAsync(cancellationToken);

        IReadOnlyDictionary<int, IReadOnlyList<string>> rolesPorUsuario = new Dictionary<int, IReadOnlyList<string>>();
        try
        {
            using var ctsRoles = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsRoles.CancelAfter(TimeSpan.FromSeconds(15));
            rolesPorUsuario = await repositorioUsuario.ObtenerRolesPorUsuariosAsync(
                usuarios.Select(x => x.Id),
                ctsRoles.Token);
        }
        catch (Exception ex) when (EsErrorTransitorio(ex))
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] ListarUsuariosUseCase: carga masiva de roles falló transitoriamente -> {ex.Message}. Se continuará con roles vacíos.");
        }

        var dtos = usuarios.Select(u => new UsuarioDto
        {
            Id = u.Id,
            NombreCompleto = u.NombreCompleto,
            CorreoInstitucional = u.CorreoInstitucional.ToString(),
            Estado = u.Estado.ToString(),
            UltimoAccesoEn = u.UltimoAccesoEn,
            Roles = rolesPorUsuario.TryGetValue(u.Id, out var roles) ? roles : []
        }).ToList();

        return dtos;
    }

    private static bool EsErrorTransitorio(Exception ex)
    {
        if (ex is TimeoutException || ex is OperationCanceledException)
            return true;

        return ex.Message.Contains("stream", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("transient", StringComparison.OrdinalIgnoreCase);
    }
}
