using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Enums;
using SistemaAranceles.Domain.ValueObjects;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class ActualizarUsuarioUseCase(
    IRepositorioUsuario repositorioUsuario,
    IRepositorioRol repositorioRol,
    IServicioHash servicioHash,
    IAuditoriaServicio auditoriaServicio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(
        ActualizarUsuarioDto dto,
        int? actualizadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
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
                case EstadoUsuario.Activo:    usuario.Reactivar(); break;
                case EstadoUsuario.Suspendido: usuario.Suspender(); break;
                case EstadoUsuario.Inactivo:  usuario.Inactivar(); break;
            }
        }

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

        await auditoriaServicio.RegistrarAsync(
            moduloNombre: "Usuarios",
            entidadNombre: "Usuario",
            entidadId: dto.Id.ToString(),
            accionNombre: "ACTUALIZAR",
            resumenTexto: $"Usuario Id {dto.Id} actualizado.",
            ejecutadoPorUsuarioId: actualizadoPorUsuarioId,
            cancellationToken: cancellationToken);
    }
}
