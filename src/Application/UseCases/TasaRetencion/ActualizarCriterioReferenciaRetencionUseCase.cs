using FluentValidation;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class ActualizarCriterioReferenciaRetencionUseCase(
    IRepositorioCriterioReferenciaRetencion repositorioCriterio,
    IValidator<GuardarCriterioReferenciaRetencionDto> validador,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task EjecutarAsync(
        GuardarCriterioReferenciaRetencionDto dto,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await validador.ValidateAndThrowAsync(dto, cancellationToken);

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] CritRefActualizar: configuracion={dto.ConfiguracionRetencionId}, usuario={usuarioId}");

        var entidad = await repositorioCriterio.ObtenerPorConfiguracionAsync(dto.ConfiguracionRetencionId, cancellationToken)
            ?? throw new KeyNotFoundException($"No existe un criterio para la configuración {dto.ConfiguracionRetencionId}. Cree uno primero.");

        entidad.ActualizarMetas(dto.MetaRetencionPorcentaje, dto.MetaGraduacionPorcentaje);

        try
        {
            await repositorioCriterio.ActualizarAsync(entidad, usuarioId, cancellationToken);
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var detalle = ex.InnerException?.Message ?? ex.Message;
            Trace.TraceError($"[{DateTime.UtcNow:O}] CritRefActualizarError: {detalle}");
            throw new InvalidOperationException($"No se pudo actualizar el criterio de referencia. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "TasaRetencion",
                entidadNombre: "CriterioReferenciaRetencion",
                entidadId: entidad.Id.ToString(),
                accionNombre: "ACTUALIZAR",
                resumenTexto: $"Criterio de referencia actualizado. ConfigId={dto.ConfiguracionRetencionId}, MetaRet={dto.MetaRetencionPorcentaje:N2}%, MetaGrad={dto.MetaGraduacionPorcentaje:N2}%.",
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
