using SistemaAranceles.Application.DTOs.DemandaIngresos;

namespace SistemaAranceles.Application.Services.Aranceles;

/// <summary>
/// Resuelve el descuento comercial aplicable a un ciclo y el arancel cobrado tras el descuento (KAN-44).
/// El arancel base no cambia; solo el valor cobrado por ciclo.
/// </summary>
public static class DescuentoArancelHelper
{
    public static decimal ResolverPorcentajeDescuentoCiclo(
        IReadOnlyList<DescuentoArancelCicloDto> descuentos,
        int numeroCiclo)
    {
        foreach (var d in descuentos)
        {
            if (d.EstaActivo && numeroCiclo >= d.CicloDesde && numeroCiclo <= d.CicloHasta)
                return d.PorcentajeDescuento;
        }
        return 0m;
    }

    public static decimal CalcularArancelCiclo(decimal arancelBase, decimal porcentajeDescuento)
    {
        var pct = Math.Clamp(porcentajeDescuento, 0m, 100m);
        return decimal.Round(arancelBase * (1m - pct / 100m), 2);
    }
}
