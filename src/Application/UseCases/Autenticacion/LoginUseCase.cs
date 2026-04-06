using SistemaAranceles.Application.DTOs.Autenticacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.Autenticacion;

public sealed class LoginUseCase(
    IRepositorioUsuario repositorioUsuario,
    IRepositorioRol repositorioRol,
    IRepositorioSesionUsuario repositorioSesion,
    IServicioHash servicioHash,
    IAuditoriaServicio auditoriaServicio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task<SesionDto> EjecutarAsync(
        string correo,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(correo))
            throw new ArgumentException("El correo es obligatorio.");

        if (string.IsNullOrWhiteSpace(contrasena))
            throw new ArgumentException("La contraseña es obligatoria.");

        var usuario = await repositorioUsuario.ObtenerPorCorreoInstitucionalAsync(correo, cancellationToken)
            ?? throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");

        if (!servicioHash.Verificar(contrasena, usuario.HashContrasena))
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Autenticacion",
                entidadNombre: "Usuario",
                entidadId: correo,
                accionNombre: "LOGIN_FALLIDO",
                resumenTexto: $"Intento de login fallido para correo '{correo}'.",
                cancellationToken: cancellationToken);

            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        }

        var estadoStr = usuario.Estado.ToString();
        if (!estadoStr.Equals("Activo", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"La cuenta está {estadoStr.ToLower()}. Contacte al administrador.");

        var roles = await repositorioRol.ObtenerNombresDeRolesDelUsuarioAsync(usuario.Id, cancellationToken);
        var rolNombre = roles.FirstOrDefault() ?? "Sin rol";

        var token = Guid.NewGuid().ToString("N");
        var expira = DateTime.UtcNow.AddHours(8);

        await repositorioSesion.CrearAsync(usuario.Id, token, expira, cancellationToken);

        usuario.RegistrarAcceso(DateTime.UtcNow);
        await repositorioUsuario.ActualizarAsync(usuario, cancellationToken);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        await auditoriaServicio.RegistrarAsync(
            moduloNombre: "Autenticacion",
            entidadNombre: "Usuario",
            entidadId: usuario.Id.ToString(),
            accionNombre: "LOGIN_EXITOSO",
            resumenTexto: $"Login exitoso para '{usuario.NombreCompleto}' con rol '{rolNombre}'.",
            ejecutadoPorUsuarioId: usuario.Id,
            cancellationToken: cancellationToken);

        return new SesionDto
        {
            UsuarioId = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Correo = usuario.CorreoInstitucional.ToString(),
            RolNombre = rolNombre,
            TokenSesion = token
        };
    }
}
