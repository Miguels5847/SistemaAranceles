using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioActivoFijo
{
    Task<ActivoFijo?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<ActivoFijo>> ListarPorCarreraAsync(int carreraId, CategoriaActivoFijo? categoria = null, CancellationToken ct = default);
    Task AgregarAsync(ActivoFijo activo, CancellationToken ct = default);
    void Actualizar(ActivoFijo activo);
    void EliminarLogico(ActivoFijo activo, int? eliminadoPorUsuarioId);
}
