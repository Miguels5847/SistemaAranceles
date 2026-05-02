using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCargoFacultad(ContextoAplicacion contextoAplicacion) : IRepositorioCargoFacultad
{
    public async Task<Domain.Entities.CargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var e = await contextoAplicacion.CargosFacultad
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<IReadOnlyList<Domain.Entities.CargoFacultad>> ListarPorCarreraAsync(int carreraId, CancellationToken ct = default)
    {
        var lista = await contextoAplicacion.CargosFacultad
            .Where(x => x.CarreraId == carreraId)
            .OrderBy(x => x.NombreCargo)
            .ToListAsync(ct);
        return lista.Select(MapearADominio).ToList();
    }

    public async Task AgregarAsync(Domain.Entities.CargoFacultad cargo, CancellationToken ct = default)
        => await contextoAplicacion.CargosFacultad.AddAsync(MapearAInfra(cargo), ct);

    public void Actualizar(Domain.Entities.CargoFacultad cargo)
        => contextoAplicacion.CargosFacultad.Update(MapearAInfra(cargo));

    public void Eliminar(Domain.Entities.CargoFacultad cargo)
        => contextoAplicacion.CargosFacultad.Remove(MapearAInfra(cargo));

    private static Domain.Entities.CargoFacultad MapearADominio(CargoFacultad e)
    {
        var dominio = new Domain.Entities.CargoFacultad(
            e.CarreraId,
            e.NombreCargo,
            e.TipoCargo,
            e.SueldoBaseMensual,
            e.EsCargoDocente);

        dominio.RehidratarId(e.Id);
        return dominio;
    }

    private static CargoFacultad MapearAInfra(Domain.Entities.CargoFacultad d) => new()
    {
        Id = d.Id,
        CarreraId = d.CarreraId,
        NombreCargo = d.NombreCargo,
        TipoCargo = d.TipoCargo,
        SueldoBaseMensual = d.SueldoBaseMensual,
        EsCargoDocente = d.EsCargoDocente,
    };
}
