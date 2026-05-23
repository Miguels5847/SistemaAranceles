using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.CapitalTrabajo;

public sealed class GuardarItemCapitalTrabajoCommand(IRepositorioItemMaterialInsumo repositorio)
{
    public Task EjecutarAsync(GuardarItemCapitalTrabajoDto dto, CancellationToken ct = default)
    {
        if (dto.CarreraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(dto.CarreraId), "Seleccione una carrera.");

        if (string.IsNullOrWhiteSpace(dto.CategoriaNombre))
            throw new ArgumentException("Seleccione una categoria.", nameof(dto.CategoriaNombre));

        if (string.IsNullOrWhiteSpace(dto.Concepto))
            throw new ArgumentException("Ingrese el concepto.", nameof(dto.Concepto));

        if (dto.Cantidad < 0m)
            throw new ArgumentOutOfRangeException(nameof(dto.Cantidad), "La cantidad no puede ser negativa.");

        if (dto.ValorUnitario < 0m)
            throw new ArgumentOutOfRangeException(nameof(dto.ValorUnitario), "El valor unitario no puede ser negativo.");

        return repositorio.GuardarAsync(dto, ct);
    }
}

public sealed class EliminarItemCapitalTrabajoCommand(IRepositorioItemMaterialInsumo repositorio)
{
    public Task EjecutarAsync(int id, CancellationToken ct = default)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id), "Seleccione un item valido.");

        return repositorio.EliminarAsync(id, ct);
    }
}
