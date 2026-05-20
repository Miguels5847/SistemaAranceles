using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.Mantenimiento;

public sealed class ListarServiciosMantenimientoQuery(IRepositorioServicioMantenimiento repositorio)
{
    public async Task<IReadOnlyList<ServicioMantenimientoDto>> EjecutarAsync(
        int carreraId,
        TipoRubroMantenimiento? tipo = null,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default)
    {
        var lista = await repositorio.ListarPorCarreraAsync(carreraId, tipo, escenarioProyeccionId, ct);
        return lista.Select(MapeoServicioMantenimiento.ADto).ToList();
    }
}
