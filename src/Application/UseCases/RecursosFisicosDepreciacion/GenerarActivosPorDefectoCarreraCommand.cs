using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Constantes;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

/// <summary>
/// Asegura el catálogo global de activos por defecto y luego siembra esos activos en la carrera.
/// Idempotente: completa solo los faltantes. Devuelve cuántos activos se crearon en la carrera.
/// </summary>
public sealed class GenerarActivosPorDefectoCarreraCommand(
    IRepositorioCatalogoActivoBase repositorioCatalogo,
    IUnidadTrabajo unidadTrabajo,
    SembrarActivosFijosDesdeCatalogoCommand sembrarCarrera)
{
    public async Task<int> EjecutarAsync(int carreraId, CancellationToken ct = default)
    {
        if (carreraId <= 0)
            throw new DominioException("Carrera es obligatoria para generar activos.");

        var agregadosCatalogo = await repositorioCatalogo.SembrarPorDefectoAsync(CatalogoActivosBasePorDefecto.Items, ct);
        if (agregadosCatalogo > 0)
            await unidadTrabajo.GuardarCambiosAsync(ct);

        return await sembrarCarrera.EjecutarAsync(carreraId, ct);
    }
}
