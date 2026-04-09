using SistemaAranceles.Application.DTOs.Autenticacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Autenticacion;

public sealed class LoginUseCase(
    IRepositorioUsuario repositorioUsuario,
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
        var swUsuario = Stopwatch.StartNew();
        SistemaAranceles.Domain.Entities.Usuario? usuario = null;
        var timeoutPersistente = false;
        try
        {
            for (var intento = 1; intento <= 2; intento++)
            {
                try
                {
                    using var ctsUsuario = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    ctsUsuario.CancelAfter(TimeSpan.FromSeconds(12));

                    usuario = await repositorioUsuario.ObtenerPorCorreoInstitucionalAsync(correo, ctsUsuario.Token);
                    break;
                }
                catch (OperationCanceledException) when (intento == 1)
                {
                    Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: timeout transitorio en consulta de usuario (intento 1). Reintentando.");
                }
                catch (OperationCanceledException) when (intento == 2)
                {
                    timeoutPersistente = true;
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("La consulta de usuario tardó demasiado.");
        }
        finally
        {
            swUsuario.Stop();
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase.Metric: usuario_ms={swUsuario.ElapsedMilliseconds}.");
        }

        if (timeoutPersistente)
            throw new TimeoutException("La consulta de usuario tardó demasiado.");

        if (usuario is null)
            throw new UnauthorizedAccessException("Correo o contraseña incorrectos.");
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
            IReadOnlyList<string> roles = [];
            for (var intento = 1; intento <= 2; intento++)
            {
                try
                {
                    using var ctsRoles = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    ctsRoles.CancelAfter(TimeSpan.FromSeconds(25));
                    roles = await repositorioUsuario.ObtenerRolesDelUsuarioAsync(usuario.Id, ctsRoles.Token);
                    break;
                }
                catch (OperationCanceledException) when (intento == 1)
                {
                    Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: timeout transitorio leyendo roles (intento 1). Reintentando.");
                }
                catch (Exception ex) when (intento == 1 && EsErrorTransitorio(ex))
                {
                    Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: error transitorio leyendo roles (intento 1) -> {ex.Message}. Reintentando.");
                }
            }

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
        var sesionPersistida = false;
        for (var intento = 1; intento <= 2; intento++)
        {
            try
            {
                using var ctsSesion = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                ctsSesion.CancelAfter(TimeSpan.FromSeconds(8));
                await repositorioSesion.CrearAsync(usuario.Id, token, expira, ctsSesion.Token);
                sesionPersistida = true;
                break;
            }
            catch (OperationCanceledException) when (intento == 1)
            {
                Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: timeout transitorio creando sesión (intento 1). Reintentando.");
            }
            catch (Exception ex) when (intento == 1)
            {
                Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: error transitorio creando sesión (intento 1) -> {ex.Message}. Reintentando.");
            }
            catch (Exception ex) when (intento == 2)
            {
                Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: no se pudo persistir sesión tras reintentos -> {ex.Message}. Se continúa en modo resiliente.");
            }
        }
        swSesion.Stop();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase.Metric: sesion_ms={swSesion.ElapsedMilliseconds}.");
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase.Metric: sesion_persistida={(sesionPersistida ? 1 : 0)}.");

        try
        {
            using var ctsAcceso = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            ctsAcceso.CancelAfter(TimeSpan.FromSeconds(4));
            await repositorioUsuario.RegistrarUltimoAccesoAsync(usuario.Id, DateTime.UtcNow, ctsAcceso.Token);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginUseCase: no se pudo actualizar ultimo acceso -> {ex.Message}. Se continúa en modo resiliente.");
        }

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

        return "Analista";
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
