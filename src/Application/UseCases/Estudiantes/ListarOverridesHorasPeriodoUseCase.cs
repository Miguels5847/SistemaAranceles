using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

public sealed class ListarOverridesHorasPeriodoUseCase(
    IRepositorioOverrideHorasPeriodo repositorio)
{
    public Task<IReadOnlyList<OverrideHorasPeriodo>> EjecutarAsync(
        int proyeccionId, CancellationToken ct = default)
        => repositorio.ListarPorProyeccionAsync(proyeccionId, ct);
}
