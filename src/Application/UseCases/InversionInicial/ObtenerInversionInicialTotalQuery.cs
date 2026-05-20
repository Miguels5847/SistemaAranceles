using SistemaAranceles.Application.DTOs.InversionInicial;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Application.UseCases.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.InversionInicial;

/// <summary>
/// Consolida la inversión inicial total (KAN-30).
/// Estructura: Activos Diferidos + Activos Fijos (4 categorías) + Cap. Trabajo (2 meses) + Imprevistos 5%
/// </summary>
public sealed class ObtenerInversionInicialTotalQuery(
    IRepositorioActivoDiferido repositorioActivoDiferido,
    ObtenerTotalesActivosQuery totalesActivosQuery,
    ObtenerResumenCapitalTrabajoQuery resumenCapitalQuery)
{
    private const decimal PorcentajeImprevistos = 0.05m;

    public async Task<InversionInicialTotalDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default)
    {
        var activosDiferidos = await repositorioActivoDiferido.SumarValorPorCarreraAsync(carreraId, ct);
        var totalesActivos = await totalesActivosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var resumenCap = await resumenCapitalQuery.EjecutarAsync(carreraId, ct);

        decimal Get(CategoriaActivoFijo cat) =>
            totalesActivos.PorCategoria.FirstOrDefault(x => x.Categoria == cat)?.SubtotalValorTotal ?? 0m;

        var muebles = Get(CategoriaActivoFijo.MueblesEnseres);
        var labs = Get(CategoriaActivoFijo.LaboratoriosEquipos);
        var computo = Get(CategoriaActivoFijo.EquipoComputo);
        var oficina = Get(CategoriaActivoFijo.EquipoOficina);
        var subtotalFijos = muebles + labs + computo + oficina;

        var capitalDosM = resumenCap.TotalDosMeses;
        var baseImprevistos = subtotalFijos + capitalDosM;
        var imprevistos = decimal.Round(baseImprevistos * PorcentajeImprevistos, 2);
        var total = decimal.Round(activosDiferidos + subtotalFijos + capitalDosM + imprevistos, 2);

        return new InversionInicialTotalDto
        {
            ActivosDiferidos = activosDiferidos,
            MueblesEnseres = muebles,
            LaboratoriosEquipos = labs,
            EquipoComputo = computo,
            EquipoOficina = oficina,
            SubtotalActivosFijos = subtotalFijos,
            CapitalTrabajoDosM = capitalDosM,
            Imprevistos = imprevistos,
            TotalInversion = total,
        };
    }
}
