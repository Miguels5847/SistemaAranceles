namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioSesionUsuario
{
    Task<int> CrearAsync(
        int usuarioId,
        string tokenSesion,
        DateTime expiraEn,
        CancellationToken cancellationToken = default);

    Task RevocarAsync(
        string tokenSesion,
        CancellationToken cancellationToken = default);

    Task RevocarTodasDelUsuarioAsync(
        int usuarioId,
        CancellationToken cancellationToken = default);
}
