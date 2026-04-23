using FluentValidation;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class ActualizarSimulacionRetencionUseCase(
    IRepositorioConfiguracionRetencion repositorioConfiguracion,
    IRepositorioSimulacionRetencion repositorioSimulacion,
    IRepositorioDetalleSimulacionRetencion repositorioDetalle,
    IValidator<ActualizarSimulacionRetencionDto> validador,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task EjecutarAsync(
        ActualizarSimulacionRetencionDto dto,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await validador.ValidateAndThrowAsync(dto, cancellationToken);

        var simulacion = await repositorioSimulacion.ObtenerDominioPorIdAsync(dto.SimulacionRetencionId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la simulación {dto.SimulacionRetencionId}.");

        var conflicto = await repositorioSimulacion.ObtenerDominioPorConfiguracionYCohorteAsync(simulacion.ConfiguracionRetencionId, dto.CohorteAnio, cancellationToken);
        if (conflicto is not null && conflicto.Id != simulacion.Id)
            throw new InvalidOperationException("Ya existe una simulación activa para esa configuración y cohorte.");

        var configuracion = await repositorioConfiguracion.ObtenerDominioPorIdAsync(simulacion.ConfiguracionRetencionId, cancellationToken)
            ?? throw new InvalidOperationException($"La configuración {simulacion.ConfiguracionRetencionId} no existe o está inactiva.");

        var resultado = MotorSimulacionRetencion.Ejecutar(configuracion, dto.CohorteAnio);

        simulacion.ActualizarCohorte(dto.CohorteAnio);
        simulacion.EstablecerFechaSimulacion(DateTime.UtcNow);
        simulacion.ActualizarIndicadoresFinales(
            estudiantesInicio: configuracion.EstudiantesPeriodo1,
            estudiantesRetenidos: resultado.TotalRetenidos,
            estudiantesGraduados: resultado.TotalGraduados,
            costoMatriculaPromedio: 0m);

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);

        await unidadTrabajo.IniciarTransaccionAsync(cancellationToken);
        try
        {
            await repositorioSimulacion.ActualizarAsync(simulacion, usuarioId, cancellationToken);
            await repositorioDetalle.ReemplazarPorSimulacionAsync(simulacion.Id, resultado.Detalles, usuarioId, cancellationToken);
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
                entidadId: simulacion.Id.ToString(),
                accionNombre: "ACTUALIZAR_SIMULACION",
                resumenTexto: $"Simulación actualizada. Id={simulacion.Id}, Cohorte={dto.CohorteAnio}.",
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
