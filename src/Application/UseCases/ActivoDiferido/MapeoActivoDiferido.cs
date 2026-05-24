using SistemaAranceles.Application.DTOs.ActivoDiferido;
using DominioActivoDiferido = SistemaAranceles.Domain.Entities.ActivoDiferido;

namespace SistemaAranceles.Application.UseCases.ActivoDiferido;

internal static class MapeoActivoDiferido
{
    public static ActivoDiferidoDto ADto(DominioActivoDiferido a) => new()
    {
        Id = a.Id,
        CarreraId = a.CarreraId,
        NombreRubro = a.NombreRubro,
        Valor = a.Valor,
        TasaAmortizacionAnual = a.TasaAmortizacionAnual,
        CuotaAnual = a.CuotaAnual(),
    };
}
