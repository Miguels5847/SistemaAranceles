using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Services.Financieros;

namespace SistemaAranceles.Application.UseCases.AnalisisFinanciero;

public sealed class ObtenerDashboardFinancieroQuery(
    ObtenerEstadoPerdidasGananciasQuery obtenerEstadoPerdidasGananciasQuery,
    ObtenerFlujoFondosQuery obtenerFlujoFondosQuery,
    ObtenerIndicadoresFinancierosQuery obtenerIndicadoresFinancierosQuery,
    ObtenerPeriodoRecuperacionQuery obtenerPeriodoRecuperacionQuery,
    ObtenerPuntoEquilibrioQuery obtenerPuntoEquilibrioQuery,
    ObtenerArancelOptimoBiseccionQuery obtenerArancelOptimoBiseccionQuery,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario)
{
    public async Task<DashboardFinancieroDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        EstadoPerdidasGananciasDto? estadoPrecalculado = null,
        FlujoFondosDto? flujoPrecalculado = null,
        IndicadoresFinancierosDto? indicadoresPrecalculados = null,
        PeriodoRecuperacionDto? periodoRecuperacionPrecalculado = null,
        PuntoEquilibrioDto? puntoEquilibrioPrecalculado = null,
        ArancelOptimoBiseccionDto? arancelOptimoPrecalculado = null)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;

        if (escenarioProyeccionId is null or <= 0)
        {
            return new DashboardFinancieroDto
            {
                CarreraId = carreraId,
                CarreraNombre = carrera?.Nombre ?? string.Empty,
                EscenarioProyeccionId = escenarioProyeccionId,
                EscenarioNombre = escenario?.Nombre ?? string.Empty,
                MensajeAdvertencia = "Selecciona un escenario para mostrar el dashboard financiero."
            };
        }

        var advertencias = new List<string>();
        var estado = estadoPrecalculado
            ?? await obtenerEstadoPerdidasGananciasQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, estado.MensajeAdvertencia);

        var flujo = flujoPrecalculado
            ?? await obtenerFlujoFondosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, estado);
        AgregarAdvertencia(advertencias, flujo.MensajeAdvertencia);

        var indicadores = indicadoresPrecalculados
            ?? await obtenerIndicadoresFinancierosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, flujo);
        AgregarAdvertencia(advertencias, indicadores.MensajeAdvertencia);

        var recuperacion = periodoRecuperacionPrecalculado
            ?? await obtenerPeriodoRecuperacionQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, flujo);
        AgregarAdvertencia(advertencias, recuperacion.MensajeAdvertencia);

        var puntoEquilibrio = puntoEquilibrioPrecalculado
            ?? await obtenerPuntoEquilibrioQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, estadoPrecalculado: estado);
        AgregarAdvertencia(advertencias, puntoEquilibrio.MensajeAdvertencia);

        var arancelOptimo = arancelOptimoPrecalculado
            ?? await obtenerArancelOptimoBiseccionQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, arancelOptimo.MensajeAdvertencia);

        return ConsolidadorDashboardFinanciero.Construir(
            carreraId,
            carrera?.Nombre ?? estado.CarreraNombre,
            escenarioProyeccionId,
            escenario?.Nombre ?? estado.EscenarioNombre,
            estado,
            flujo,
            indicadores,
            recuperacion,
            puntoEquilibrio,
            arancelOptimo,
            ConstruirMensaje(advertencias));
    }

    private static void AgregarAdvertencia(List<string> advertencias, string? mensaje)
    {
        if (!string.IsNullOrWhiteSpace(mensaje))
            advertencias.Add(mensaje.Trim());
    }

    private static string? ConstruirMensaje(List<string> advertencias)
        => advertencias.Count == 0
            ? null
            : string.Join(" ", advertencias.Distinct(StringComparer.OrdinalIgnoreCase));
}
