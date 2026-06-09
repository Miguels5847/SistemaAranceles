using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;

namespace SistemaAranceles.Application.UseCases.Mantenimiento;

/// <summary>
/// Siembra en una carrera los servicios básicos y rubros de mantenimiento por defecto
/// (config general, escenario NULL). Idempotente: completa solo los faltantes. Devuelve cuántos se crearon.
/// </summary>
public sealed class GenerarServiciosMantenimientoPorDefectoCommand(
    IRepositorioServicioMantenimiento repositorio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task<int> EjecutarAsync(int carreraId, int? escenarioProyeccionId = null, CancellationToken ct = default)
    {
        if (carreraId <= 0)
            throw new ArgumentException("Carrera es obligatoria.", nameof(carreraId));

        var creados = await repositorio.SembrarPorDefectoAsync(
            carreraId, escenarioProyeccionId, CatalogoServiciosMantenimientoPorDefecto.Items, ct);

        if (creados > 0)
            await unidadTrabajo.GuardarCambiosAsync(ct);

        return creados;
    }
}
