using SistemaAranceles.Application.DTOs.CargosFacultad;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioPeriodoAcademico
{
    Task<IReadOnlyList<PeriodoAcademicoCatalogDto>> ListarTodosAsync(CancellationToken cancellationToken = default);
}
