using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Enums;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class EliminarUsuarioUseCase(
    IRepositorioUsuario repositorioUsuario,
    IRepositorioSesionUsuario repositorioSesionUsuario,
    IAuditoriaServicio auditoriaServicio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(
        int id,
        int eliminadoPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var swTotal = Stopwatch.StartNew();
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] EliminarUsuarioUseCase: inicio para usuarioId={id}.");

        var usuario = await repositorioUsuario.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el usuario con Id {id}.");

        if (id == eliminadoPorUsuarioId)
            throw new InvalidOperationException("Un usuario no puede eliminarse a sí mismo.");

        var eliminacionDefinitiva = usuario.Estado == EstadoUsuario.Inactivo;
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] EliminarUsuarioUseCase: preparando {(eliminacionDefinitiva ? "eliminación definitiva" : "baja lógica")} de usuario '{usuario.NombreCompleto}'.");

        await repositorioSesionUsuario.RevocarTodasDelUsuarioAsync(id, cancellationToken);

        if (eliminacionDefinitiva)
        {
            await repositorioUsuario.EliminarDefinitivamenteAsync(id, cancellationToken);
        }
        else
        {
            await repositorioUsuario.EliminarAsync(id, eliminadoPorUsuarioId, cancellationToken);
        }

        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        Trace.TraceInformation($"[{DateTime.UtcNow:O}] EliminarUsuarioUseCase: usuario '{usuario.NombreCompleto}' {(eliminacionDefinitiva ? "eliminado definitivamente" : "desactivado")}.");

        await auditoriaServicio.RegistrarAsync(
            moduloNombre: "Usuarios",
            entidadNombre: "Usuario",
            entidadId: id.ToString(),
            accionNombre: "ELIMINAR",
            resumenTexto: eliminacionDefinitiva
                ? $"Usuario '{usuario.NombreCompleto}' eliminado de forma permanente."
                : $"Usuario '{usuario.NombreCompleto}' desactivado.",
            ejecutadoPorUsuarioId: eliminadoPorUsuarioId,
            cancellationToken: cancellationToken);

        swTotal.Stop();
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] EliminarUsuarioUseCase: fin exitoso para usuarioId={id}. total_ms={swTotal.ElapsedMilliseconds}.");
    }
}
