using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

public sealed class ListarProyeccionesEstudiantesUseCase(IRepositorioProyeccionEstudiantes repositorio)
{
    public async Task<IReadOnlyList<ResumenProyeccionEstudiantesDto>> EjecutarAsync(
        int? carreraId = null,
        int? escenarioProyeccionId = null,
        CancellationToken cancellationToken = default)
    {
        return await repositorio.ListarResumenAsync(carreraId, escenarioProyeccionId, cancellationToken);
    }
}
