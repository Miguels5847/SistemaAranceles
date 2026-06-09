using SistemaAranceles.Application.DTOs.Amortizacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.InversionInicial;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.Amortizacion;

/// <summary>
/// Financiamiento de la inversión inicial en 3 fuentes (KAN-44B): Recursos Propios,
/// Préstamo Bancario y Convenio Institucional. Genera la tabla de amortización francesa
/// (cuota fija PMT) del préstamo, replicando la hoja "Amort. prestamo" del Excel.
/// </summary>
public sealed class ObtenerResumenAmortizacionQuery(
    ObtenerInversionInicialTotalQuery inversionInicialQuery,
    IRepositorioDatosInstitucionales repositorioDatos)
{
    public async Task<ResumenFinanciamientoDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId = null,
        CancellationToken ct = default)
    {
        var inversion = await inversionInicialQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);

        var porcentajePrestamo = datos?.PorcentajeFinanciadoPrestamo ?? 0m;
        var porcentajeConvenio = datos?.PorcentajeFinanciadoConvenio ?? 0m;
        var tasaAnual = datos?.TasaInteresAnualPrestamo ?? DatosInstitucionales.TasaInteresAnualPrestamoPorDefecto;
        var plazoMeses = datos?.PlazoPrestamoMeses ?? DatosInstitucionales.PlazoPrestamoMesesPorDefecto;

        return ConstruirResumen(
            inversion.TotalInversion,
            porcentajePrestamo,
            porcentajeConvenio,
            datos?.NombreEntidadPrestamo,
            datos?.NombreEntidadConvenio,
            tasaAnual,
            plazoMeses);
    }

    /// <summary>Cálculo puro en memoria; lo reutiliza el ViewModel para recalcular en vivo al cambiar %.</summary>
    public static ResumenFinanciamientoDto ConstruirResumen(
        decimal totalInversion,
        decimal porcentajePrestamo,
        decimal porcentajeConvenio,
        string? nombrePrestamo,
        string? nombreConvenio,
        decimal tasaAnual,
        int plazoMeses)
    {
        var montoPrestamo = decimal.Round(totalInversion * porcentajePrestamo / 100m, 2);
        var montoConvenio = decimal.Round(totalInversion * porcentajeConvenio / 100m, 2);
        var montoPropio = decimal.Round(totalInversion - montoPrestamo - montoConvenio, 2);

        var tasaMensual = tasaAnual / 100m / 12m;
        var (cuota, tabla) = GenerarTablaFrancesa(montoPrestamo, tasaMensual, plazoMeses);

        return new ResumenFinanciamientoDto
        {
            TotalInversion = totalInversion,
            PorcentajePropio = 100m - porcentajePrestamo - porcentajeConvenio,
            PorcentajePrestamo = porcentajePrestamo,
            PorcentajeConvenio = porcentajeConvenio,
            MontoPropio = montoPropio,
            MontoPrestamo = montoPrestamo,
            MontoConvenio = montoConvenio,
            NombreEntidadPrestamo = nombrePrestamo,
            NombreEntidadConvenio = nombreConvenio,
            TasaInteresAnual = tasaAnual,
            TasaInteresMensual = decimal.Round(tasaMensual * 100m, 4),
            PlazoMeses = plazoMeses,
            CuotaMensual = cuota,
            TotalIntereses = tabla.Sum(f => f.Interes),
            TablaAmortizacion = tabla,
        };
    }

    private static (decimal Cuota, IReadOnlyList<FilaAmortizacionDto> Tabla) GenerarTablaFrancesa(
        decimal capital,
        decimal tasaMensual,
        int plazoMeses)
    {
        if (capital <= 0m || plazoMeses <= 0)
            return (0m, []);

        decimal cuota;
        if (tasaMensual <= 0m)
        {
            cuota = decimal.Round(capital / plazoMeses, 2);
        }
        else
        {
            var factor = (double)(1m + tasaMensual);
            var potencia = (decimal)Math.Pow(factor, plazoMeses);
            cuota = decimal.Round(capital * (tasaMensual * potencia) / (potencia - 1m), 2);
        }

        var tabla = new List<FilaAmortizacionDto>(plazoMeses);
        var saldo = capital;
        var interesAcumulado = 0m;

        for (var pago = 1; pago <= plazoMeses; pago++)
        {
            var interes = decimal.Round(saldo * tasaMensual, 2);
            var amortizacion = pago == plazoMeses
                ? saldo
                : decimal.Round(cuota - interes, 2);
            var cuotaFila = pago == plazoMeses ? amortizacion + interes : cuota;
            interesAcumulado += interes;

            tabla.Add(new FilaAmortizacionDto
            {
                NumeroPago = pago,
                SaldoCapital = saldo,
                Interes = interes,
                AmortizacionCapital = amortizacion,
                Cuota = cuotaFila,
                InteresAcumulado = interesAcumulado,
            });

            saldo = decimal.Round(saldo - amortizacion, 2);
        }

        return (cuota, tabla);
    }
}
