using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCargoFacultad(ContextoAplicacion contextoAplicacion) : IRepositorioCargoFacultad
{
    public async Task<CargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await contextoAplicacion.CargosFacultad
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<CargoFacultad>> ListarPorCarreraAsync(int carreraId, CancellationToken ct = default)
        => await contextoAplicacion.CargosFacultad
            .Where(x => x.CarreraId == carreraId)
            .OrderBy(x => x.NombreCargo)
            .ToListAsync(ct);

    public async Task AgregarAsync(CargoFacultad cargo, CancellationToken ct = default)
        => await contextoAplicacion.CargosFacultad.AddAsync(cargo, ct);

    public void Actualizar(CargoFacultad cargo)
        => contextoAplicacion.CargosFacultad.Update(cargo);

    public void Eliminar(CargoFacultad cargo)
        => contextoAplicacion.CargosFacultad.Remove(cargo);
}
