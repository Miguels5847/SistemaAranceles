using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class EliminarConfiguracionRetencionUseCase(
    IRepositorioConfiguracionRetencion repositorioConfiguracion,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task EjecutarAsync(int id, int? ejecutadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("El Id de la configuración debe ser mayor a cero.", nameof(id));

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetEliminar: id={id}, usuario={usuarioId}");

        try
        {
            await repositorioConfiguracion.EliminarPorIdAsync(id, usuarioId, cancellationToken);
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var detalle = ex.InnerException?.Message ?? ex.Message;
            Trace.TraceError($"[{DateTime.UtcNow:O}] ConfRetEliminarError: {detalle}");
            throw new InvalidOperationException($"No se pudo eliminar la configuración. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "TasaRetencion",
                entidadNombre: "ConfiguracionRetencion",
                entidadId: id.ToString(),
                accionNombre: "ELIMINAR",
                resumenTexto: $"Configuración de retención eliminada (soft delete). Id={id}.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
        }
    }

    private static int? NormalizarUsuarioId(int? usuarioId)
        => usuarioId.HasValue && usuarioId.Value > 0 ? usuarioId.Value : null;
}
