namespace SistemaAranceles.Presentation.State;

public sealed class FactorImprevistoCostosGastosState
{
    public const decimal FactorPorDefecto = 1.05m;

    public decimal FactorImprevisto { get; private set; } = FactorPorDefecto;

    public void Establecer(decimal factorImprevisto)
    {
        if (factorImprevisto <= 0m)
            return;

        FactorImprevisto = decimal.Round(factorImprevisto, 4);
    }
}
