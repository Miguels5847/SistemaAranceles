using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioEscenarioProyeccion
{
    Task<IReadOnlyList<EscenarioProyeccion>> ListarAsync(CancellationToken cancellationToken = default);

    Task<EscenarioProyeccion?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistePorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Nombres de los escenarios activos de una carrera (para sembrar solo los faltantes).</summary>
    Task<IReadOnlyList<string>> ListarNombresPorCarreraAsync(int carreraId, CancellationToken cancellationToken = default);

    Task AgregarAsync(EscenarioProyeccion escenario, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default);
}
