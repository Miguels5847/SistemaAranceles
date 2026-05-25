using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// Resuelve el arancel y matrícula efectivos por carrera+escenario.
/// Si Modo=AutomaticoCostoCarrera y no hay costo (Épica 10 pendiente),
/// devuelve ArancelEfectivo=null con mensaje de advertencia.
/// </summary>
public sealed class ObtenerArancelEfectivoQuery(
    IRepositorioConfiguracionArancelCarrera repositorioArancel,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioDatosInstitucionales repositorioDatos)
{
    public async Task<ArancelEfectivoDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var carreraNombre = carrera?.Nombre ?? string.Empty;

        var configuracion = await repositorioArancel.ObtenerPorCarreraEscenarioAsync(carreraId, escenarioProyeccionId, ct);

        if (configuracion is null)
        {
            return new ArancelEfectivoDto
            {
                CarreraId = carreraId,
                CarreraNombre = carreraNombre,
                EscenarioProyeccionId = escenarioProyeccionId,
                ModoCalculoArancel = "Manual",
                ArancelEfectivo = null,
                MatriculaEfectiva = 0m,
                PorcentajeMatriculaAplicado = 0m,
                FuenteCalculo = "Sin configuración",
                MensajeAdvertencia = "No existe configuración de arancel para esta carrera/escenario."
            };
        }

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var porcentajeInstitucional = ObtenerPorcentajeMatriculaInstitucional(datos);

        var modo = Enum.TryParse<ModoCalculoArancel>(configuracion.ModoCalculoArancel, ignoreCase: true, out var m)
            ? m
            : ModoCalculoArancel.Manual;

        decimal? arancel;
        string fuente;
        string? advertencia = null;

        if (modo == ModoCalculoArancel.Manual)
        {
            arancel = configuracion.ArancelManual;
            fuente = "Manual";
            if (arancel is null or <= 0m)
            {
                advertencia = "Arancel manual no definido.";
            }
        }
        else
        {
            arancel = null;
            fuente = "Automatico (Costo Carrera)";
            advertencia = "Cálculo automático pendiente — requiere Épica 10 (Costo de Carrera).";
        }

        var porcentajeAplicado = configuracion.UsaPorcentajeMatriculaInstitucional
            ? porcentajeInstitucional
            : configuracion.PorcentajeMatricula ?? 0m;
        var matricula = arancel is > 0m
            ? decimal.Round(arancel.Value * porcentajeAplicado / 100m, 2)
            : 0m;

        return new ArancelEfectivoDto
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            ModoCalculoArancel = configuracion.ModoCalculoArancel,
            ArancelEfectivo = arancel,
            MatriculaEfectiva = matricula,
            PorcentajeMatriculaAplicado = porcentajeAplicado,
            FuenteCalculo = fuente,
            MensajeAdvertencia = advertencia
        };
    }

    private static decimal ObtenerPorcentajeMatriculaInstitucional(SistemaAranceles.Domain.Entities.DatosInstitucionales? datos)
        => datos?.PorcentajeMatriculaDefault ?? SistemaAranceles.Domain.Entities.DatosInstitucionales.PorcentajeMatriculaDefaultPorDefecto;
}
