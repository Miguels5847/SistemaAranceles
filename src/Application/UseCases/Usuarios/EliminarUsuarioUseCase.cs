using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.Usuarios;

public sealed class EliminarUsuarioUseCase(
    IRepositorioUsuario repositorioUsuario,
    IAuditoriaServicio auditoriaServicio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(
        int id,
        int eliminadoPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        var usuario = await repositorioUsuario.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el usuario con Id {id}.");

        if (id == eliminadoPorUsuarioId)
            throw new InvalidOperationException("Un usuario no puede eliminarse a sí mismo.");

        await repositorioUsuario.EliminarAsync(id, eliminadoPorUsuarioId, cancellationToken);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        await auditoriaServicio.RegistrarAsync(
            moduloNombre: "Usuarios",
            entidadNombre: "Usuario",
            entidadId: id.ToString(),
            accionNombre: "ELIMINAR",
            resumenTexto: $"Usuario '{usuario.NombreCompleto}' eliminado (baja lógica).",
            ejecutadoPorUsuarioId: eliminadoPorUsuarioId,
            cancellationToken: cancellationToken);
    }
}
