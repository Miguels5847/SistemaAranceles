using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;
using DominioActivoDiferido = SistemaAranceles.Domain.Entities.ActivoDiferido;

namespace SistemaAranceles.Application.UseCases.ActivoDiferido;

/// <summary>
/// Siembra en una carrera los activos diferidos por defecto (Permiso Municipal, Permiso de
/// Bomberos) con valor 0 para que el usuario cargue el monto real. Idempotente: agrega solo los
/// que falten, así sirve tanto al crear la carrera como para reparar una existente sin diferidos.
/// Devuelve cuántos se crearon.
/// </summary>
public sealed class GenerarActivosDiferidosPorDefectoCarreraCommand(
    IRepositorioActivoDiferido repositorio,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task<int> EjecutarAsync(int carreraId, CancellationToken ct = default)
    {
        if (carreraId <= 0)
            throw new ArgumentException("Carrera es obligatoria.", nameof(carreraId));

        var existentes = await repositorio.ListarPorCarreraAsync(carreraId, ct);
        var nombres = existentes.Select(a => a.NombreRubro).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var creados = 0;
        foreach (var item in CatalogoActivosDiferidosPorDefecto.Items)
        {
            if (nombres.Contains(item.Nombre))
                continue;

            await repositorio.AgregarAsync(new DominioActivoDiferido(carreraId, item.Nombre, item.Valor, item.TasaAnual), ct);
            creados++;
        }

        if (creados > 0)
            await unidadTrabajo.GuardarCambiosAsync(ct);

        return creados;
    }
}
