using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

public sealed class GuardarRatioMaterialDemandaCommand(
    IRepositorioRatioMaterialDemanda repositorio,
    IAuditoriaServicio auditoria)
{
    public async Task<int> EjecutarAsync(
        GuardarRatioMaterialDemandaDto dto,
        int? usuarioId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (string.IsNullOrWhiteSpace(dto.Categoria))
            throw new ArgumentException("Categoría es obligatoria.", nameof(dto.Categoria));
        if (string.IsNullOrWhiteSpace(dto.Concepto))
            throw new ArgumentException("Concepto es obligatorio.", nameof(dto.Concepto));
        if (dto.RatioConsumo < 0m)
            throw new ArgumentException("Ratio consumo no puede ser negativo.", nameof(dto.RatioConsumo));
        var mesesOperativos = dto.UnidadRatio == UnidadRatioMaterialExtensiones.FijoPeriodoText
            ? 1
            : dto.MesesOperativos;

        if (mesesOperativos <= 0 || mesesOperativos > 12)
            throw new ArgumentException("Meses operativos entre 1 y 12.", nameof(dto.MesesOperativos));
        if (dto.UnidadRatio is not (UnidadRatioMaterialExtensiones.PorEstudianteText
            or UnidadRatioMaterialExtensiones.PorEstudianteMesText
            or UnidadRatioMaterialExtensiones.FijoPeriodoText))
            throw new ArgumentException("Unidad inválida.", nameof(dto.UnidadRatio));

        var dtoNormalizado = mesesOperativos == dto.MesesOperativos
            ? dto
            : new GuardarRatioMaterialDemandaDto
            {
                Id = dto.Id,
                CarreraId = dto.CarreraId,
                Categoria = dto.Categoria,
                Concepto = dto.Concepto,
                ItemMaterialInsumoId = dto.ItemMaterialInsumoId,
                RatioConsumo = dto.RatioConsumo,
                UnidadRatio = dto.UnidadRatio,
                MesesOperativos = mesesOperativos,
                AplicaInflacion = dto.AplicaInflacion
            };

        var id = await repositorio.GuardarAsync(dtoNormalizado, usuarioId, ct);

        try
        {
            await auditoria.RegistrarAsync(
                moduloNombre: "DemandaIngresos",
                entidadNombre: "RatioMaterialDemanda",
                entidadId: id.ToString(),
                accionNombre: dto.Id is null or 0 ? "CREAR" : "ACTUALIZAR",
                resumenTexto: $"Categoria={dto.Categoria}, Concepto={dto.Concepto}, Carrera={dto.CarreraId?.ToString() ?? "GLOBAL"}",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: ct);
        }
        catch
        {
        }

        return id;
    }
}
