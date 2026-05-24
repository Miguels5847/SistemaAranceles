using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Application.UseCases.Mantenimiento;

public sealed class EliminarServicioMantenimientoCommand(
    IRepositorioServicioMantenimiento repositorio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(int id, int? usuarioId = null, CancellationToken ct = default)
    {
        var servicio = await repositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new DominioException($"Servicio/Mantenimiento con Id={id} no encontrado.");
        repositorio.EliminarLogico(servicio, usuarioId);
        await unidadTrabajo.GuardarCambiosAsync(ct);
    }
}
