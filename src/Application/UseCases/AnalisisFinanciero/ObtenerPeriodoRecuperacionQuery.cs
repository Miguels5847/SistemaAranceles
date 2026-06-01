using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Services.Financieros;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.AnalisisFinanciero;

public sealed class ObtenerPeriodoRecuperacionQuery(
    ObtenerFlujoFondosQuery obtenerFlujoFondosQuery,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioDatosInstitucionales repositorioDatos)
{
    public async Task<PeriodoRecuperacionDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        FlujoFondosDto? flujoPrecalculado = null,
        decimal factorImprevisto = 1.05m)
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
                "Selecciona un escenario para calcular el periodo de recuperación.");
        }

        var flujo = flujoPrecalculado
            ?? await obtenerFlujoFondosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct, factorImprevisto: factorImprevisto);
        AgregarAdvertencia(advertencias, flujo.MensajeAdvertencia);

        if (!flujo.TieneDatos || flujo.ValoresPorPeriodo.Count == 0)
        {
            return Vacio(
                carreraId,
                carrera?.Nombre ?? flujo.CarreraNombre,
                escenarioProyeccionId,
                escenario?.Nombre ?? flujo.EscenarioNombre,
                ConstruirMensaje(advertencias, "No hay Flujo de Fondos para calcular el periodo de recuperación."));
        }

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var semestresPorAnio = datos?.SemestresPorAnio is > 0
            ? datos.SemestresPorAnio
            : DatosInstitucionales.SemestresPorAnioPorDefecto;
        var mesesPorPeriodo = decimal.Round(12m / semestresPorAnio, 4);

        var entradas = flujo.ValoresPorPeriodo
            .OrderBy(p => p.PeriodoOrden)
            .Select(p => new EntradaPeriodoRecuperacion
            {
                PeriodoOrden = p.PeriodoOrden,
                EtiquetaPeriodo = p.EtiquetaPeriodo,
                FlujoNeto = p.FlujoNeto,
                FlujoAcumulado = p.FlujoAcumulado
            })
            .ToList();
        var resultado = CalculadoraPeriodoRecuperacion.Calcular(entradas, mesesPorPeriodo);
        // El período de recuperación se calcula con el arancel vigente del escenario; si no recupera,
        // se aclara para que el usuario revise el arancel financiero sugerido por VAN=0.
        if (!resultado.Recuperado)
            advertencias.Add("No recuperado dentro del horizonte proyectado con el arancel vigente. Revise el arancel financiero sugerido por VAN=0.");
        else
            AgregarAdvertencia(advertencias, resultado.Mensaje);

        var detalle = entradas
            .Select(p => new PeriodoRecuperacionDetalleDto
            {
                PeriodoOrden = p.PeriodoOrden,
                EtiquetaPeriodo = p.EtiquetaPeriodo,
                FlujoNeto = p.FlujoNeto,
                FlujoAcumulado = p.FlujoAcumulado,
                EsPeriodoRecuperacion = resultado.Recuperado && resultado.PeriodoOrdenRecuperacion == p.PeriodoOrden,
                Estado = resultado.Recuperado && resultado.PeriodoOrdenRecuperacion == p.PeriodoOrden
                    ? "Recuperación"
                    : p.FlujoAcumulado < 0m ? "Pendiente" : "Recuperado"
            })
            .ToList();

        return new PeriodoRecuperacionDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? flujo.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? flujo.EscenarioNombre,
            Recuperado = resultado.Recuperado,
            Estado = resultado.Estado,
            PeriodoRecuperacion = resultado.PeriodoRecuperacion,
            MesesPorPeriodo = mesesPorPeriodo,
            TotalMeses = resultado.TotalMeses,
            Anios = resultado.Anios,
            Meses = resultado.Meses,
            Dias = resultado.Dias,
            FlujoFaltanteAnterior = resultado.FlujoFaltanteAnterior,
            ProporcionPeriodo = resultado.ProporcionPeriodo,
            FlujoAcumuladoFinal = flujo.FlujoAcumuladoFinal,
            Detalle = detalle,
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

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

    private static PeriodoRecuperacionDto Vacio(
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
            Estado = "Sin datos",
            MensajeAdvertencia = mensaje
        };
}
