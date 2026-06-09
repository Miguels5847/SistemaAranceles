using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// Crea o actualiza un descuento de arancel por ciclo (KAN-44). Valida rango, porcentaje y
/// que no se solape con otro rango activo de la misma carrera+escenario.
/// </summary>
public sealed class GuardarDescuentoArancelCicloCommand(IRepositorioDescuentoArancelCiclo repositorio)
{
    public async Task<int> EjecutarAsync(GuardarDescuentoArancelCicloDto dto, int? usuarioId, CancellationToken ct = default)
    {
        if (dto.CarreraId <= 0)
            throw new ArgumentException("La carrera es obligatoria.", nameof(dto));
        if (dto.CicloDesde < 1)
            throw new ArgumentException("El ciclo desde debe ser mayor o igual a 1.", nameof(dto));
        if (dto.CicloHasta < dto.CicloDesde)
            throw new ArgumentException("El ciclo hasta debe ser mayor o igual al ciclo desde.", nameof(dto));
        if (dto.PorcentajeDescuento is < 0m or > 100m)
            throw new ArgumentException("El porcentaje de descuento debe estar entre 0 y 100.", nameof(dto));

        var existentes = await repositorio.ListarPorCarreraEscenarioAsync(dto.CarreraId, dto.EscenarioProyeccionId, ct);
        var solapado = existentes.Any(e =>
            e.Id != dto.Id
            && dto.CicloDesde <= e.CicloHasta
            && e.CicloDesde <= dto.CicloHasta);
        if (solapado)
            throw new InvalidOperationException("El rango de ciclos se solapa con otro descuento activo de esta carrera/escenario.");

        return await repositorio.GuardarAsync(dto, usuarioId, ct);
    }
}
