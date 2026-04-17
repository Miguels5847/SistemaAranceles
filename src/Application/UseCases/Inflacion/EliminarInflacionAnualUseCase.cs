using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Inflacion;

public sealed class EliminarInflacionAnualUseCase(
    IRepositorioInflacionAnual repositorioInflacionAnual,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task EjecutarAsync(int id, int? ejecutadoPorUsuarioId, CancellationToken cancellationToken = default)
    {
        await repositorioInflacionAnual.EliminarPorIdAsync(id, cancellationToken);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Inflacion",
                entidadNombre: "InflacionAnual",
                entidadId: id.ToString(),
                accionNombre: "ELIMINAR",
                resumenTexto: $"Registro de inflación eliminado. Id={id}.",
                ejecutadoPorUsuarioId: ejecutadoPorUsuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionManualDelete: auditoría omitida para id={id}.");
        }
    }
}