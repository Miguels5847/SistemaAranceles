using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public sealed class ActualizarActivoFijoCommand(
    IRepositorioActivoFijo repositorioActivoFijo,
    IUnidadTrabajo unidadTrabajo)
{
    public async Task EjecutarAsync(ActualizarActivoFijoDto dto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.Id <= 0)
            throw new ArgumentOutOfRangeException(nameof(dto), "El identificador debe ser mayor a cero.");

        var activo = await repositorioActivoFijo.ObtenerPorIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontro el activo fijo con Id {dto.Id}.");

        activo.CambiarDescripcion(dto.Descripcion);
        activo.CambiarCategoria(dto.Categoria, dto.CategoriaPersonalizada);
        activo.CambiarCantidad(dto.Cantidad);
        activo.CambiarUnidadMedida(dto.UnidadMedida);
        activo.CambiarValorUnitario(dto.ValorUnitario);
        activo.CambiarVidaUtil(dto.VidaUtilAnios);
        activo.CambiarPorcentajeResidual(dto.PorcentajeResidual);
        activo.CambiarFechaAdquisicion(dto.FechaAdquisicion);
        activo.CambiarCalculoCantidad(dto.TipoCalculoCantidad, dto.FactorMultiplicador, dto.OffsetCantidad);

        repositorioActivoFijo.Actualizar(activo);
        await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
    }
}
