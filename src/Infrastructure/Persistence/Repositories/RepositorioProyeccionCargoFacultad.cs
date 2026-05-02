using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioProyeccionCargoFacultad(ContextoAplicacion contextoAplicacion) : IRepositorioProyeccionCargoFacultad
{
    public async Task<Domain.Entities.ProyeccionCargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var e = await contextoAplicacion.ProyeccionesCargoFacultad
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<IReadOnlyList<Domain.Entities.ProyeccionCargoFacultad>> ListarPorCargoAsync(int cargoFacultadId, CancellationToken ct = default)
    {
        var lista = await contextoAplicacion.ProyeccionesCargoFacultad
            .Where(x => x.CargoFacultadId == cargoFacultadId)
            .OrderBy(x => x.PeriodoAcademicoId)
            .ToListAsync(ct);
        return lista.Select(MapearADominio).ToList();
    }

    public async Task AgregarAsync(Domain.Entities.ProyeccionCargoFacultad proyeccion, CancellationToken ct = default)
        => await contextoAplicacion.ProyeccionesCargoFacultad.AddAsync(MapearAInfra(proyeccion), ct);

    public void Actualizar(Domain.Entities.ProyeccionCargoFacultad proyeccion)
        => contextoAplicacion.ProyeccionesCargoFacultad.Update(MapearAInfra(proyeccion));

    public void Eliminar(Domain.Entities.ProyeccionCargoFacultad proyeccion)
        => contextoAplicacion.ProyeccionesCargoFacultad.Remove(MapearAInfra(proyeccion));

    private static Domain.Entities.ProyeccionCargoFacultad MapearADominio(ProyeccionCargoFacultad e)
    {
        var dominio = new Domain.Entities.ProyeccionCargoFacultad(
            e.CargoFacultadId,
            e.PeriodoAcademicoId,
            e.CantidadPersonas,
            e.FactorPonderacion,
            e.FactorInflacion,
            e.CostoTotalSemestre);

        dominio.RehidratarId(e.Id);
        return dominio;
    }

    private static ProyeccionCargoFacultad MapearAInfra(Domain.Entities.ProyeccionCargoFacultad d) => new()
    {
        Id = d.Id,
        CargoFacultadId = d.CargoFacultadId,
        PeriodoAcademicoId = d.PeriodoAcademicoId,
        CantidadPersonas = d.CantidadPersonas,
        FactorPonderacion = d.FactorPonderacion,
        FactorInflacion = d.FactorInflacion,
        CostoTotalSemestre = d.CostoTotalSemestre,
    };
}
