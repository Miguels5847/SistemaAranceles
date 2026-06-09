using SistemaAranceles.Application.DTOs.CapitalTrabajo;
using SistemaAranceles.Application.DTOs.InversionInicial;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CapitalTrabajo;
using SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.InversionInicial;

/// <summary>
/// Consolida la inversion inicial total (KAN-30).
/// Estructura: Activos Diferidos + todas las categorias de Activos Fijos + Cap. Trabajo + Imprevistos configurable.
/// </summary>
public sealed class ObtenerInversionInicialTotalQuery(
    IRepositorioActivoDiferido repositorioActivoDiferido,
    ObtenerTotalesActivosQuery totalesActivosQuery,
    ObtenerResumenCapitalTrabajoQuery resumenCapitalQuery,
    IRepositorioDatosInstitucionales repositorioDatosInstitucionales)
{
    private const decimal PorcentajeImprevistosFallback = DatosInstitucionales.PorcentajeImprevistosInversionPorDefecto;

    public async Task<InversionInicialTotalDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default,
        ResumenCapitalTrabajoDto? capitalTrabajoPrecalculado = null)
    {
        var activosDiferidos = await repositorioActivoDiferido.SumarValorPorCarreraAsync(carreraId, ct);
        var totalesActivos = await totalesActivosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var resumenCap = capitalTrabajoPrecalculado
            ?? await resumenCapitalQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var datos = await repositorioDatosInstitucionales.ObtenerVigenteAsync(ct);
        var porcentajeImprevistos = datos?.PorcentajeImprevistosInversion is >= 0m and <= 100m
            ? datos.PorcentajeImprevistosInversion
            : PorcentajeImprevistosFallback;

        var categoriasActivosFijos = totalesActivos.PorCategoria
            .Select(x => new CategoriaActivoFijoInversionDto
            {
                Categoria = x.Categoria,
                CategoriaNombre = x.CategoriaNombre,
                Valor = x.SubtotalValorTotal
            })
            .ToList();

        decimal Get(CategoriaActivoFijo cat) =>
            totalesActivos.PorCategoria.FirstOrDefault(x => x.Categoria == cat)?.SubtotalValorTotal ?? 0m;

        var muebles = Get(CategoriaActivoFijo.MueblesEnseres);
        var labs = Get(CategoriaActivoFijo.LaboratoriosEquipos);
        var computo = Get(CategoriaActivoFijo.EquipoComputo);
        var oficina = Get(CategoriaActivoFijo.EquipoOficina);
        var subtotalFijos = categoriasActivosFijos.Sum(x => x.Valor);

        var capitalTrabajo = resumenCap.TotalCapitalTrabajo;
        var baseImprevistos = subtotalFijos + capitalTrabajo;
        var imprevistos = decimal.Round(baseImprevistos * (porcentajeImprevistos / 100m), 2);
        var total = decimal.Round(activosDiferidos + subtotalFijos + capitalTrabajo + imprevistos, 2);

        return new InversionInicialTotalDto
        {
            ActivosDiferidos = activosDiferidos,
            CategoriasActivosFijos = categoriasActivosFijos,
            MueblesEnseres = muebles,
            LaboratoriosEquipos = labs,
            EquipoComputo = computo,
            EquipoOficina = oficina,
            SubtotalActivosFijos = subtotalFijos,
            CapitalTrabajoDosM = capitalTrabajo,
            MesesCapitalTrabajo = resumenCap.MesesCapitalTrabajo,
            PorcentajeImprevistosInversion = porcentajeImprevistos,
            Imprevistos = imprevistos,
            TotalInversion = total,
        };
    }
}
