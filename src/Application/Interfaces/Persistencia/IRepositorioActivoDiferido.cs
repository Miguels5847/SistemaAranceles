using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioActivoDiferido
{
    Task<ActivoDiferido?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ActivoDiferido>> ListarPorCarreraAsync(int carreraId, CancellationToken ct = default);
    Task<decimal> SumarValorPorCarreraAsync(int carreraId, CancellationToken ct = default);
    Task AgregarAsync(ActivoDiferido activo, CancellationToken ct = default);
    void Actualizar(ActivoDiferido activo);
    void EliminarLogico(ActivoDiferido activo, int? eliminadoPorUsuarioId);
}
