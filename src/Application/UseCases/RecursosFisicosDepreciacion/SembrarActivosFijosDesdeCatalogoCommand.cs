using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

/// <summary>
/// Siembra los activos fijos de una carrera a partir del catalogo institucional global
/// (KAN-24). Idempotente: completa solo los faltantes y no duplica.
/// </summary>
public sealed class SembrarActivosFijosDesdeCatalogoCommand(
    IRepositorioCatalogoActivoBase repositorioCatalogo,
    IRepositorioActivoFijo repositorioActivo,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task<int> EjecutarAsync(int carreraId, CancellationToken ct = default)
    {
        if (carreraId <= 0)
        {
            throw new DominioException("Carrera es obligatoria para sembrar activos.");
        }

        var plantillas = await repositorioCatalogo.ListarActivosAsync(ct);
        if (plantillas.Count == 0)
        {
            return 0;
        }

        var descripcionesRegistradas = await repositorioActivo.ListarDescripcionesRegistradasPorCarreraAsync(carreraId, ct);
        var conocidas = descripcionesRegistradas
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizarDescripcion)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var faltantes = plantillas
            .Where(x => !conocidas.Contains(NormalizarDescripcion(x.Descripcion)))
            .ToList();

        if (faltantes.Count == 0)
        {
            return 0;
        }

        foreach (var plantilla in faltantes)
        {
            await repositorioActivo.AgregarAsync(plantilla.CrearActivoParaCarrera(carreraId), ct);
        }

        await unidadTrabajo.GuardarCambiosAsync(ct);
        return faltantes.Count;
    }

    private static string NormalizarDescripcion(string descripcion) => descripcion.Trim();
}
