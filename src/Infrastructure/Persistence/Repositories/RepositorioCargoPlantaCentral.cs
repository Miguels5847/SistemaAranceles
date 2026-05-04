using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCargoPlantaCentral(ContextoAplicacion contextoAplicacion) : IRepositorioCargoPlantaCentral
{
    public async Task<IReadOnlyList<Domain.Entities.CargoPlantaCentral>> ObtenerTodosAsync(CancellationToken cancellationToken = default)
    {
        var lista = await contextoAplicacion.CargosPlantaCentral
            .OrderBy(x => x.NombreCargo)
            .ToListAsync(cancellationToken);
        return lista.Select(MapearADominio).ToList();
    }

    public async Task<Domain.Entities.CargoPlantaCentral?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var e = await contextoAplicacion.CargosPlantaCentral
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<Domain.Entities.CargoPlantaCentral?> ObtenerPorNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        var e = await contextoAplicacion.CargosPlantaCentral
            .FirstOrDefaultAsync(x => x.NombreCargo == nombre, cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public void Agregar(Domain.Entities.CargoPlantaCentral cargo)
        => contextoAplicacion.CargosPlantaCentral.Add(MapearAInfra(cargo));

    public void Actualizar(Domain.Entities.CargoPlantaCentral cargo)
        => contextoAplicacion.CargosPlantaCentral.Update(MapearAInfra(cargo));

    public void Eliminar(Domain.Entities.CargoPlantaCentral cargo)
        => contextoAplicacion.CargosPlantaCentral.Remove(MapearAInfra(cargo));

    private static Domain.Entities.CargoPlantaCentral MapearADominio(CargoPlantaCentral e)
    {
        var dominio = new Domain.Entities.CargoPlantaCentral(
            e.NombreCargo,
            e.SueldoMensualTotal);

        dominio.RehidratarId(e.Id);
        return dominio;
    }

    private static CargoPlantaCentral MapearAInfra(Domain.Entities.CargoPlantaCentral d) => new()
    {
        Id = d.Id,
        NombreCargo = d.NombreCargo,
        SueldoMensualTotal = d.SueldoMensualTotal,
    };
}
