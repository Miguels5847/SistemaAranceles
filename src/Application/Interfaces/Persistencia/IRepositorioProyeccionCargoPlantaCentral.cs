using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioProyeccionCargoPlantaCentral
{
    Task<IReadOnlyList<ProyeccionCargoPlantaCentral>> ObtenerPorCarreraYPeriodoAsync(
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken = default);

    Task<ProyeccionCargoPlantaCentral?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<ProyeccionCargoPlantaCentral?> ObtenerPorCargoYCarreraYPeriodoAsync(
        int cargoPlantaCentralId,
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken = default);

    void Agregar(ProyeccionCargoPlantaCentral proyeccion);
    void Actualizar(ProyeccionCargoPlantaCentral proyeccion);
    void Eliminar(ProyeccionCargoPlantaCentral proyeccion);
}
