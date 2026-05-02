using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioCargoFacultad
{
    Task<CargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CargoFacultad>> ListarPorCarreraAsync(int carreraId, CancellationToken ct = default);
    Task AgregarAsync(CargoFacultad cargo, CancellationToken ct = default);
    void Actualizar(CargoFacultad cargo);
    void Eliminar(CargoFacultad cargo);
}
