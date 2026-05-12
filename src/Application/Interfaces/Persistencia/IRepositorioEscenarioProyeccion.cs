using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioEscenarioProyeccion
{
    Task<IReadOnlyList<EscenarioProyeccion>> ListarAsync(CancellationToken cancellationToken = default);

    Task<EscenarioProyeccion?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistePorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Retorna escenarios de una carrera específica.</summary>
    Task<IReadOnlyList<EscenarioProyeccion>> ListarPorCarreraAsync(int carreraId, CancellationToken cancellationToken = default);

    /// <summary>Agrega nuevo escenario a la BD (sin guardar cambios aún).</summary>
    Task AgregarAsync(EscenarioProyeccion escenario, CancellationToken cancellationToken = default);
}
