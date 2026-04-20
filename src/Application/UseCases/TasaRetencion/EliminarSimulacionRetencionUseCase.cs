using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class EliminarSimulacionRetencionUseCase(
    IRepositorioSimulacionRetencion repositorioSimulacion,
    IRepositorioDetalleSimulacionRetencion repositorioDetalle,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task EjecutarAsync(int simulacionId, int? ejecutadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        if (simulacionId <= 0)
            throw new ArgumentException("El Id de simulación debe ser mayor a cero.", nameof(simulacionId));

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);

        await unidadTrabajo.IniciarTransaccionAsync(cancellationToken);
        try
        {
            await repositorioDetalle.EliminarPorSimulacionAsync(simulacionId, cancellationToken);
            await repositorioSimulacion.EliminarPorIdAsync(simulacionId, usuarioId, cancellationToken);
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
                moduloNombre: "TasaRetencion",
                entidadNombre: "SimulacionRetencion",
                entidadId: simulacionId.ToString(),
                accionNombre: "ELIMINAR_SIMULACION",
                resumenTexto: $"Simulación eliminada. Id={simulacionId}.",
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
