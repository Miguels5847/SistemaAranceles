using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IRepositorioDatosInstitucionales
{
    Task<DatosInstitucionales?> ObtenerPorPeriodoAsync(string periodo, CancellationToken cancellationToken = default);
    Task<DatosInstitucionales?> ObtenerVigenteAsync(CancellationToken cancellationToken = default);
    Task<DatosInstitucionales?> ObtenerAnteriorAsync(string periodoActual, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DatosInstitucionales>> ListarHistoricoAsync(CancellationToken cancellationToken = default);
    void Agregar(DatosInstitucionales datos);
    void Actualizar(DatosInstitucionales datos);
}
