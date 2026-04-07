using SistemaAranceles.Application.Interfaces.Persistencia;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Autenticacion;

public sealed class CerrarSesionUseCase(
    IRepositorioSesionUsuario repositorioSesion)
{
    public async Task EjecutarAsync(
        int usuarioId,
        string tokenSesion,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await repositorioSesion.RevocarAsync(tokenSesion, cancellationToken);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] CerrarSesionUseCase: no se pudo revocar sesión -> {ex.Message}.");
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] CerrarSesionUseCase: logout local completado (modo resiliente, sin auditoría síncrona).");
    }
}
