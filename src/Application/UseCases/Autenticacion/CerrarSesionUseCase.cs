using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Autenticacion;

public sealed class CerrarSesionUseCase(
    IRepositorioSesionUsuario repositorioSesion,
    IAuditoriaServicio auditoriaServicio)
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

        try
        {
            using var ctsAuditoria = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsAuditoria.CancelAfter(TimeSpan.FromSeconds(4));
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Autenticacion",
                entidadNombre: "SesionUsuario",
                entidadId: usuarioId.ToString(),
                accionNombre: "LOGOUT",
                resumenTexto: $"Cierre de sesión del usuario Id {usuarioId}.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: ctsAuditoria.Token);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] CerrarSesionUseCase: auditoría de logout omitida por error/transitorio -> {ex.Message}.");
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] CerrarSesionUseCase: logout local completado.");
    }
}
