using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

public sealed class ObtenerProyeccionEstudiantesUseCase(IRepositorioProyeccionEstudiantes repositorio)
{
    public async Task<ProyeccionEstudiantesDto?> EjecutarAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser mayor a cero.");

        return await repositorio.ObtenerDtoPorIdAsync(id, cancellationToken);
    }
}
