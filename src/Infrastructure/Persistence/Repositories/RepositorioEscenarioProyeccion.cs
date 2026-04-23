using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using EscenarioDominio = SistemaAranceles.Domain.Entities.EscenarioProyeccion;
using EscenarioPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.EscenarioProyeccion;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioEscenarioProyeccion(ContextoAplicacion contextoAplicacion) : IRepositorioEscenarioProyeccion
{
    public async Task<IReadOnlyList<EscenarioDominio>> ListarAsync(CancellationToken cancellationToken = default)
    {
        var lista = await contextoAplicacion.EscenariosProyeccion
            .AsNoTracking()
            .Where(x => x.EstaActivo)
            .OrderByDescending(x => x.EsPredeterminado)
            .ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        return lista.Select(MapearADominio).ToList();
    }

    public async Task<EscenarioDominio?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entidad = await contextoAplicacion.EscenariosProyeccion
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, cancellationToken);
        return entidad is null ? null : MapearADominio(entidad);
    }

    public async Task<bool> ExistePorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.EscenariosProyeccion.AnyAsync(x => x.Id == id && x.EstaActivo, cancellationToken);
    }

    private static EscenarioDominio MapearADominio(EscenarioPersistencia entidad)
    {
        var dominio = new EscenarioDominio(entidad.CarreraId, entidad.Nombre, entidad.Descripcion, entidad.EsPredeterminado);
        dominio.RehidratarId(entidad.Id);
        return dominio;
    }
}
