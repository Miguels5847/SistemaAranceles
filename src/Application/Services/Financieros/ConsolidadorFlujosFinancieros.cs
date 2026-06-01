using SistemaAranceles.Application.DTOs.AnalisisFinanciero;

namespace SistemaAranceles.Application.Services.Financieros;

public sealed class FlujoAnualFinancieroDto
{
    public int Orden { get; init; }
    public int? Anio { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public decimal FlujoNeto { get; init; }
}

/// <summary>
/// Consolida el flujo de fondos semestral en flujo ANUAL para los indicadores VAN/TIR.
/// El docente analiza VAN/TIR por año aunque el sistema muestre detalle semestral, por lo que
/// no se debe descontar una TMR anual sobre períodos semestrales como si cada semestre fuese un año.
///
/// Reglas:
/// - El período 0 (inversión inicial) queda en orden 0 y NUNCA se agrupa con un año operativo.
/// - Cada año posterior suma los flujos netos de sus semestres.
/// </summary>
public static class ConsolidadorFlujosFinancieros
{
    public static IReadOnlyList<FlujoAnualFinancieroDto> ConstruirDetalleAnual(
        IReadOnlyList<FlujoFondosPeriodoDto> valoresPorPeriodo)
    {
        var resultado = new List<FlujoAnualFinancieroDto>();

        var periodo0 = valoresPorPeriodo.FirstOrDefault(p => p.PeriodoOrden == 0);
        resultado.Add(new FlujoAnualFinancieroDto
        {
            Orden = 0,
            Anio = periodo0?.Anio > 0 ? periodo0.Anio : null,
            Etiqueta = string.IsNullOrWhiteSpace(periodo0?.EtiquetaPeriodo) ? "Periodo 0" : periodo0!.EtiquetaPeriodo,
            FlujoNeto = periodo0?.FlujoNeto ?? 0m
        });

        var anuales = valoresPorPeriodo
            .Where(p => p.PeriodoOrden > 0 && p.Anio > 0)
            .GroupBy(p => p.Anio)
            .OrderBy(g => g.Key)
            .Select((g, index) => new FlujoAnualFinancieroDto
            {
                Orden = index + 1,
                Anio = g.Key,
                Etiqueta = $"Año {g.Key}",
                FlujoNeto = decimal.Round(g.Sum(x => x.FlujoNeto), 2)
            });
        resultado.AddRange(anuales);

        return resultado;
    }

    public static IReadOnlyList<decimal> ConstruirFlujosAnuales(
        IReadOnlyList<FlujoFondosPeriodoDto> valoresPorPeriodo)
        => ConstruirDetalleAnual(valoresPorPeriodo).Select(f => f.FlujoNeto).ToList();

    /// <summary>
    /// Núcleo genérico para módulos que no usan <see cref="FlujoFondosPeriodoDto"/> (p. ej. la
    /// evaluación de aranceles candidatos en bisección). El período 0 se entrega aparte.
    /// </summary>
    public static IReadOnlyList<decimal> ConstruirFlujosAnuales(
        decimal flujoPeriodo0,
        IEnumerable<(int Anio, decimal FlujoNeto)> flujosOperativos)
    {
        var resultado = new List<decimal> { decimal.Round(flujoPeriodo0, 2) };
        resultado.AddRange(flujosOperativos
            .Where(f => f.Anio > 0)
            .GroupBy(f => f.Anio)
            .OrderBy(g => g.Key)
            .Select(g => decimal.Round(g.Sum(x => x.FlujoNeto), 2)));
        return resultado;
    }
}
