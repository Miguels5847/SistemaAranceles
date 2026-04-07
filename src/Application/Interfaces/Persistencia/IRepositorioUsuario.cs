using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioUsuario
{
    Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Usuario?> ObtenerPorCorreoInstitucionalAsync(string correoInstitucional, CancellationToken cancellationToken = default);

    Task<bool> ExisteCorreoInstitucionalAsync(string correoInstitucional, CancellationToken cancellationToken = default);

    Task AgregarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task<int> AgregarConRolAsync(
        Usuario usuario,
        int rolId,
        CancellationToken cancellationToken = default);

    Task ActualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    Task EliminarAsync(int id, int eliminadoPorUsuarioId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ObtenerRolesDelUsuarioAsync(int id, CancellationToken cancellationToken = default);
}
