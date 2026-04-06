using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.ValueObjects;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class CrearUsuarioUseCase(
    IRepositorioUsuario repositorioUsuario,
    IRepositorioRol repositorioRol,
    IServicioHash servicioHash,
    IAuditoriaServicio auditoriaServicio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task<int> EjecutarAsync(
        CrearUsuarioDto dto,
        int? creadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            throw new ArgumentException("El nombre completo es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.CorreoInstitucional))
            throw new ArgumentException("El correo institucional es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Contrasena) || dto.Contrasena.Length < 8)
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");

        if (string.IsNullOrWhiteSpace(dto.RolNombre))
            throw new ArgumentException("Debe asignar un rol al usuario.");

        var existeCorreo = await repositorioUsuario.ExisteCorreoInstitucionalAsync(
            dto.CorreoInstitucional, cancellationToken);
        if (existeCorreo)
            throw new InvalidOperationException($"Ya existe un usuario con el correo '{dto.CorreoInstitucional}'.");

        var rolEncontrado = await repositorioRol.ObtenerPorNombreAsync(dto.RolNombre, cancellationToken);
        if (!rolEncontrado.HasValue)
            throw new InvalidOperationException($"El rol '{dto.RolNombre}' no existe.");

        var hash = servicioHash.Hashear(dto.Contrasena);
        var correo = new CorreoInstitucional(dto.CorreoInstitucional);
        var usuario = new Usuario(dto.NombreCompleto, correo, hash);

        await repositorioUsuario.AgregarAsync(usuario, cancellationToken);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        var usuarioPersistido = await repositorioUsuario.ObtenerPorCorreoInstitucionalAsync(
            correo.ToString(),
            cancellationToken);

        if (usuarioPersistido is null)
            throw new InvalidOperationException("No se pudo recuperar el usuario recién creado.");

        await repositorioRol.AsignarRolAUsuarioAsync(usuarioPersistido.Id, rolEncontrado.Value.Id, cancellationToken);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        await auditoriaServicio.RegistrarAsync(
            moduloNombre: "Usuarios",
            entidadNombre: "Usuario",
            entidadId: usuarioPersistido.Id.ToString(),
            accionNombre: "CREAR",
            resumenTexto: $"Usuario '{dto.NombreCompleto}' creado con rol '{rolEncontrado.Value.Nombre}'.",
            ejecutadoPorUsuarioId: creadoPorUsuarioId,
            cancellationToken: cancellationToken);

        return usuarioPersistido.Id;
    }
}
