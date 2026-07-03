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

    public async Task<IReadOnlyList<Domain.Entities.CargoFacultad>> ListarPlantillaCompartidaAsync(int carreraExcluidaId, CancellationToken ct = default)
    {
        // Carrera con más cargos = plantilla más completa; empate → menor id (determinista).
        // A propósito NO se filtra por carrera activa: los cargos de una carrera borrada
        // lógicamente siguen siendo una plantilla válida.
        var plantillaCarreraId = await contextoAplicacion.CargosFacultad
            .Where(x => x.CarreraId != carreraExcluidaId)
            .GroupBy(x => x.CarreraId)
            .OrderByDescending(g => g.Count())
            .ThenBy(g => g.Key)
            .Select(g => (int?)g.Key)
            .FirstOrDefaultAsync(ct);

        return plantillaCarreraId is null
            ? []
            : await ListarPorCarreraAsync(plantillaCarreraId.Value, ct);
    }

    public async Task AgregarAsync(Domain.Entities.CargoFacultad cargo, CancellationToken ct = default)
        => await contextoAplicacion.CargosFacultad.AddAsync(MapearAInfra(cargo), ct);

    public void Actualizar(Domain.Entities.CargoFacultad cargo)
    {
        var tracked = contextoAplicacion.CargosFacultad.Local.FirstOrDefault(x => x.Id == cargo.Id);
        if (tracked is not null)
        {
            tracked.CarreraId = cargo.CarreraId;
            tracked.NombreCargo = cargo.NombreCargo;
            tracked.SueldoBaseMensual = cargo.SueldoBaseMensual;
            tracked.EsCargoDocente = cargo.EsCargoDocente;
            tracked.CantidadDefault = cargo.CantidadDefault;
            tracked.TipoContrato = cargo.TipoContrato;
            tracked.TarifaHora = cargo.TarifaHora;
        }
        else
        {
            contextoAplicacion.CargosFacultad.Update(MapearAInfra(cargo));
        }
    }

    public void Eliminar(Domain.Entities.CargoFacultad cargo)
    {
        var tracked = contextoAplicacion.CargosFacultad.Local.FirstOrDefault(x => x.Id == cargo.Id);
        if (tracked is not null)
        {
            contextoAplicacion.CargosFacultad.Remove(tracked);
            return;
        }

        var proxy = MapearAInfra(cargo);
        contextoAplicacion.Entry(proxy).State = EntityState.Deleted;
    }

    private static Domain.Entities.CargoFacultad MapearADominio(CargoFacultad e)
    {
        var dominio = new Domain.Entities.CargoFacultad(
            e.CarreraId,
            e.NombreCargo,
            "No especificado",
            e.SueldoBaseMensual,
            e.EsCargoDocente,
            e.CantidadDefault);

        dominio.CambiarTipoContrato(e.TipoContrato);
        dominio.CambiarTarifaHora(e.TarifaHora);
        dominio.RehidratarId(e.Id);
        return dominio;
    }

    private static CargoFacultad MapearAInfra(Domain.Entities.CargoFacultad d) => new()
    {
        Id = d.Id,
        CarreraId = d.CarreraId,
        NombreCargo = d.NombreCargo,
        SueldoBaseMensual = d.SueldoBaseMensual,
        EsCargoDocente = d.EsCargoDocente,
        CantidadDefault = d.CantidadDefault,
        TipoContrato = d.TipoContrato,
        TarifaHora = d.TarifaHora,
    };
}
