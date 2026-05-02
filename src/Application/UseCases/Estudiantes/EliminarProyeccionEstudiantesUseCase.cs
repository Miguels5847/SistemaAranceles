using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

public sealed class EliminarProyeccionEstudiantesUseCase(
    IRepositorioProyeccionEstudiantes repositorio,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task EjecutarAsync(
        int id,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser mayor a cero.");

        var usuarioId = ejecutadoPorUsuarioId.HasValue && ejecutadoPorUsuarioId.Value > 0
            ? ejecutadoPorUsuarioId.Value
            : (int?)null;

        await unidadTrabajo.IniciarTransaccionAsync(cancellationToken);
        try
        {
            await repositorio.EliminarPorIdAsync(id, usuarioId, cancellationToken);
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
            await unidadTrabajo.ConfirmarTransaccionAsync(cancellationToken);
        }
        catch
        {
            await unidadTrabajo.RevertirTransaccionAsync();
            throw;
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Estudiantes",
                entidadNombre: "ProyeccionEstudiantes",
                entidadId: id.ToString(),
                accionNombre: "ELIMINAR_PROYECCION",
                resumenTexto: $"Proyección de estudiantes eliminada. Id={id}.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Auditoría no bloquea la operación principal.
        }
    }
}
