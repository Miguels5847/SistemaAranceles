using FluentValidation;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class CrearSimulacionRetencionUseCase(
    IRepositorioConfiguracionRetencion repositorioConfiguracion,
    IRepositorioSimulacionRetencion repositorioSimulacion,
    IRepositorioDetalleSimulacionRetencion repositorioDetalle,
    IValidator<CrearSimulacionRetencionDto> validador,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<int> EjecutarAsync(
        CrearSimulacionRetencionDto dto,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await validador.ValidateAndThrowAsync(dto, cancellationToken);

        var configuracion = await repositorioConfiguracion.ObtenerDominioPorIdAsync(dto.ConfiguracionRetencionId, cancellationToken)
            ?? throw new InvalidOperationException($"La configuración {dto.ConfiguracionRetencionId} no existe o está inactiva.");

        if (configuracion.EstudiantesPeriodo1 <= 0m)
            throw new InvalidOperationException("La configuración no puede simularse porque EstudiantesPeriodo1 es 0.");

        var existente = await repositorioSimulacion.ObtenerDominioPorConfiguracionYCohorteAsync(dto.ConfiguracionRetencionId, dto.CohorteAnio, cancellationToken);

        var resultado = MotorSimulacionRetencion.Ejecutar(configuracion, dto.CohorteAnio);

        var simulacion = existente ?? new SimulacionRetencion(dto.ConfiguracionRetencionId, dto.CohorteAnio);
        simulacion.ActualizarCohorte(dto.CohorteAnio);
        simulacion.EstablecerFechaSimulacion(DateTime.UtcNow);
        simulacion.ActualizarIndicadoresFinales(
            estudiantesInicio: configuracion.EstudiantesPeriodo1,
            estudiantesRetenidos: resultado.TotalRetenidos,
            estudiantesGraduados: resultado.TotalGraduados,
            costoMatriculaPromedio: 0m);

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);

        await unidadTrabajo.IniciarTransaccionAsync(cancellationToken);
        var simulacionId = 0;
        try
        {
            if (existente is null)
            {
                await repositorioSimulacion.AgregarAsync(simulacion, usuarioId, cancellationToken);
                await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

                var creada = await repositorioSimulacion.ObtenerDominioPorConfiguracionYCohorteAsync(
                    dto.ConfiguracionRetencionId,
                    dto.CohorteAnio,
                    cancellationToken);

                simulacionId = creada?.Id ?? 0;
                if (simulacionId <= 0)
                    throw new InvalidOperationException("No se pudo obtener el identificador de la simulacion creada.");
            }
            else
            {
                simulacionId = existente.Id;
                await repositorioSimulacion.ActualizarAsync(simulacion, usuarioId, cancellationToken);
            }

            await repositorioDetalle.ReemplazarPorSimulacionAsync(simulacionId, resultado.Detalles, usuarioId, cancellationToken);
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
                accionNombre: existente is null ? "CREAR_SIMULACION" : "REEJECUTAR_SIMULACION",
                resumenTexto: existente is null
                    ? $"Simulación creada. Config={dto.ConfiguracionRetencionId}, Cohorte={dto.CohorteAnio}."
                    : $"Simulación re-ejecutada. Config={dto.ConfiguracionRetencionId}, Cohorte={dto.CohorteAnio}.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // Auditoria no bloquea la operacion principal.
        }

        return simulacionId;
    }

    private static int? NormalizarUsuarioId(int? usuarioId)
        => usuarioId.HasValue && usuarioId.Value > 0 ? usuarioId.Value : null;
}
