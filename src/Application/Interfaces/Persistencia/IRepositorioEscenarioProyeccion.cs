using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioEscenarioProyeccion
{
    Task<IReadOnlyList<EscenarioProyeccion>> ListarAsync(CancellationToken cancellationToken = default);

    Task<EscenarioProyeccion?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistePorIdAsync(int id, CancellationToken cancellationToken = default);
}
