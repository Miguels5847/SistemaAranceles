using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Infrastructure.Persistence.Contexto;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

internal sealed class RepositorioProyeccionCargoFacultad : IRepositorioProyeccionCargoFacultad
{
    private readonly ContextoAplicacion _contexto;

    public RepositorioProyeccionCargoFacultad(ContextoAplicacion contexto)
    {
        _contexto = contexto;
    }

    public async Task<ProyeccionCargoFacultad?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
        => await _contexto.ProyeccionesCargoFacultad
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<IReadOnlyList<ProyeccionCargoFacultad>> ListarPorCargoAsync(int cargoFacultadId, CancellationToken ct = default)
        => await _contexto.ProyeccionesCargoFacultad
            .Where(x => x.CargoFacultadId == cargoFacultadId)
            .OrderBy(x => x.PeriodoAcademicoId)
            .ToListAsync(ct);

    public async Task AgregarAsync(ProyeccionCargoFacultad proyeccion, CancellationToken ct = default)
        => await _contexto.ProyeccionesCargoFacultad.AddAsync(proyeccion, ct);

    public void Actualizar(ProyeccionCargoFacultad proyeccion)
        => _contexto.ProyeccionesCargoFacultad.Update(proyeccion);
}
