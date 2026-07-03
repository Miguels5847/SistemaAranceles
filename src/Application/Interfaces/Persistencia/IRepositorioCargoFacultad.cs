using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioCargoFacultad
{
    Task<CargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<CargoFacultad>> ListarPorCarreraAsync(int carreraId, CancellationToken ct = default);

    /// <summary>
    /// Plantilla compartida: cargos de la carrera que MÁS cargos tiene (excluyendo la indicada),
    /// sin exigir que esa carrera siga activa. Eliminar una carrera no debe dejar sin plantilla
    /// a las carreras que aún no registran cargos propios.
    /// </summary>
    Task<IReadOnlyList<CargoFacultad>> ListarPlantillaCompartidaAsync(int carreraExcluidaId, CancellationToken ct = default);
    Task AgregarAsync(CargoFacultad cargo, CancellationToken ct = default);
    void Actualizar(CargoFacultad cargo);
    void Eliminar(CargoFacultad cargo);
}
