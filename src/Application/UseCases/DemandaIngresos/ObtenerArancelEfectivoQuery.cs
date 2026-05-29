using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CostosGastos;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

/// <summary>
/// Resuelve el arancel y matricula efectivos por carrera+escenario.
/// En modo AutomaticoCostoCarrera toma el arancel sugerido del modulo Costos y Gastos.
/// </summary>
public sealed class ObtenerArancelEfectivoQuery(
    IRepositorioConfiguracionArancelCarrera repositorioArancel,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioDatosInstitucionales repositorioDatos,
    ObtenerArancelOptimoCarreraQuery? obtenerArancelOptimoCarreraQuery = null)
{
    public async Task<ArancelEfectivoDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var carreraNombre = carrera?.Nombre ?? string.Empty;

        var configuracion = await repositorioArancel.ObtenerPorCarreraEscenarioAsync(carreraId, escenarioProyeccionId, ct);
        if (configuracion is null && escenarioProyeccionId is not null)
            configuracion = await repositorioArancel.ObtenerPorCarreraEscenarioAsync(carreraId, null, ct);

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
                FuenteCalculo = "Sin configuracion",
                MensajeAdvertencia = "No existe configuracion de arancel para esta carrera/escenario."
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
                advertencia = "Arancel manual no definido.";
        }
        else
        {
            if (obtenerArancelOptimoCarreraQuery is null)
            {
                arancel = null;
                fuente = "Automatico (Costo Carrera)";
                advertencia = "Costo de Carrera pendiente; no se puede resolver el arancel automatico.";
            }
            else
            {
                var optimo = await obtenerArancelOptimoCarreraQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
                fuente = "Automatico (Costo Carrera)";
                if (optimo.Disponible)
                {
                    arancel = optimo.ArancelSugeridoSemestre;
                    advertencia = optimo.MensajeAdvertencia;
                }
                else
                {
                    arancel = null;
                    advertencia = optimo.MensajeAdvertencia ?? "Costo de Carrera pendiente; no se puede resolver el arancel automatico.";
                }
            }
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

    private static decimal ObtenerPorcentajeMatriculaInstitucional(DatosInstitucionales? datos)
        => datos?.PorcentajeMatriculaDefault ?? DatosInstitucionales.PorcentajeMatriculaDefaultPorDefecto;
}
