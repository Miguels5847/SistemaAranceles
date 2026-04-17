using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Inflacion;

public sealed class LimpiarInflacionUseCase(
    IRepositorioInflacionAnual repositorioInflacionAnual,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<LimpiarInflacionResultadoDto> EjecutarAsync(
        int? ejecutadoPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        int? usuarioId = ejecutadoPorUsuarioId.HasValue && ejecutadoPorUsuarioId.Value > 0 ? ejecutadoPorUsuarioId.Value : null;
        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionLimpieza: inicio. usuario={usuarioId}");

        await unidadTrabajo.IniciarTransaccionAsync(cancellationToken);

        try
        {
            var (anuales, proyectadas) = await repositorioInflacionAnual.LimpiarTodoAsync(cancellationToken);
            await unidadTrabajo.ConfirmarTransaccionAsync(cancellationToken);

            try
            {
                await auditoriaServicio.RegistrarAsync(
                    moduloNombre: "Inflacion",
                    entidadNombre: "Inflacion",
                    entidadId: "ALL",
                    accionNombre: "LIMPIAR",
                    resumenTexto: $"Limpieza total de inflación completada. Anuales={anuales}, proyectadas={proyectadas}.",
                    ejecutadoPorUsuarioId: usuarioId,
                    cancellationToken: cancellationToken);
            }
            catch
            {
            }

            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionLimpieza: fin. anuales={anuales}, proyectadas={proyectadas}");
            return new LimpiarInflacionResultadoDto
            {
                RegistrosAnualesEliminados = anuales,
                RegistrosProyectadosEliminados = proyectadas
            };
        }
        catch
        {
            await unidadTrabajo.RevertirTransaccionAsync();
            throw;
        }
    }
}