using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Domain.Retencion.Politicas;

public static class AjustesPorEscenario
{
    /// <summary>
    /// Los deltas de retención y graduación representan suma o resta directa sobre el porcentaje
    /// del escenario Histórico. No son factores multiplicativos.
    /// Ejemplo: 89.8 con delta -10 produce 79.8.
    /// </summary>
    public const int OPTIMISTA_RETENCION_DELTA_PP = 5;
    public const int OPTIMISTA_GRADUACION_DELTA_PP = 8;
    public const decimal OPTIMISTA_ESTUDIANTES_FACTOR = 1.20m;

    public const int PESIMISTA_RETENCION_DELTA_PP = -10;
    public const int PESIMISTA_GRADUACION_DELTA_PP = -15;
    public const decimal PESIMISTA_ESTUDIANTES_FACTOR = 0.80m;

    public static ValoresSugeridosDto CalcularDesdeHistorico(ConfiguracionRetencion h, string escenario)
    {
        ArgumentNullException.ThrowIfNull(h);

        return escenario switch
        {
            "Optimista" => Calcular(h, OPTIMISTA_RETENCION_DELTA_PP, OPTIMISTA_GRADUACION_DELTA_PP, OPTIMISTA_ESTUDIANTES_FACTOR),
            "Pesimista" => Calcular(h, PESIMISTA_RETENCION_DELTA_PP, PESIMISTA_GRADUACION_DELTA_PP, PESIMISTA_ESTUDIANTES_FACTOR),
            _ => throw new ArgumentException($"Escenario '{escenario}' no admite ajuste automatico. Use 'Optimista' o 'Pesimista'.", nameof(escenario))
        };
    }

    private static ValoresSugeridosDto Calcular(ConfiguracionRetencion h, int deltaRet, int deltaGrad, decimal factorEst)
    {
        // Los escenarios ajustan la META acumulada del histórico (input del usuario), no la tasa por ciclo.
        var retencion = Math.Round(Math.Clamp(h.MetaRetencionPorcentaje + deltaRet, 0m, 100m), 1);
        var graduacion = Math.Round(Math.Clamp(h.MetaGraduacionPorcentaje + deltaGrad, 0m, 100m), 1);
        var estP1 = Math.Round(h.EstudiantesPeriodo1 * factorEst, 0, MidpointRounding.AwayFromZero);
        var estP2 = Math.Round(h.EstudiantesPeriodo2 * factorEst, 0, MidpointRounding.AwayFromZero);

        return new ValoresSugeridosDto
        {
            Retencion = retencion,
            Graduacion = graduacion,
            EstP1 = estP1,
            EstP2 = estP2,
            ParP1 = h.ParalelosPeriodo1,
            ParP2 = h.ParalelosPeriodo2,
            Ciclos = h.TotalCiclos
        };
    }
}
