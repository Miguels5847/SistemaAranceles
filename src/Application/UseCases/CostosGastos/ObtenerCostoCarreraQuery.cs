using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CostosGastos;

public sealed class ObtenerCostoCarreraQuery(
    ObtenerMatrizCostosGastosQuery obtenerMatrizCostosGastosQuery,
    ObtenerDemandaProyectadaQuery obtenerDemandaProyectadaQuery,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioConfiguracionArancelCarrera repositorioConfiguracionArancel)
{
    public async Task<CostoCarreraResultadoDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        MatrizCostosGastosDto? matrizPrecalculada = null)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;
        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
            return Vacio(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId, escenario?.Nombre ?? string.Empty, "Selecciona un escenario para calcular Costo de la Carrera.");

        // Reutiliza la matriz de Costos y Gastos si ya fue calculada (evita recomputar el consolidado completo).
        var matriz = matrizPrecalculada
            ?? await obtenerMatrizCostosGastosQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, matriz.MensajeAdvertencia);

        var demanda = await obtenerDemandaProyectadaQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertenciaDocentes);

        if (!matriz.TieneDatos || !demanda.TieneDatos)
            return Vacio(carreraId, carrera?.Nombre ?? matriz.CarreraNombre, escenarioProyeccionId, escenario?.Nombre ?? matriz.EscenarioNombre, ConstruirMensaje(advertencias, "No hay datos suficientes para calcular Costo de la Carrera."));

        var estudiantes = ObtenerMatrizInvVinBecasQuery.CompletarValores(demanda.TotalesPorPeriodo, matriz.ValoresPorPeriodo.Count);
        var periodos = new List<CostoCarreraPeriodoDto>();
        for (var i = 0; i < matriz.ValoresPorPeriodo.Count; i++)
        {
            // Hoja 11 Costo de la Carrera usa el Total Costos y Gastos BRUTO, no el descontado por
            // becas gobierno (ese bloque es solo análisis en la hoja 10 y puede quedar negativo).
            var costoPeriodoOriginal = decimal.Round(matriz.ValoresPorPeriodo[i].TotalCostosGastos, 2);
            var costoPeriodo = Math.Max(0m, costoPeriodoOriginal);
            if (costoPeriodoOriginal < 0m)
                advertencias.Add($"Se detectó un costo negativo en el período {matriz.ValoresPorPeriodo[i].EtiquetaPeriodo}; se usó 0 para evitar arancel negativo.");

            // Nº estudiantes redondeado a entero para coincidir con Proyección de Estudiantes.
            var estudiantesPeriodo = decimal.Round(estudiantes[i], 0, MidpointRounding.AwayFromZero);
            if (estudiantesPeriodo <= 0m)
                advertencias.Add($"Período {matriz.ValoresPorPeriodo[i].EtiquetaPeriodo}: estudiantes en 0; costo por estudiante queda en 0.");

            periodos.Add(new CostoCarreraPeriodoDto
            {
                PeriodoAcademicoId = matriz.ValoresPorPeriodo[i].PeriodoAcademicoId,
                Anio = matriz.ValoresPorPeriodo[i].Anio,
                NumeroPeriodo = matriz.ValoresPorPeriodo[i].NumeroPeriodo,
                EtiquetaPeriodo = matriz.ValoresPorPeriodo[i].EtiquetaPeriodo,
                TotalCostosGastos = costoPeriodo,
                NumeroEstudiantes = estudiantesPeriodo,
                CostoPorEstudiante = estudiantesPeriodo > 0m
                    ? decimal.Round(costoPeriodo / estudiantesPeriodo, 2)
                    : 0m
            });
        }

        var numeroSemestres = carrera?.TotalCiclos is > 0
            ? carrera.TotalCiclos
            : periodos.Count;
        if (numeroSemestres <= 0)
            advertencias.Add("La carrera no tiene número de semestres/ciclos configurado.");

        var costoCarreraCompleta = decimal.Round(periodos.Sum(p => p.CostoPorEstudiante), 2);
        var arancelSugerido = numeroSemestres > 0
            ? decimal.Round(costoCarreraCompleta / numeroSemestres, 2)
            : 0m;
        var porcentajeMatricula = await ObtenerPorcentajeMatriculaAsync(carreraId, escenarioProyeccionId, ct);
        var matricula = decimal.Round(arancelSugerido * porcentajeMatricula / 100m, 2);

        return new CostoCarreraResultadoDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? matriz.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? matriz.EscenarioNombre,
            Periodos = periodos,
            NumeroSemestresCarrera = numeroSemestres,
            PorcentajeMatriculaAplicado = porcentajeMatricula,
            CostoCarreraCompleta = costoCarreraCompleta,
            ArancelSugeridoSemestre = arancelSugerido,
            MatriculaSugerida = matricula,
            TotalPorSemestre = arancelSugerido + matricula,
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

    private async Task<decimal> ObtenerPorcentajeMatriculaAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct)
    {
        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var porcentajeInstitucional = datos?.PorcentajeMatriculaDefault
            ?? DatosInstitucionales.PorcentajeMatriculaDefaultPorDefecto;

        var configuracion = await repositorioConfiguracionArancel.ObtenerPorCarreraEscenarioAsync(carreraId, escenarioProyeccionId, ct);
        if (configuracion is null && escenarioProyeccionId is not null)
            configuracion = await repositorioConfiguracionArancel.ObtenerPorCarreraEscenarioAsync(carreraId, null, ct);

        if (configuracion is null || configuracion.UsaPorcentajeMatriculaInstitucional)
            return porcentajeInstitucional;

        return configuracion.PorcentajeMatricula ?? 0m;
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

    private static CostoCarreraResultadoDto Vacio(
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
            MensajeAdvertencia = mensaje
        };
}
