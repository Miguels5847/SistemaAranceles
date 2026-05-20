using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public static class CalculadoraDepreciacion
{
    public static IReadOnlyList<CeldaDepreciacionDto> CalcularFila(
        IReadOnlyList<PeriodoDepreciacionDto> periodos,
        IReadOnlyDictionary<int, decimal> montosPorNumeroPeriodo,
        int vidaUtilAnios,
        decimal porcentajeResidual)
    {
        if (vidaUtilAnios <= 0)
            vidaUtilAnios = 1;

        if (porcentajeResidual < 0m)
            porcentajeResidual = 0m;

        if (porcentajeResidual >= 1m)
            porcentajeResidual = 0m;

        var acumulado = 0m;
        var celdas = new List<CeldaDepreciacionDto>();
        var inversionesActivas = new List<InversionDepreciable>();

        foreach (var periodo in periodos.OrderBy(p => p.NumeroPeriodo))
        {
            if (montosPorNumeroPeriodo.TryGetValue(periodo.NumeroPeriodo, out var monto) && monto > 0m)
            {
                inversionesActivas.Add(new InversionDepreciable(
                    periodo.NumeroPeriodo,
                    monto,
                    vidaUtilAnios,
                    porcentajeResidual));
            }

            var depreciacionPeriodo = inversionesActivas.Sum(i => i.CalcularDepreciacionParaPeriodo(periodo.NumeroPeriodo));
            depreciacionPeriodo = decimal.Round(depreciacionPeriodo, 2);
            acumulado = decimal.Round(acumulado + depreciacionPeriodo, 2);

            celdas.Add(new CeldaDepreciacionDto
            {
                Anio = periodo.Anio,
                Semestre = periodo.Semestre,
                NumeroPeriodo = periodo.NumeroPeriodo,
                DepreciacionPeriodo = depreciacionPeriodo,
                DepreciacionAcumulada = acumulado
            });
        }

        return celdas;
    }

    public static decimal CalcularValorResidual(decimal valor, decimal porcentajeResidual)
    {
        if (valor <= 0m)
            return 0m;

        if (porcentajeResidual < 0m || porcentajeResidual >= 1m)
            porcentajeResidual = 0m;

        return decimal.Round(valor * porcentajeResidual, 2);
    }

    private sealed class InversionDepreciable(
        int periodoInicio,
        decimal valor,
        int vidaUtilAnios,
        decimal porcentajeResidual)
    {
        private int VidaUtilAnios { get; } = vidaUtilAnios <= 0 ? 1 : vidaUtilAnios;
        private decimal BaseDepreciable { get; } = CalcularBaseDepreciable(valor, porcentajeResidual);
        private int PeriodoInicio { get; } = periodoInicio;
        private int PeriodosVidaUtil => VidaUtilAnios * 2;

        public decimal CalcularDepreciacionParaPeriodo(int numeroPeriodo)
        {
            if (numeroPeriodo < PeriodoInicio)
                return 0m;

            var periodoRelativo = numeroPeriodo - PeriodoInicio + 1;
            if (periodoRelativo > PeriodosVidaUtil)
                return 0m;

            var depreciacionAnual = BaseDepreciable / VidaUtilAnios;
            var depreciacionSemestral = depreciacionAnual / 2m;

            return periodoRelativo == 1
                ? depreciacionAnual
                : depreciacionSemestral;
        }

        private static decimal CalcularBaseDepreciable(decimal valor, decimal porcentajeResidual)
        {
            if (porcentajeResidual < 0m || porcentajeResidual >= 1m)
                porcentajeResidual = 0m;

            return valor * (1m - porcentajeResidual);
        }
    }
}
