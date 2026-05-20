using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Application.UseCases.Mantenimiento;

public sealed class ActualizarServicioMantenimientoCommand(
    IRepositorioServicioMantenimiento repositorio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(ActualizarServicioMantenimientoDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var servicio = await repositorio.ObtenerPorIdAsync(dto.Id, ct)
            ?? throw new DominioException($"Servicio/Mantenimiento con Id={dto.Id} no encontrado.");
        servicio.CambiarTipoRubro(dto.TipoRubro);
        servicio.CambiarNombreRubro(dto.NombreRubro);
        servicio.CambiarCosto(dto.CostoAnualUniversidad);
        repositorio.Actualizar(servicio);
        await unidadTrabajo.GuardarCambiosAsync(ct);
    }
}
