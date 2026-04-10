using SistemaAranceles.Application.DTOs.Permisos;
using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Domain.ValueObjects;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class ActualizarUsuarioUseCase(
    IRepositorioUsuario repositorioUsuario,
    IRepositorioRol repositorioRol,
    IServicioHash servicioHash,
    IAuditoriaServicio auditoriaServicio,
    IUnidadTrabajo unidadTrabajo,
    IRepositorioPermiso repositorioPermiso)
{
    public async Task EjecutarAsync(
        ActualizarUsuarioDto dto,
        int? actualizadoPorUsuarioId = null,
        CancellationToken cancellationToken = default,
        IEnumerable<PermisoOverrideDto>? overrides = null)
    {
        var usuario = await repositorioUsuario.ObtenerPorIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el usuario con Id {dto.Id}.");

        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            throw new ArgumentException("El nombre completo es obligatorio.");

        if (!string.IsNullOrWhiteSpace(dto.CorreoInstitucional) &&
            !dto.CorreoInstitucional.Equals(usuario.CorreoInstitucional.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            var existe = await repositorioUsuario.ExisteCorreoInstitucionalAsync(dto.CorreoInstitucional, cancellationToken);
            if (existe)
                throw new InvalidOperationException($"Ya existe un usuario con el correo '{dto.CorreoInstitucional}'.");

            usuario.CambiarCorreo(new CorreoInstitucional(dto.CorreoInstitucional));
        }

        usuario.CambiarNombre(dto.NombreCompleto);

        if (!string.IsNullOrWhiteSpace(dto.NuevaContrasena))
        {
            if (dto.NuevaContrasena.Length < 8)
                throw new ArgumentException("La nueva contraseña debe tener al menos 8 caracteres.");
            usuario.CambiarHashContrasena(servicioHash.Hashear(dto.NuevaContrasena));
        }

        if (Enum.TryParse<EstadoUsuario>(dto.Estado, true, out var nuevoEstado))
        {
            switch (nuevoEstado)
            {
                case EstadoUsuario.Activo:     usuario.Reactivar(); break;
                case EstadoUsuario.Suspendido: usuario.Suspender(); break;
                case EstadoUsuario.Inactivo:   usuario.Inactivar(); break;
            }
        }

        // Iniciar transacción única para usuario + rol + overrides
        await unidadTrabajo.IniciarTransaccionAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(dto.RolNombre))
            {
                var rolNuevo = await repositorioRol.ObtenerPorNombreAsync(dto.RolNombre, cancellationToken);
                if (!rolNuevo.HasValue)
                    throw new InvalidOperationException($"El rol '{dto.RolNombre}' no existe.");

                var rolesActuales = await repositorioRol.ObtenerNombresDeRolesDelUsuarioAsync(dto.Id, cancellationToken);
                foreach (var nombreRol in rolesActuales)
                {
                    var rolActual = await repositorioRol.ObtenerPorNombreAsync(nombreRol, cancellationToken);
                    if (rolActual.HasValue)
                        await repositorioRol.QuitarRolDeUsuarioAsync(dto.Id, rolActual.Value.Id, cancellationToken);
                }

                await repositorioRol.AsignarRolAUsuarioAsync(dto.Id, rolNuevo.Value.Id, cancellationToken);
            }

            await repositorioUsuario.ActualizarAsync(usuario, cancellationToken);
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

            // Overrides dentro de la misma transacción (GuardarOverridesAsync detecta CurrentTransaction)
            if (overrides is not null)
            {
                await repositorioPermiso.GuardarOverridesAsync(
                    dto.Id, overrides, actualizadoPorUsuarioId ?? 0, cancellationToken);
            }

            await unidadTrabajo.ConfirmarTransaccionAsync(cancellationToken);
            Trace.WriteLine($"[{DateTime.UtcNow:O}] ActualizarUsuarioUseCase: commit exitoso para usuarioId={dto.Id}.");
        }
        catch (Exception ex)
        {
            await unidadTrabajo.RevertirTransaccionAsync();
            Trace.WriteLine($"[{DateTime.UtcNow:O}] ActualizarUsuarioUseCase: rollback ejecutado para usuarioId={dto.Id} -> {ex.Message}.");
            throw;
        }

        // Auditoría fuera de transacción (fallo no revierte el guardado)
        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Usuarios",
                entidadNombre: "Usuario",
                entidadId: dto.Id.ToString(),
                accionNombre: "ACTUALIZAR",
                resumenTexto: $"Usuario Id {dto.Id} actualizado.",
                ejecutadoPorUsuarioId: actualizadoPorUsuarioId,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] ActualizarUsuarioUseCase: auditoría falló (no crítico) -> {ex.Message}.");
        }
    }
}
