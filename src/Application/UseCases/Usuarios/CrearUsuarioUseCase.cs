using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.ValueObjects;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class CrearUsuarioUseCase(
    IRepositorioUsuario repositorioUsuario,
    IRepositorioRol repositorioRol,
    IServicioHash servicioHash,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<int> EjecutarAsync(
        CrearUsuarioDto dto,
        int? creadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        var swTotal = Stopwatch.StartNew();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] CrearUsuarioUseCase: inicio para correo '{dto.CorreoInstitucional}'.");

        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            throw new ArgumentException("El nombre completo es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.CorreoInstitucional))
            throw new ArgumentException("El correo institucional es obligatorio.");

        if (string.IsNullOrWhiteSpace(dto.Contrasena) || dto.Contrasena.Length < 8)
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");

        if (string.IsNullOrWhiteSpace(dto.RolNombre))
            throw new ArgumentException("Debe asignar un rol al usuario.");

        var swExiste = Stopwatch.StartNew();
        var existeCorreo = await repositorioUsuario.ExisteCorreoInstitucionalAsync(
            dto.CorreoInstitucional, cancellationToken);
        Trace.WriteLine($"[{DateTime.UtcNow:O}] CrearUsuarioUseCase.Metric: existe_correo_ms={swExiste.ElapsedMilliseconds}.");
        if (existeCorreo)
            throw new InvalidOperationException($"Ya existe un usuario con el correo '{dto.CorreoInstitucional}'.");

        (int Id, string Nombre)? rolEncontrado;
        if (dto.RolId > 0)
        {
            rolEncontrado = (dto.RolId, dto.RolNombre);
        }
        else
        {
            rolEncontrado = await repositorioRol.ObtenerPorNombreAsync(dto.RolNombre, cancellationToken);
            if (!rolEncontrado.HasValue)
                throw new InvalidOperationException($"El rol '{dto.RolNombre}' no existe.");
        }

        var hash = servicioHash.Hashear(dto.Contrasena);
        var correo = new CorreoInstitucional(dto.CorreoInstitucional);
        var usuario = new Usuario(dto.NombreCompleto, correo, hash);

        var swInsert = Stopwatch.StartNew();
        var usuarioId = await repositorioUsuario.AgregarConRolAsync(
            usuario,
            rolEncontrado.Value.Id,
            cancellationToken);
        swInsert.Stop();
        Trace.WriteLine($"[{DateTime.UtcNow:O}] CrearUsuarioUseCase.Metric: insert_usuario_rol_ms={swInsert.ElapsedMilliseconds}.");

        var swAudit = Stopwatch.StartNew();
        try
        {
            using var ctsAuditoria = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Usuarios",
                entidadNombre: "Usuario",
                entidadId: usuarioId.ToString(),
                accionNombre: "CREAR",
                resumenTexto: $"Usuario '{dto.NombreCompleto}' creado con rol '{rolEncontrado.Value.Nombre}'.",
                ejecutadoPorUsuarioId: creadoPorUsuarioId,
                cancellationToken: ctsAuditoria.Token);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] CrearUsuarioUseCase: auditoría omitida por error/transitorio -> {ex.Message}.");
        }
        Trace.WriteLine($"[{DateTime.UtcNow:O}] CrearUsuarioUseCase.Metric: auditoria_ms={swAudit.ElapsedMilliseconds}.");

        Trace.WriteLine($"[{DateTime.UtcNow:O}] CrearUsuarioUseCase: fin exitoso. usuario_id={usuarioId}. total_ms={swTotal.ElapsedMilliseconds}.");

        return usuarioId;
    }
}
