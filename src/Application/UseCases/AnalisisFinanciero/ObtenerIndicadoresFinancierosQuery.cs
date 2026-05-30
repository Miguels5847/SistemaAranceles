using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Services.Financieros;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.AnalisisFinanciero;

public sealed class ObtenerIndicadoresFinancierosQuery(
    ObtenerFlujoFondosQuery obtenerFlujoFondosQuery,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioInflacionAnual repositorioInflacion,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario)
{
    private const decimal ToleranciaEstado = 0.01m;

    public async Task<IndicadoresFinancierosDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        FlujoFondosDto? flujoPrecalculado = null)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;
        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
        {
            return Vacio(
                carreraId,
                carrera?.Nombre ?? string.Empty,
                escenarioProyeccionId,
                escenario?.Nombre ?? string.Empty,
                "Selecciona un escenario para calcular los indicadores financieros.");
        }

        var flujo = flujoPrecalculado
            ?? await obtenerFlujoFondosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, flujo.MensajeAdvertencia);

        if (!flujo.TieneDatos || flujo.ValoresPorPeriodo.Count == 0)
        {
            return Vacio(
                carreraId,
                carrera?.Nombre ?? flujo.CarreraNombre,
                escenarioProyeccionId,
                escenario?.Nombre ?? flujo.EscenarioNombre,
                ConstruirMensaje(advertencias, "No hay Flujo de Fondos para calcular los indicadores financieros."));
        }

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var tasaInteres = datos?.TasaInteresFinanciera ?? DatosInstitucionales.TasaInteresFinancieraPorDefecto;
        var premioRiesgo = datos?.PremioRiesgo ?? DatosInstitucionales.PremioRiesgoPorDefecto;
        var usarTmrManual = datos?.UsarTmrManual ?? DatosInstitucionales.UsarTmrManualPorDefecto;
        var tmrManual = datos?.TmrManual ?? DatosInstitucionales.TmrManualPorDefecto;

        var (inflacionPromedio, anioDesde, anioHasta, tieneInflacion) =
            await ObtenerInflacionPromedioAsync(flujo, ct);
        if (!usarTmrManual && !tieneInflacion)
            advertencias.Add("No hay registros de inflación para el horizonte; la inflación promedio se tomó como 0%.");

        var tmr = CalculadoraTMR.Calcular(new ParametrosTmr
        {
            TasaInteresFinancieraPorcentaje = tasaInteres,
            InflacionPromedioPorcentaje = inflacionPromedio,
            PremioRiesgoPorcentaje = premioRiesgo,
            UsarTmrManual = usarTmrManual,
            TmrManualPorcentaje = tmrManual
        });

        var flujos = flujo.ValoresPorPeriodo.Select(p => p.FlujoNeto).ToList();
        var van = CalculadoraVAN.Calcular(flujos, tmr.TmrTasa);
        var tir = CalculadoraTIR.Calcular(flujos);
        if (!tir.EsCalculable)
            AgregarAdvertencia(advertencias, tir.Mensaje);

        var detalleVan = flujo.ValoresPorPeriodo
            .Select((p, t) => new IndicadorVanPeriodoDto
            {
                PeriodoOrden = p.PeriodoOrden,
                EtiquetaPeriodo = p.EtiquetaPeriodo,
                FlujoNeto = p.FlujoNeto,
                FactorDescuento = CalculadoraVAN.FactorDescuento(tmr.TmrTasa, t),
                ValorPresente = CalculadoraVAN.ValorPresente(p.FlujoNeto, tmr.TmrTasa, t)
            })
            .ToList();

        return new IndicadoresFinancierosDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? flujo.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? flujo.EscenarioNombre,
            EsTmrManual = tmr.EsManual,
            TmrPorcentaje = tmr.TmrPorcentaje,
            TmrTasa = tmr.TmrTasa,
            TasaInteresFinancieraPorcentaje = tmr.TasaInteresFinancieraPorcentaje,
            InflacionPromedioPorcentaje = tmr.InflacionPromedioPorcentaje,
            PremioRiesgoPorcentaje = tmr.PremioRiesgoPorcentaje,
            AnioInflacionDesde = anioDesde,
            AnioInflacionHasta = anioHasta,
            TieneDatosInflacion = tieneInflacion,
            Van = van,
            DetalleVan = detalleVan,
            EsTirCalculable = tir.EsCalculable,
            TirPorcentaje = tir.EsCalculable ? decimal.Round(tir.Tir * 100m, 2) : 0m,
            TirIteraciones = tir.Iteraciones,
            EstadoViabilidad = DeterminarEstado(van),
            TieneDatos = true,
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

    private async Task<(decimal promedio, int desde, int hasta, bool tieneDatos)> ObtenerInflacionPromedioAsync(
        FlujoFondosDto flujo,
        CancellationToken ct)
    {
        var anios = flujo.ValoresPorPeriodo
            .Where(p => p.PeriodoOrden > 0 && p.Anio > 0)
            .Select(p => p.Anio)
            .Distinct()
            .OrderBy(a => a)
            .ToList();

        if (anios.Count == 0)
            return (0m, 0, 0, false);

        var desde = anios.First();
        var hasta = anios.Last();
        var registros = await repositorioInflacion.ListarPorRangoAsync(desde, hasta, ct);

        // Un registro por año (misma desambiguación que CalculoInflacionAplicada).
        var porAnio = registros
            .GroupBy(r => r.Anio)
            .Select(g => g.OrderBy(r => r.TipoFuente).First())
            .ToList();

        if (porAnio.Count == 0)
            return (0m, desde, hasta, false);

        var promedio = decimal.Round(porAnio.Average(r => r.PorcentajeInflacion), 4);
        return (promedio, desde, hasta, true);
    }

    private static string DeterminarEstado(decimal van)
        => van > ToleranciaEstado
            ? "Viable"
            : van < -ToleranciaEstado
                ? "No viable"
                : "En equilibrio";

    private static void AgregarAdvertencia(List<string> advertencias, string? mensaje)
    {
        if (!string.IsNullOrWhiteSpace(mensaje))
            advertencias.Add(mensaje.Trim());
    }

    private static string? ConstruirMensaje(List<string> advertencias, string? adicional = null)
    {
        if (!string.IsNullOrWhiteSpace(adicional))
            advertencias.Add(adicional);

        return advertencias.Count == 0
            ? null
            : string.Join(" ", advertencias.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static IndicadoresFinancierosDto Vacio(
        int carreraId,
        string carreraNombre,
        int? escenarioId,
        string escenarioNombre,
        string? mensaje)
        => new()
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EscenarioProyeccionId = escenarioId,
            EscenarioNombre = escenarioNombre,
            EstadoViabilidad = "Sin datos",
            MensajeAdvertencia = mensaje
        };
}
