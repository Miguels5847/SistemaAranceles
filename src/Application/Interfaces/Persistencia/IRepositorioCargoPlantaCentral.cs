using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioCargoPlantaCentral
{
    Task<IReadOnlyList<CargoPlantaCentral>> ObtenerTodosAsync(CancellationToken cancellationToken = default);
    Task<CargoPlantaCentral?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CargoPlantaCentral?> ObtenerPorNombreAsync(string nombre, CancellationToken cancellationToken = default);
    void Agregar(CargoPlantaCentral cargo);
    void Actualizar(CargoPlantaCentral cargo);
    void Eliminar(CargoPlantaCentral cargo);
}
