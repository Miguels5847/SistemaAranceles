using FluentValidation;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class CrearCriterioReferenciaRetencionUseCase(
    IRepositorioCriterioReferenciaRetencion repositorioCriterio,
    IRepositorioConfiguracionRetencion repositorioConfiguracion,
    IValidator<GuardarCriterioReferenciaRetencionDto> validador,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<int> EjecutarAsync(
        GuardarCriterioReferenciaRetencionDto dto,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await validador.ValidateAndThrowAsync(dto, cancellationToken);

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] CritRefCrear: configuracion={dto.ConfiguracionRetencionId}, usuario={usuarioId}");

        var configuracion = await repositorioConfiguracion.ObtenerDominioPorIdAsync(dto.ConfiguracionRetencionId, cancellationToken)
            ?? throw new InvalidOperationException($"No existe la configuración de retención con Id {dto.ConfiguracionRetencionId}.");

        var yaExiste = await repositorioCriterio.ExistePorConfiguracionAsync(dto.ConfiguracionRetencionId, cancellationToken);
        if (yaExiste)
            throw new InvalidOperationException("La configuración ya tiene un criterio de referencia. Use actualizar.");

        var entidad = new CriterioReferenciaRetencion(
            dto.ConfiguracionRetencionId,
            dto.MetaRetencionPorcentaje,
            dto.MetaGraduacionPorcentaje);

        try
        {
            await repositorioCriterio.AgregarAsync(entidad, usuarioId, cancellationToken);
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var detalle = ex.InnerException?.Message ?? ex.Message;
            Trace.TraceError($"[{DateTime.UtcNow:O}] CritRefCrearError: {detalle}");
            throw new InvalidOperationException($"No se pudo guardar el criterio de referencia. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "TasaRetencion",
                entidadNombre: "CriterioReferenciaRetencion",
                entidadId: entidad.Id.ToString(),
                accionNombre: "CREAR",
                resumenTexto: $"Criterio de referencia creado. ConfigId={configuracion.Id}, MetaRet={dto.MetaRetencionPorcentaje:N2}%, MetaGrad={dto.MetaGraduacionPorcentaje:N2}%.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
        }

        return entidad.Id;
    }

    private static int? NormalizarUsuarioId(int? usuarioId)
        => usuarioId.HasValue && usuarioId.Value > 0 ? usuarioId.Value : null;
}
