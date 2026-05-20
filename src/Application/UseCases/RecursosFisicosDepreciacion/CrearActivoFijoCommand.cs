using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public sealed class CrearActivoFijoCommand(
    IRepositorioActivoFijo repositorioActivoFijo,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(CrearActivoFijoDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var activo = new ActivoFijo(
            dto.CarreraId,
            dto.Descripcion,
            dto.Categoria,
            dto.Cantidad,
            dto.UnidadMedida,
            dto.ValorUnitario,
            dto.VidaUtilAnios,
            dto.PorcentajeResidual,
            dto.FechaAdquisicion,
            dto.TipoCalculoCantidad,
            dto.FactorMultiplicador,
            dto.OffsetCantidad,
            dto.CategoriaPersonalizada);

        await repositorioActivoFijo.AgregarAsync(activo, cancellationToken);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}
