using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioOverrideHorasPeriodo
{
    Task<IReadOnlyList<OverrideHorasPeriodo>> ListarPorProyeccionAsync(int proyeccionId, CancellationToken ct = default);
    Task<OverrideHorasPeriodo?> ObtenerPorProyeccionYPeriodoAsync(int proyeccionId, int periodo, CancellationToken ct = default);
    Task AgregarAsync(OverrideHorasPeriodo entidad, CancellationToken ct = default);
    void Actualizar(OverrideHorasPeriodo entidad);
    void Eliminar(OverrideHorasPeriodo entidad);
}
