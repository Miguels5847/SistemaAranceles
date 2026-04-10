using SistemaAranceles.Application.DTOs.Auditoria;

namespace SistemaAranceles.Application.Interfaces.Servicios;

public interface IAuditoriaServicio
{
    Task RegistrarAsync(
        string moduloNombre,
        string entidadNombre,
        string entidadId,
        string accionNombre,
        string resumenTexto,
        int? ejecutadoPorUsuarioId = null,
        string? valoresAnterioresJson = null,
        string? valoresNuevosJson = null,
        CancellationToken cancellationToken = default);

    Task<AuditoriaConsultaResultadoDto> ConsultarAsync(
        AuditoriaFiltroDto filtros,
        CancellationToken cancellationToken = default);
}
