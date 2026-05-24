using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Gasto diferido amortizable (KAN-31). Equivale a permisos legales (Municipal, Bomberos)
/// que se amortizan al 20% anual durante 5 años.
/// </summary>
public sealed class ActivoDiferido : EntidadDominioBase
{
    private ActivoDiferido() { }

    public ActivoDiferido(int carreraId, string nombreRubro, decimal valor, decimal tasaAmortizacionAnual = 0.20m)
    {
        CambiarCarrera(carreraId);
        CambiarNombreRubro(nombreRubro);
        CambiarValor(valor);
        CambiarTasa(tasaAmortizacionAnual);
    }

    public const decimal TasaPorDefecto = 0.20m;

    public int CarreraId { get; private set; }
    public string NombreRubro { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public decimal TasaAmortizacionAnual { get; private set; } = TasaPorDefecto;

    /// <summary>Cuota anual de amortización (Valor × Tasa).</summary>
    public decimal CuotaAnual() => decimal.Round(Valor * TasaAmortizacionAnual, 2);

    public void CambiarCarrera(int carreraId)
        => CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");

    public void CambiarNombreRubro(string nombre)
        => NombreRubro = GuardiaDominio.Requerido(nombre, "Nombre del activo diferido", 150);

    public void CambiarValor(decimal valor)
        => Valor = GuardiaDominio.DecimalNoNegativo(valor, "Valor del activo diferido", 2);

    public void CambiarTasa(decimal tasa)
    {
        if (tasa < 0m || tasa > 1m)
            throw new DominioException("La tasa de amortización debe estar entre 0 y 1 (ej. 0.20 = 20%).");
        TasaAmortizacionAnual = decimal.Round(tasa, 4);
    }
}
