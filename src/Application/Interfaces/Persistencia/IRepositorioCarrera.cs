using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioCarrera
{
    Task<IReadOnlyList<Carrera>> ListarAsync(CancellationToken cancellationToken = default);

    Task<Carrera?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Carrera?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancellationToken = default);

    Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default);

    Task AgregarAsync(Carrera carrera, CancellationToken cancellationToken = default);

    Task ActualizarAsync(Carrera carrera, CancellationToken cancellationToken = default);
}
