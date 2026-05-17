using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Carga el catálogo de períodos académicos para usar en dropdowns.
/// </summary>
public sealed class ListarPeriodosAcademicosQuery(IRepositorioPeriodoAcademico repositorio)
{
    public async Task<IReadOnlyList<PeriodoAcademicoCatalogDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        return await repositorio.ListarTodosAsync(cancellationToken);
    }
}
