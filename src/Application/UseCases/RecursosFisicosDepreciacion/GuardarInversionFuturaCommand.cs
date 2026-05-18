using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public sealed class GuardarInversionFuturaCommand(
    IRepositorioActivoFijo repositorioActivoFijo,
    IRepositorioInversionFutura repositorioInversionFutura,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(GuardarInversionFuturaDto dto, CancellationToken cancellationToken = default)
    {
        var activo = await repositorioActivoFijo.ObtenerPorIdAsync(dto.ActivoFijoId, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el activo fijo con Id {dto.ActivoFijoId}.");

        if (activo.TipoCalculoCantidad is TipoCalculoCantidad.PorEstudiante or TipoCalculoCantidad.PorDocente)
        {
            throw new InvalidOperationException("Las inversiones futuras derivadas por estudiantes o docentes se calculan automáticamente y no se guardan manualmente.");
        }

        if (dto.Semestre is not 1 and not 2)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.Semestre), "El semestre debe ser 1 o 2.");
        }

        if (dto.CantidadProyectada < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.CantidadProyectada), "La cantidad proyectada no puede ser negativa.");
        }

        var existente = await repositorioInversionFutura.ObtenerPorActivoPeriodoAsync(
            dto.ActivoFijoId,
            dto.Anio,
            dto.Semestre,
            cancellationToken);

        if (existente is null)
        {
            var inversion = new InversionFutura(
                dto.ActivoFijoId,
                dto.Anio,
                dto.Semestre,
                dto.CantidadProyectada);
            await repositorioInversionFutura.AgregarAsync(inversion, cancellationToken);
        }
        else
        {
            existente.CambiarCantidad(dto.CantidadProyectada);
            repositorioInversionFutura.Actualizar(existente);
        }

        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}
