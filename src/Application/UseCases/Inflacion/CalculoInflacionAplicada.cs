using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.Inflacion;

public static class CalculoInflacionAplicada
{
    public static decimal CalcularFactorPeriodo(
        IReadOnlyList<InflacionAnual> registros,
        int anioBase,
        int anioPeriodo,
        int numeroPeriodo)
    {
        if (anioPeriodo < anioBase)
            return 1m;

        decimal factor = 1m;

        for (var anio = anioBase; anio <= anioPeriodo; anio++)
        {
            var registro = registros
                .Where(r => r.Anio == anio)
                .OrderBy(r => r.TipoFuente)
                .FirstOrDefault();

            var porcentaje = registro?.PorcentajeInflacion ?? 0m;
            var factorAnual = 1m + porcentaje / 100m;
            if (factorAnual < 1m)
                factorAnual = 1m;

            if (anio < anioPeriodo || numeroPeriodo >= 2)
                factor *= factorAnual;
        }

        return Math.Round(factor < 1m ? 1m : factor, 6);
    }
}
