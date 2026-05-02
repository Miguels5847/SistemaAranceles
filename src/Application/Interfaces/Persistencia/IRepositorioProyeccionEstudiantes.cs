using SistemaAranceles.Application.DTOs.Estudiantes;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioProyeccionEstudiantes
{
    Task<IReadOnlyList<ResumenProyeccionEstudiantesDto>> ListarResumenAsync(
        int? carreraId = null,
        int? escenarioProyeccionId = null,
        CancellationToken cancellationToken = default);

    Task<ProyeccionEstudiantesDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<int?> ObtenerIdPorCarreraYEscenarioAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken cancellationToken = default);

    Task<int> GuardarAsync(
        int carreraId,
        int escenarioProyeccionId,
        int anioBase,
        int semanasPorSemestre,
        IReadOnlyList<CeldaProyeccionEstudiantesDto> celdas,
        int? usuarioId,
        CancellationToken cancellationToken = default);

    Task EliminarPorIdAsync(int id, int? usuarioId, CancellationToken cancellationToken = default);
}
