using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.Mantenimiento;

public sealed class CrearServicioMantenimientoCommand(
    IRepositorioServicioMantenimiento repositorio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(CrearServicioMantenimientoDto dto, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var servicio = new ServicioMantenimiento(dto.CarreraId, dto.TipoRubro, dto.NombreRubro, dto.CostoAnualUniversidad);
        await repositorio.AgregarAsync(servicio, ct);
        await unidadTrabajo.GuardarCambiosAsync(ct);
    }
}
