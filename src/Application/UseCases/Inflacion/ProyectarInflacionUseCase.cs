using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Inflacion;

public sealed class ProyectarInflacionUseCase(
    IRepositorioInflacionAnual repositorioInflacionAnual,
    IServicioProyeccion servicioProyeccion,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<ProyeccionInflacionResultadoDto> EjecutarAsync(
        ProyeccionInflacionSolicitudDto solicitud,
        int? ejecutadoPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        int? usuarioId = ejecutadoPorUsuarioId.HasValue && ejecutadoPorUsuarioId.Value > 0 ? ejecutadoPorUsuarioId.Value : null;
        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionProyeccion: inicio. desde={solicitud.AnioDesde}, hasta={solicitud.AnioHasta}, usuario={usuarioId}");

        if (solicitud.AnioDesde < 2000 || solicitud.AnioHasta > 2100 || solicitud.AnioDesde > solicitud.AnioHasta)
            throw new ArgumentException("Rango de proyección inválido.");

        var todos = await repositorioInflacionAnual.ListarAsync(cancellationToken);
        var historicos = todos
            .Where(x => !x.TipoFuente.Equals("Estimacion", StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Anio)
            .Select(x => (x.Anio, x.PorcentajeInflacion))
            .ToList();

        if (historicos.Count < 3)
            throw new InvalidOperationException("Se requieren al menos 3 datos históricos para proyectar con el método conservador.");

        var proyecciones = servicioProyeccion.ProyectarPromedioSuave(historicos, solicitud.AnioDesde, solicitud.AnioHasta);

        var creados = 0;
        var actualizados = 0;
        var omitidos = 0;

        foreach (var (anio, porcentaje) in proyecciones)
        {
            var existente = await repositorioInflacionAnual.ObtenerPorAnioAsync(anio, cancellationToken);

            if (existente is null)
            {
                await repositorioInflacionAnual.AgregarAsync(
                    new InflacionAnual(anio, porcentaje, "Proyeccion lineal", "Estimacion"),
                    usuarioId,
                    cancellationToken);
                creados++;
                continue;
            }

            if (existente.TipoFuente.Equals("Ajuste manual", StringComparison.OrdinalIgnoreCase)
                || existente.TipoFuente.Equals("Dato historico", StringComparison.OrdinalIgnoreCase))
            {
                omitidos++;
                continue;
            }

            existente.CambiarPorcentajeInflacion(porcentaje);
            existente.CambiarFuente("Proyeccion lineal", "Estimacion");
            await repositorioInflacionAnual.ActualizarAsync(existente, usuarioId, cancellationToken);
            actualizados++;
        }

        try
        {
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var detalle = ex.InnerException?.Message ?? ex.Message;
            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionDBError: proyeccion guardado falló. detalle={detalle}");
            throw new InvalidOperationException($"Error al guardar proyección de inflación. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Inflacion",
                entidadNombre: "ProyeccionInflacion",
                entidadId: $"{solicitud.AnioDesde}-{solicitud.AnioHasta}",
                accionNombre: "PROYECCION_GENERADA",
                resumenTexto: $"Proyección conservadora generada. Creados={creados}, actualizados={actualizados}, omitidos={omitidos}.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // No bloquear flujo por auditoría.
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionProyeccion: fin. creados={creados}, actualizados={actualizados}, omitidos={omitidos}");

        return new ProyeccionInflacionResultadoDto
        {
            RegistrosCreados = creados,
            RegistrosActualizados = actualizados,
            RegistrosOmitidos = omitidos
        };
    }
}
