using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Application.UseCases.ActivoDiferido;

public sealed class EliminarActivoDiferidoCommand(IRepositorioActivoDiferido repositorio, IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(int id, int? usuarioId = null, CancellationToken ct = default)
    {
        var activo = await repositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new DominioException($"Activo diferido con Id={id} no encontrado.");
        repositorio.EliminarLogico(activo, usuarioId);
        await unidadTrabajo.GuardarCambiosAsync(ct);
    }
}
