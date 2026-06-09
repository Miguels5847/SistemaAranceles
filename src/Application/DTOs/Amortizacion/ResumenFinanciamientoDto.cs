using System.Globalization;

namespace SistemaAranceles.Application.DTOs.Amortizacion;

public sealed class FilaAmortizacionDto
{
    public int NumeroPago { get; init; }
    public decimal SaldoCapital { get; init; }
    public decimal Interes { get; init; }
    public decimal AmortizacionCapital { get; init; }
    public decimal Cuota { get; init; }
    public decimal InteresAcumulado { get; init; }

    public string SaldoCapitalDisplay => FormatoAmortizacion.Moneda(SaldoCapital);
    public string InteresDisplay => FormatoAmortizacion.Moneda(Interes);
    public string AmortizacionCapitalDisplay => FormatoAmortizacion.Moneda(AmortizacionCapital);
    public string CuotaDisplay => FormatoAmortizacion.Moneda(Cuota);
    public string InteresAcumuladoDisplay => FormatoAmortizacion.Moneda(InteresAcumulado);
}

public sealed class ResumenFinanciamientoDto
{
    public decimal TotalInversion { get; init; }
    public decimal PorcentajePropio { get; init; }
    public decimal PorcentajePrestamo { get; init; }
    public decimal PorcentajeConvenio { get; init; }
    public decimal MontoPropio { get; init; }
    public decimal MontoPrestamo { get; init; }
    public decimal MontoConvenio { get; init; }
    public string? NombreEntidadPrestamo { get; init; }
    public string? NombreEntidadConvenio { get; init; }
    public decimal TasaInteresAnual { get; init; }
    public decimal TasaInteresMensual { get; init; }
    public int PlazoMeses { get; init; }
    public decimal CuotaMensual { get; init; }
    public decimal TotalIntereses { get; init; }
    public IReadOnlyList<FilaAmortizacionDto> TablaAmortizacion { get; init; } = [];

    public string TotalInversionDisplay => FormatoAmortizacion.Moneda(TotalInversion);
    public string MontoPropioDisplay => FormatoAmortizacion.Moneda(MontoPropio);
    public string MontoPrestamoDisplay => FormatoAmortizacion.Moneda(MontoPrestamo);
    public string MontoConvenioDisplay => FormatoAmortizacion.Moneda(MontoConvenio);
    public string CuotaMensualDisplay => FormatoAmortizacion.Moneda(CuotaMensual);
    public string TotalInteresesDisplay => FormatoAmortizacion.Moneda(TotalIntereses);
    public string PorcentajePropioDisplay => FormatoAmortizacion.Porcentaje(PorcentajePropio);
    public string PorcentajePrestamoDisplay => FormatoAmortizacion.Porcentaje(PorcentajePrestamo);
    public string PorcentajeConvenioDisplay => FormatoAmortizacion.Porcentaje(PorcentajeConvenio);
    public string TasaInteresAnualDisplay => FormatoAmortizacion.Porcentaje(TasaInteresAnual);
    public string TasaInteresMensualDisplay => FormatoAmortizacion.Porcentaje(TasaInteresMensual);
}

internal static class FormatoAmortizacion
{
    private static readonly CultureInfo Cultura = new("es-EC");

    public static string Moneda(decimal valor)
        => valor == 0m ? "$ -" : $"$ {valor.ToString("N2", Cultura)}";

    public static string Porcentaje(decimal valor)
        => valor.ToString("0.####", CultureInfo.InvariantCulture) + "%";
}
