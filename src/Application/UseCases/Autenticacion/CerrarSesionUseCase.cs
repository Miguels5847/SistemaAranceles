using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.Autenticacion;

public sealed class CerrarSesionUseCase(
    IRepositorioSesionUsuario repositorioSesion,
    IAuditoriaServicio auditoriaServicio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(
        int usuarioId,
        string tokenSesion,
        CancellationToken cancellationToken = default)
    {
        await repositorioSesion.RevocarAsync(tokenSesion, cancellationToken);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        await auditoriaServicio.RegistrarAsync(
            moduloNombre: "Autenticacion",
            entidadNombre: "Usuario",
            entidadId: usuarioId.ToString(),
            accionNombre: "LOGOUT",
            resumenTexto: $"Cierre de sesión del usuario Id {usuarioId}.",
            ejecutadoPorUsuarioId: usuarioId,
            cancellationToken: cancellationToken);
    }
}
