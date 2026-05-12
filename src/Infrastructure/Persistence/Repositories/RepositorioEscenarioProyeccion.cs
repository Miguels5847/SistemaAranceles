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

    public async Task<IReadOnlyList<EscenarioDominio>> ListarPorCarreraAsync(int carreraId, CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
            throw new ArgumentException($"{nameof(carreraId)} debe ser mayor que 0.", nameof(carreraId));

        var lista = await contextoAplicacion.EscenariosProyeccion
            .AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EstaActivo)
            .OrderByDescending(x => x.EsPredeterminado)
            .ThenBy(x => x.Nombre)
            .ToListAsync(cancellationToken);

        return lista.Select(MapearADominio).ToList();
    }

    public async Task AgregarAsync(EscenarioDominio escenario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(escenario);

        var entidad = new EscenarioPersistencia
        {
            CarreraId = escenario.CarreraId,
            Nombre = escenario.Nombre,
            Descripcion = escenario.Descripcion,
            EsPredeterminado = false,
            EstaActivo = true
        };

        contextoAplicacion.EscenariosProyeccion.Add(entidad);
        await Task.CompletedTask;
    }

    private static EscenarioDominio MapearADominio(EscenarioPersistencia entidad)
    {
        var dominio = new EscenarioDominio(entidad.CarreraId, entidad.Nombre, entidad.Descripcion, entidad.EsPredeterminado);
        dominio.RehidratarId(entidad.Id);
        return dominio;
    }
}
