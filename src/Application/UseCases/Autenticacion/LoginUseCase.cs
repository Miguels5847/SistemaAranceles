using SistemaAranceles.Application.DTOs.Autenticacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Autenticacion;

public sealed class LoginUseCase(
    IRepositorioUsuario repositorioUsuario,
    IRepositorioRol repositorioRol,
    IRepositorioSesionUsuario repositorioSesion,
    IServicioHash servicioHash,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<SesionDto> EjecutarAsync(
        string correo,
        string contrasena,
        CancellationToken cancellationToken = default)
    {
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: inicio para correo '{correo}'.");

        if (string.IsNullOrWhiteSpace(correo))
            throw new ArgumentException("El correo es obligatorio.");

        if (string.IsNullOrWhiteSpace(contrasena))
            throw new ArgumentException("La contraseña es obligatoria.");

        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: consultando usuario por correo.");
        var usuario = await repositorioUsuario.ObtenerPorCorreoInstitucionalAsync(correo, cancellationToken)
            ?? throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: usuario encontrado Id={usuario.Id}.");

        if (!servicioHash.Verificar(contrasena, usuario.HashContrasena))
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: hash inválido para correo '{correo}'.");
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

        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: obteniendo roles de usuario.");
        var rolNombre = "Sin rol";
        var swRoles = Stopwatch.StartNew();

        try
        {
            using var ctsRoles = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsRoles.CancelAfter(TimeSpan.FromSeconds(2));

            var roles = await repositorioRol.ObtenerNombresDeRolesDelUsuarioAsync(usuario.Id, ctsRoles.Token);
            rolNombre = roles.FirstOrDefault() ?? InferirRolDeRespaldo(usuario.Id, usuario.CorreoInstitucional.ToString());
        }
        catch (Exception ex)
        {
            rolNombre = InferirRolDeRespaldo(usuario.Id, usuario.CorreoInstitucional.ToString());
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: error leyendo roles -> {ex.Message}. Se usará rol de respaldo '{rolNombre}'.");
        }
        finally
        {
            swRoles.Stop();
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase.Metric: roles_ms={swRoles.ElapsedMilliseconds}.");
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: rol resuelto '{rolNombre}'.");

        var token = Guid.NewGuid().ToString("N");
        var expira = DateTime.UtcNow.AddHours(8);

        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: creando sesión en base de datos.");
        var swSesion = Stopwatch.StartNew();
        await repositorioSesion.CrearAsync(usuario.Id, token, expira, cancellationToken);
        swSesion.Stop();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase.Metric: sesion_ms={swSesion.ElapsedMilliseconds}.");

        // Temporalmente fuera de ruta crítica por latencia intermitente en BD.
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: update/auditoría de login omitidos (modo resiliente). ");
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase.Metric: update_ms=-1.");
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase.Metric: auditoria_ms=-1.");

        var dto = new SesionDto
        {
            UsuarioId = usuario.Id,
            NombreCompleto = usuario.NombreCompleto,
            Correo = usuario.CorreoInstitucional.ToString(),
            RolNombre = rolNombre,
            TokenSesion = token
        };

        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: fin exitoso para usuario Id={dto.UsuarioId}.");
        return dto;
    }

    private static string InferirRolDeRespaldo(int usuarioId, string correo)
    {
        if (usuarioId == 1 || correo.Equals("admin@ucacue.edu.ec", StringComparison.OrdinalIgnoreCase))
            return "Administrador";

        return "Sin rol";
    }
}
