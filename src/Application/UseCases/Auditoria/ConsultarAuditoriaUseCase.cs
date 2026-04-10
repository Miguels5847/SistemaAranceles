using SistemaAranceles.Application.DTOs.Auditoria;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Application.UseCases.Auditoria;

public sealed class ConsultarAuditoriaUseCase(IAuditoriaServicio auditoriaServicio)
{
    public Task<AuditoriaConsultaResultadoDto> EjecutarAsync(
        AuditoriaFiltroDto filtros,
        CancellationToken cancellationToken = default)
    {
        var numeroPagina = filtros.NumeroPagina <= 0 ? 1 : filtros.NumeroPagina;
        var tamanoPagina = filtros.TamanoPagina <= 0 ? 25 : Math.Min(filtros.TamanoPagina, 100);

        var normalizado = new AuditoriaFiltroDto
        {
            UsuarioId = filtros.UsuarioId,
            FechaDesdeUtc = filtros.FechaDesdeUtc,
            FechaHastaUtc = filtros.FechaHastaUtc,
            ModuloNombre = string.IsNullOrWhiteSpace(filtros.ModuloNombre) ? null : filtros.ModuloNombre.Trim(),
            AccionNombre = string.IsNullOrWhiteSpace(filtros.AccionNombre) ? null : filtros.AccionNombre.Trim(),
            NumeroPagina = numeroPagina,
            TamanoPagina = tamanoPagina
        };

        return auditoriaServicio.ConsultarAsync(normalizado, cancellationToken);
    }
}
