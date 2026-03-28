using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioUsuario
{
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorCorreoInstitucionalAsync(string correoInstitucional, CancellationToken cancellationToken = default);

    Task<bool> ExisteCorreoInstitucionalAsync(string correoInstitucional, CancellationToken cancellationToken = default);

    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
}
