using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Infrastructure.Persistence;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

internal sealed class RepositorioCargoFacultad : IRepositorioCargoFacultad
{
    private readonly ContextoAplicacion _contexto;

    public RepositorioCargoFacultad(ContextoAplicacion contexto)
    {
        _contexto = contexto;
    }

    public async Task<CargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await _contexto.CargosFacultad
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<CargoFacultad>> ListarPorCarreraAsync(int carreraId, CancellationToken ct = default)
        => await _contexto.CargosFacultad
            .Where(x => x.CarreraId == carreraId)
            .OrderBy(x => x.NombreCargo)
            .ToListAsync(ct);

    public async Task AgregarAsync(CargoFacultad cargo, CancellationToken ct = default)
        => await _contexto.CargosFacultad.AddAsync(cargo, ct);

    public void Actualizar(CargoFacultad cargo)
        => _contexto.CargosFacultad.Update(cargo);

    public void Eliminar(CargoFacultad cargo)
        => _contexto.CargosFacultad.Remove(cargo);
}
