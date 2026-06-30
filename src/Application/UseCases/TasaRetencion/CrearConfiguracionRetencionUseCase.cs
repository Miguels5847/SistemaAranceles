using FluentValidation;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

public sealed class CrearConfiguracionRetencionUseCase(
    IRepositorioConfiguracionRetencion repositorioConfiguracion,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IValidator<CrearConfiguracionRetencionDto> validador,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<int> EjecutarAsync(
        CrearConfiguracionRetencionDto dto,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        await validador.ValidateAndThrowAsync(dto, cancellationToken);

        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetCrear: carrera={dto.CarreraId}, escenario={dto.EscenarioProyeccionId}, usuario={usuarioId}");

        var carrera = await repositorioCarrera.ObtenerPorIdAsync(dto.CarreraId, cancellationToken)
            ?? throw new InvalidOperationException($"La carrera con Id {dto.CarreraId} no existe.");

        var escenario = await repositorioEscenario.ObtenerPorIdAsync(dto.EscenarioProyeccionId, cancellationToken)
            ?? throw new InvalidOperationException($"El escenario con Id {dto.EscenarioProyeccionId} no existe.");

        if ((escenario.Nombre == "Optimista" || escenario.Nombre == "Pesimista")
            && await repositorioConfiguracion.ObtenerActivoPorCarreraYEscenarioNombreAsync(dto.CarreraId, "Histórico", cancellationToken) is null)
            throw new InvalidOperationException("Debe crear primero la configuración del escenario Histórico para esta carrera antes de crear un escenario Optimista o Pesimista.");

        var duplicado = await repositorioConfiguracion.ExisteCombinacionAsync(dto.CarreraId, dto.EscenarioProyeccionId, null, cancellationToken);
        if (duplicado)
            throw new InvalidOperationException($"Ya existe una configuración para la carrera '{carrera.Nombre}' y ese escenario.");

        var entidad = new ConfiguracionRetencion(
            dto.CarreraId,
            dto.EscenarioProyeccionId,
            dto.TotalCiclos,
            dto.TasaRetencionPorcentaje,
            dto.TasaGraduacionPorcentaje);

        entidad.ActualizarBaseEstudiantes(
            dto.EstudiantesPeriodo1,
            dto.EstudiantesPeriodo2,
            dto.ParalelosPeriodo1,
            dto.ParalelosPeriodo2);

        // Input principal: las metas acumuladas. Si vienen, derivan las tasas por ciclo (sobrescriben
        // las del constructor); si no, se conserva el modo antiguo (tasa directa) por compatibilidad.
        if (dto.MetaRetencionPorcentaje > 0m || dto.MetaGraduacionPorcentaje > 0m)
            entidad.DefinirMetas(dto.MetaRetencionPorcentaje, dto.MetaGraduacionPorcentaje);

        // No hay duplicado ACTIVO, pero el índice único (carrera, escenario) cubre filas
        // borradas lógicamente. Si existe una fila inactiva para la misma combinación, se
        // reactiva en lugar de insertar; así no se dispara el error 23505.
        var idInactivo = await repositorioConfiguracion.ObtenerIdCualquierEstadoPorCombinacionAsync(
            dto.CarreraId, dto.EscenarioProyeccionId, cancellationToken);

        int configuracionId;
        try
        {
            if (idInactivo is int idRevivir)
            {
                entidad.RehidratarId(idRevivir);
                await repositorioConfiguracion.ActualizarAsync(entidad, usuarioId, cancellationToken);
                await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
                configuracionId = idRevivir;
            }
            else
            {
                await repositorioConfiguracion.AgregarAsync(entidad, usuarioId, cancellationToken);
                await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

                var creada = await repositorioConfiguracion.ObtenerActivoPorCarreraYEscenarioNombreAsync(
                    dto.CarreraId,
                    escenario.Nombre,
                    cancellationToken);

                if (creada is null || creada.Id <= 0)
                    throw new InvalidOperationException("No se pudo recuperar el identificador de la configuración creada.");

                configuracionId = creada.Id;
            }
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var detalle = ex.InnerException?.Message ?? ex.Message;
            Trace.TraceError($"[{DateTime.UtcNow:O}] ConfRetCrearError: {detalle}");
            throw new InvalidOperationException($"No se pudo guardar la configuración de retención. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "TasaRetencion",
                entidadNombre: "ConfiguracionRetencion",
                entidadId: configuracionId.ToString(),
                accionNombre: "CREAR",
                resumenTexto: $"Configuración de retención creada. Carrera={carrera.Codigo}, Escenario={dto.EscenarioProyeccionId}, Ciclos={dto.TotalCiclos}.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // La auditoría es best-effort: un fallo al registrarla no debe revertir el guardado.
        }

        return configuracionId;
    }

    private static int? NormalizarUsuarioId(int? usuarioId)
        => usuarioId.HasValue && usuarioId.Value > 0 ? usuarioId.Value : null;
}
