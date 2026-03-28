using System.Linq.Expressions;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioGenerico<TEntidad>
    where TEntidad : class
{
    Task<IReadOnlyList<TEntidad>> ListarAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntidad>> BuscarAsync(
        Expression<Func<TEntidad, bool>> predicado,
        CancellationToken cancellationToken = default);

    Task<TEntidad?> ObtenerPorLlaveAsync(
        object[] claves,
        CancellationToken cancellationToken = default);

    Task AgregarAsync(TEntidad entidad, CancellationToken cancellationToken = default);

    void Actualizar(TEntidad entidad);

    void Eliminar(TEntidad entidad);
}
