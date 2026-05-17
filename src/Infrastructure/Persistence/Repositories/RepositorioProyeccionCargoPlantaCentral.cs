using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioProyeccionCargoPlantaCentral(ContextoAplicacion contextoAplicacion) : IRepositorioProyeccionCargoPlantaCentral
{
    public async Task<IReadOnlyList<Domain.Entities.ProyeccionCargoPlantaCentral>> ObtenerPorCarreraYPeriodoAsync(
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        var lista = await contextoAplicacion.ProyeccionesCargoPlantaCentral
            .Where(x => x.CarreraId == carreraId && x.PeriodoAcademicoId == periodoAcademicoId)
            .OrderBy(x => x.CargoPlantaCentralId)
            .ToListAsync(cancellationToken);
        return lista.Select(MapearADominio).ToList();
    }

    public async Task<Domain.Entities.ProyeccionCargoPlantaCentral?> ObtenerPorIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var e = await contextoAplicacion.ProyeccionesCargoPlantaCentral
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<Domain.Entities.ProyeccionCargoPlantaCentral?> ObtenerPorCargoYCarreraYPeriodoAsync(
        int cargoPlantaCentralId,
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        var e = await contextoAplicacion.ProyeccionesCargoPlantaCentral
            .FirstOrDefaultAsync(x => x.CargoPlantaCentralId == cargoPlantaCentralId &&
                                      x.CarreraId == carreraId &&
                                      x.PeriodoAcademicoId == periodoAcademicoId,
                cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public void Agregar(Domain.Entities.ProyeccionCargoPlantaCentral proyeccion)
        => contextoAplicacion.ProyeccionesCargoPlantaCentral.Add(MapearAInfra(proyeccion));

    public void Actualizar(Domain.Entities.ProyeccionCargoPlantaCentral proyeccion)
        => contextoAplicacion.ProyeccionesCargoPlantaCentral.Update(MapearAInfra(proyeccion));

    public void Eliminar(Domain.Entities.ProyeccionCargoPlantaCentral proyeccion)
        => contextoAplicacion.ProyeccionesCargoPlantaCentral.Remove(MapearAInfra(proyeccion));

    private static Domain.Entities.ProyeccionCargoPlantaCentral MapearADominio(ProyeccionCargoPlantaCentral e)
    {
        var dominio = new Domain.Entities.ProyeccionCargoPlantaCentral(
            e.CargoPlantaCentralId,
            e.CarreraId,
            e.PeriodoAcademicoId,
            e.ProporcionAsignacion,
            e.CostoTotalSemestre);

        dominio.RehidratarId(e.Id);
        return dominio;
    }

    private static ProyeccionCargoPlantaCentral MapearAInfra(Domain.Entities.ProyeccionCargoPlantaCentral d) => new()
    {
        Id = d.Id,
        CargoPlantaCentralId = d.CargoPlantaCentralId,
        CarreraId = d.CarreraId,
        PeriodoAcademicoId = d.PeriodoAcademicoId,
        ProporcionAsignacion = d.ProporcionAsignacion,
        CostoTotalSemestre = d.CostoTotalSemestre,
    };
}
