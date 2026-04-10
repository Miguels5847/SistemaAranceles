using SistemaAranceles.Application.DTOs.Auditoria;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Infrastructure.Servicios;

public sealed class ServicioAuditoria(IRepositorioAuditoriaLog repositorioAuditoriaLog) : IAuditoriaServicio
{
    public Task RegistrarAsync(
        string moduloNombre,
        string entidadNombre,
        string entidadId,
        string accionNombre,
        string resumenTexto,
        int? ejecutadoPorUsuarioId = null,
        string? valoresAnterioresJson = null,
        string? valoresNuevosJson = null,
        CancellationToken cancellationToken = default)
    {
        return repositorioAuditoriaLog.AgregarAsync(
            moduloNombre, entidadNombre, entidadId,
            accionNombre, resumenTexto,
            ejecutadoPorUsuarioId,
            valoresAnterioresJson, valoresNuevosJson,
            cancellationToken);
    }

    public Task<AuditoriaConsultaResultadoDto> ConsultarAsync(
        AuditoriaFiltroDto filtros,
        CancellationToken cancellationToken = default)
    {
        return repositorioAuditoriaLog.ConsultarAsync(filtros, cancellationToken);
    }
}
