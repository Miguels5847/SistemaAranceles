using System.Globalization;
using System.Text;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Application.UseCases.SueldosPlantaCentral;
using SistemaAranceles.Domain.Common;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Genera el pivot completo "Resumen Sueldos": filas=cargos, columnas=todos los períodos
/// de la proyección de estudiantes (carrera+escenario). Cada celda es el costo semestral
/// del cargo en ese período aplicando inflación encadenada por año.
/// </summary>
public sealed class GenerarResumenSueldosQuery(
    IRepositorioCargoFacultad repositorioCargo,
    IRepositorioInflacionAnual repositorioInflacion,
    IRepositorioProyeccionEstudiantes repositorioProyeccionEstudiantes,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioDatosInstitucionales repositorioDatosInstitucionales,
    IRepositorioConfiguracionRetencion repositorioConfiguracionRetencion,
    IRepositorioOverrideHorasPeriodo repositorioOverrides)
{
    private static readonly string[] OrdenCargos =
    [
        "Decano",
        "Subdecano",
        "Director de Carrera",
        "Secretario",
        "Auxiliar de Secretaria",
        "Coordinador",
        "Bienestar Estudiantil",
        "Tiempo Completo PhD",
        "Tiempo Completo Mgs.",
        "Medio Tiempo",
        "Tiempo Parcial",
        "Ocasional Tipo 2 (Técnico Docente)",
        "Bibliotecario",
        "Auxiliar de Servicio",
        "Guardia",
    ];

    public async Task<ResumenSueldosVistaDto> EjecutarAsync(
        int carreraId,
        int escenarioProyeccionId,
        decimal estudiantesUA,
        ProyeccionConsolidadaDto? consolidadoActual = null,
        CancellationToken cancellationToken = default)
    {
        var generadorPeriodo = new GenerarTablaSueldosPeriodoQuery(
            repositorioCargo,
            repositorioInflacion,
            repositorioProyeccionEstudiantes,
            repositorioCarrera);

        var contextoPeriodo = await generadorPeriodo.PrepararContextoAsync(
            carreraId,
            escenarioProyeccionId,
            cancellationToken);

        if (contextoPeriodo is null || contextoPeriodo.Proyeccion.Detalles.Count == 0)
            return new ResumenSueldosVistaDto { CarreraId = carreraId };

        var proyeccion = contextoPeriodo.Proyeccion;

        // Sin consolidado los cargos DOCENTES quedan en $0 (no hay personas por período).
        // Si el caller no lo provee (reportes, CES), se construye aquí con la misma receta
        // que la matriz de Costos y Gastos (KAN-49).
        consolidadoActual ??= await ConstructorConsolidadoProyeccion.ConstruirAsync(
            proyeccion,
            carreraId,
            escenarioProyeccionId,
            repositorioConfiguracionRetencion,
            repositorioOverrides,
            cancellationToken);
        var periodos = ConstruirPeriodos(proyeccion);
        var cargosOrdenados = OrdenarCargos(contextoPeriodo.Cargos);
        var tablasPorPeriodo = periodos
            .Select(p => generadorPeriodo.GenerarParaPeriodo(
                contextoPeriodo,
                carreraId,
                p.PeriodoAcademicoId,
                estudiantesUA,
                consolidadoActual))
            .ToList();

        var filas = new List<FilaResumenSueldosDto>(cargosOrdenados.Count);
        var totalesPorPeriodo = new decimal[periodos.Count];

        foreach (var cargo in cargosOrdenados)
        {
            var valores = new decimal[periodos.Count];

            decimal pesoUltimoPeriodo = 0m;
            decimal personasUltimoPeriodo = 0m;
            var pesosPorPeriodo = new decimal[periodos.Count];

            for (int i = 0; i < periodos.Count; i++)
            {
                var filaPeriodo = BuscarFilaCargo(tablasPorPeriodo[i], cargo);
                var total = Math.Round(filaPeriodo?.TotalSemestre ?? 0m, 2);
                valores[i] = total;
                totalesPorPeriodo[i] += total;
                pesosPorPeriodo[i] = Math.Round(filaPeriodo?.Peso ?? 0m, 4);

                pesoUltimoPeriodo = pesosPorPeriodo[i];
                personasUltimoPeriodo = filaPeriodo?.NumeroPersonas ?? 0m;
            }

            filas.Add(new FilaResumenSueldosDto
            {
                CargoId = cargo.Id,
                NombreCargo = cargo.NombreCargo,
                EsCargoDocente = cargo.EsCargoDocente,
                Peso = pesoUltimoPeriodo,
                PesosPorPeriodo = pesosPorPeriodo,
                NumeroPersonas = personasUltimoPeriodo,
                ValoresPorPeriodo = valores,
                TotalFila = Math.Round(valores.Sum(), 2),
            });
        }

        for (int i = 0; i < totalesPorPeriodo.Length; i++)
            totalesPorPeriodo[i] = Math.Round(totalesPorPeriodo[i], 2);

        var ultimoPeriodo = periodos[^1];

        var granTotal = Math.Round(totalesPorPeriodo.Sum(), 2);

        // Intentar calcular aporte a Planta Central; si no hay datos institucionales, no interrumpimos el resumen.
        AportePlantaCentralCarreraDto? aporte = null;
        try
        {
            var calcular = new CalcularAportePlantaCentralCarreraQuery(
                repositorioDatosInstitucionales,
                repositorioProyeccionEstudiantes,
                repositorioCarrera);

            aporte = await calcular.EjecutarAsync(carreraId, escenarioProyeccionId, cancellationToken);
        }
        catch (DominioException)
        {
            aporte = null;
        }

        return new ResumenSueldosVistaDto
        {
            CarreraId = carreraId,
            CarreraNombre = contextoPeriodo.CarreraNombre,
            Periodos = periodos,
            Filas = filas,
            TotalesPorPeriodo = totalesPorPeriodo,
            GranTotal = granTotal,
            TotalSemestralPeriodoFinal = totalesPorPeriodo[^1],
            EtiquetaPeriodoFinal = $"P{ultimoPeriodo.NumeroPeriodo}-{ultimoPeriodo.Anio}",
            PlantaCentralDistribucion = aporte,
            TotalSueldosMasPlantaCentral = Math.Round(granTotal + (aporte?.AporteAcumulado ?? 0m), 2),
        };
    }

    private static List<PeriodoDisponibleSueldosDto> ConstruirPeriodos(ProyeccionEstudiantesDto proyeccion)
        => proyeccion.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .Select(g =>
            {
                var p = g.First();
                return new PeriodoDisponibleSueldosDto
                {
                    PeriodoAcademicoId = p.PeriodoAcademicoId,
                    Anio = p.Anio,
                    NumeroPeriodo = p.NumeroPeriodo,
                    EtiquetaPeriodo = p.EtiquetaPeriodo,
                };
            })
            .OrderBy(p => p.Anio)
            .ThenBy(p => p.NumeroPeriodo)
            .ToList();

    private static List<CargoFacultad> OrdenarCargos(IReadOnlyList<CargoFacultad> cargos)
        => cargos
            .OrderBy(ObtenerIndiceOrden)
            .ThenBy(c => c.NombreCargo)
            .ToList();

    private static int ObtenerIndiceOrden(CargoFacultad cargo)
    {
        for (var i = 0; i < OrdenCargos.Length; i++)
        {
            if (CoincideCargo(cargo.NombreCargo, OrdenCargos[i]))
                return i;
        }

        return int.MaxValue;
    }

    private static FilaSueldoPeriodoDto? BuscarFilaCargo(SueldosPeriodoVistaDto tablaPeriodo, CargoFacultad cargo)
        => tablaPeriodo.Filas.FirstOrDefault(f => f.CargoId == cargo.Id)
           ?? tablaPeriodo.Filas.FirstOrDefault(f => CoincideCargo(f.NombreCargo, cargo.NombreCargo));

    private static bool CoincideCargo(string nombreA, string nombreB)
    {
        var normalizadoA = NormalizarTexto(nombreA);
        var normalizadoB = NormalizarTexto(nombreB);

        if (normalizadoA.Length == 0 || normalizadoB.Length == 0)
            return false;

        if (string.Equals(normalizadoA, normalizadoB, StringComparison.Ordinal))
            return true;

        if (EsOcasionalTipo2(normalizadoA) && EsOcasionalTipo2(normalizadoB))
            return true;

        var tokensA = NormalizarTokens(normalizadoA);
        var tokensB = NormalizarTokens(normalizadoB);
        return tokensA.IsSubsetOf(tokensB) || tokensB.IsSubsetOf(tokensA);
    }

    private static bool EsOcasionalTipo2(string textoNormalizado)
        => textoNormalizado.Contains("ocasional tipo 2", StringComparison.Ordinal)
           || textoNormalizado.Contains("tecnico docente", StringComparison.Ordinal);

    private static HashSet<string> NormalizarTokens(string textoNormalizado)
        => textoNormalizado
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);

    private static string NormalizarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        var separadorPendiente = false;

        foreach (var c in descompuesto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
            {
                if (separadorPendiente && sb.Length > 0)
                    sb.Append(' ');

                sb.Append(c);
                separadorPendiente = false;
            }
            else
            {
                separadorPendiente = true;
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
