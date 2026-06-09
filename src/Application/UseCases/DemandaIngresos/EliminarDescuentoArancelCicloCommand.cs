using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>Desactiva (soft-delete) un descuento de arancel por ciclo (KAN-44).</summary>
public sealed class EliminarDescuentoArancelCicloCommand(IRepositorioDescuentoArancelCiclo repositorio)
{
    public Task EjecutarAsync(int id, int? usuarioId, CancellationToken ct = default)
        => repositorio.DesactivarAsync(id, usuarioId, ct);
}
