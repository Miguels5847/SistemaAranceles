using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class LimpiarSimulacionesRetencionUseCase(
    IRepositorioSimulacionRetencion repositorioSimulacion,
    IRepositorioDetalleSimulacionRetencion repositorioDetalle,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<int> EjecutarAsync(int configuracionRetencionId, int? ejecutadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        if (configuracionRetencionId <= 0)
            throw new ArgumentException("La configuración debe ser mayor a cero.", nameof(configuracionRetencionId));

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);

        await unidadTrabajo.IniciarTransaccionAsync(cancellationToken);
        try
        {
            await repositorioDetalle.EliminarPorConfiguracionAsync(configuracionRetencionId, cancellationToken);
            var simulaciones = await repositorioSimulacion.LimpiarPorConfiguracionAsync(configuracionRetencionId, usuarioId, cancellationToken);
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
            await unidadTrabajo.ConfirmarTransaccionAsync(cancellationToken);

            try
            {
                await auditoriaServicio.RegistrarAsync(
                    moduloNombre: "TasaRetencion",
                    entidadNombre: "SimulacionRetencion",
                    entidadId: configuracionRetencionId.ToString(),
                    accionNombre: "LIMPIAR_SIMULACIONES",
                    resumenTexto: $"Simulaciones limpiadas para configuración {configuracionRetencionId}. Total={simulaciones}.",
                    ejecutadoPorUsuarioId: usuarioId,
                    cancellationToken: cancellationToken);
            }
            catch
            {
            }

            return simulaciones;
        }
        catch
        {
            await unidadTrabajo.RevertirTransaccionAsync();
            throw;
        }
    }

    private static int? NormalizarUsuarioId(int? usuarioId)
        => usuarioId.HasValue && usuarioId.Value > 0 ? usuarioId.Value : null;
}
