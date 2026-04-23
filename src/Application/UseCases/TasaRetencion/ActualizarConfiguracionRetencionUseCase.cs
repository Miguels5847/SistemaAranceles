using FluentValidation;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class ActualizarConfiguracionRetencionUseCase(
    IRepositorioConfiguracionRetencion repositorioConfiguracion,
    IRepositorioSimulacionRetencion repositorioSimulacion,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IValidator<ActualizarConfiguracionRetencionDto> validador,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task EjecutarAsync(
        ActualizarConfiguracionRetencionDto dto,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await validador.ValidateAndThrowAsync(dto, cancellationToken);

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetActualizar: id={dto.Id}, usuario={usuarioId}");

        var entidad = await repositorioConfiguracion.ObtenerDominioPorIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la configuración de retención con Id {dto.Id}.");

        var tieneSimulaciones = await repositorioSimulacion.ExisteActivaPorConfiguracionAsync(dto.Id, cancellationToken);
        if (tieneSimulaciones)
            throw new InvalidOperationException("No se puede editar la configuración porque tiene simulaciones activas asociadas.");

        var carrera = await repositorioCarrera.ObtenerPorIdAsync(dto.CarreraId, cancellationToken)
            ?? throw new InvalidOperationException($"La carrera con Id {dto.CarreraId} no existe.");

        var escenarioExiste = await repositorioEscenario.ExistePorIdAsync(dto.EscenarioProyeccionId, cancellationToken);
        if (!escenarioExiste)
            throw new InvalidOperationException($"El escenario con Id {dto.EscenarioProyeccionId} no existe.");

        var duplicado = await repositorioConfiguracion.ExisteCombinacionAsync(dto.CarreraId, dto.EscenarioProyeccionId, dto.Id, cancellationToken);
        if (duplicado)
            throw new InvalidOperationException($"Ya existe otra configuración para la carrera '{carrera.Nombre}' y ese escenario.");

        // Reconstruir con nuevos valores (las tasas y base se actualizan vía métodos de dominio).
        var nuevaEntidad = new SistemaAranceles.Domain.Entities.ConfiguracionRetencion(
            dto.CarreraId,
            dto.EscenarioProyeccionId,
            dto.TotalCiclos,
            dto.TasaRetencionPorcentaje,
            dto.TasaGraduacionPorcentaje);

        nuevaEntidad.ActualizarBaseEstudiantes(
            dto.EstudiantesPeriodo1,
            dto.EstudiantesPeriodo2,
            dto.ParalelosPeriodo1,
            dto.ParalelosPeriodo2);

        nuevaEntidad.RehidratarId(entidad.Id);

        try
        {
            await repositorioConfiguracion.ActualizarAsync(nuevaEntidad, usuarioId, cancellationToken);
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var detalle = ex.InnerException?.Message ?? ex.Message;
            Trace.TraceError($"[{DateTime.UtcNow:O}] ConfRetActualizarError: {detalle}");
            throw new InvalidOperationException($"No se pudo actualizar la configuración. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "TasaRetencion",
                entidadNombre: "ConfiguracionRetencion",
                entidadId: dto.Id.ToString(),
                accionNombre: "ACTUALIZAR",
                resumenTexto: $"Configuración de retención actualizada. Id={dto.Id}, Carrera={carrera.Codigo}, Escenario={dto.EscenarioProyeccionId}.",
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
