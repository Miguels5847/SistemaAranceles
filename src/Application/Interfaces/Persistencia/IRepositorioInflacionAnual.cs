using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioInflacionAnual
{
    Task<IReadOnlyList<InflacionAnual>> ListarAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InflacionAnual>> ListarPorRangoAsync(int anioDesde, int anioHasta, CancellationToken cancellationToken = default);

    Task<InflacionAnual?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<InflacionAnual?> ObtenerPorAnioAsync(int anio, CancellationToken cancellationToken = default);

    Task<bool> ExisteAnioAsync(int anio, int? excluirId = null, CancellationToken cancellationToken = default);

    Task AgregarAsync(InflacionAnual inflacionAnual, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default);

    Task ActualizarAsync(InflacionAnual inflacionAnual, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default);

    Task EliminarPorIdAsync(int id, CancellationToken cancellationToken = default);

    Task<(int registrosAnualesEliminados, int registrosProyectadosEliminados)> LimpiarTodoAsync(CancellationToken cancellationToken = default);
}
