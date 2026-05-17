using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioPeriodoAcademico(ContextoAplicacion contexto) : IRepositorioPeriodoAcademico
{
    public async Task<IReadOnlyList<PeriodoAcademicoCatalogDto>> ListarTodosAsync(CancellationToken cancellationToken = default)
    {
        var periodos = await contexto.PeriodosAcademicos
            .OrderBy(x => x.Anio)
            .ThenBy(x => x.NumeroPeriodo)
            .Select(x => new PeriodoAcademicoCatalogDto
            {
                Id = x.Id,
                EtiquetaPeriodo = x.EtiquetaPeriodo,
                Anio = x.Anio,
                NumeroPeriodo = x.NumeroPeriodo,
            })
            .ToListAsync(cancellationToken);

        return periodos;
    }
}
