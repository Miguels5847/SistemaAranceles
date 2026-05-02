using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioProyeccionCargoFacultad(ContextoAplicacion contextoAplicacion) : IRepositorioProyeccionCargoFacultad
{
    public async Task<ProyeccionCargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await contextoAplicacion.ProyeccionesCargoFacultad
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<ProyeccionCargoFacultad>> ListarPorCargoAsync(int cargoFacultadId, CancellationToken ct = default)
        => await contextoAplicacion.ProyeccionesCargoFacultad
            .Where(x => x.CargoFacultadId == cargoFacultadId)
            .OrderBy(x => x.PeriodoAcademicoId)
            .ToListAsync(ct);

    public async Task AgregarAsync(ProyeccionCargoFacultad proyeccion, CancellationToken ct = default)
        => await contextoAplicacion.ProyeccionesCargoFacultad.AddAsync(proyeccion, ct);

    public void Actualizar(ProyeccionCargoFacultad proyeccion)
        => contextoAplicacion.ProyeccionesCargoFacultad.Update(proyeccion);
}
