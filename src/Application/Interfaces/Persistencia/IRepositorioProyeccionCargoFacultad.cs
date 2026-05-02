using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioProyeccionCargoFacultad
{
    Task<ProyeccionCargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ProyeccionCargoFacultad>> ListarPorCargoAsync(int cargoFacultadId, CancellationToken ct = default);
    Task AgregarAsync(ProyeccionCargoFacultad proyeccion, CancellationToken ct = default);
    void Actualizar(ProyeccionCargoFacultad proyeccion);
    void Eliminar(ProyeccionCargoFacultad proyeccion);
}
