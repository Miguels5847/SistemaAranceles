using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.CapitalTrabajo;

/// <summary>
/// Calcula el capital de trabajo mensual sumando los 4 bloques del Excel "6 Capital de trabajo" (KAN-29).
/// Bloque A: sueldos (CargoFacultad.SueldoBaseMensual × CantidadDefault)
/// Bloques B/C/D: ítems materiales por categoría (CantidadBase × PrecioUnitario)
/// </summary>
public sealed class ObtenerResumenCapitalTrabajoQuery(
    IRepositorioCargoFacultad repositorioCargos,
    IRepositorioItemMaterialInsumo repositorioMateriales)
{
    public async Task<ResumenCapitalTrabajoDto> EjecutarAsync(int carreraId, CancellationToken ct = default)
    {
        var cargos = await repositorioCargos.ListarPorCarreraAsync(carreraId, ct);
        var subtotalCargos = cargos.Sum(c => decimal.Round(c.SueldoBaseMensual * c.CantidadDefault, 2));

        var subtotalMat = await repositorioMateriales.SumarCostoMensualPorCategoriaAsync(carreraId, "MATERIALES_SUMINISTROS", ct);
        var subtotalAseo = await repositorioMateriales.SumarCostoMensualPorCategoriaAsync(carreraId, "ASEO_LIMPIEZA", ct);
        var subtotalAcc = await repositorioMateriales.SumarCostoMensualPorCategoriaAsync(carreraId, "ACCESORIOS_MATERIALES", ct);

        return new ResumenCapitalTrabajoDto
        {
            SubtotalCargos = subtotalCargos,
            SubtotalMateriales = subtotalMat,
            SubtotalAseo = subtotalAseo,
            SubtotalAccesorios = subtotalAcc,
        };
    }
}
