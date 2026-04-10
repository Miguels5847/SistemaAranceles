namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioRol
{
    Task<IReadOnlyList<(int Id, string Nombre, string Descripcion)>> ListarAsync(
        CancellationToken cancellationToken = default);

    Task<(int Id, string Nombre)?> ObtenerPorNombreAsync(
        string nombre,
        CancellationToken cancellationToken = default);

    Task AsignarRolAUsuarioAsync(
        int usuarioId,
        int rolId,
        CancellationToken cancellationToken = default);

    Task<int?> AsignarRolAUsuarioPorCorreoAsync(
        string correoInstitucional,
        int rolId,
        CancellationToken cancellationToken = default);

    Task QuitarRolDeUsuarioAsync(
        int usuarioId,
        int rolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ObtenerNombresDeRolesDelUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);
}
