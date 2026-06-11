using SistemaAranceles.Application.DTOs.Amortizacion;

namespace SistemaAranceles.Application.UseCases.Amortizacion;

/// <summary>
/// Agrega la tabla francesa MENSUAL del préstamo (KAN-44B) a períodos académicos
/// semestrales: el período global n agrupa los pagos (n-1)*6+1 .. n*6.
/// Alimenta el gasto financiero de Costos y Gastos, el pago del crédito del
/// Flujo de Fondos y el saldo del préstamo del Balance Proyectado (KAN-48).
/// </summary>
public static class AmortizacionPorPeriodo
{
    public const int MesesPorPeriodo = 6;

    /// <summary>Interés pagado durante el período semestral (hoja "Amort. prestamo").</summary>
    public static decimal InteresDelPeriodo(ResumenFinanciamientoDto financiamiento, int numeroPeriodoGlobal)
        => SumarRango(financiamiento, numeroPeriodoGlobal, f => f.Interes);

    /// <summary>Capital amortizado durante el período semestral ("PAGO DEL CREDITO" del flujo).</summary>
    public static decimal CapitalDelPeriodo(ResumenFinanciamientoDto financiamiento, int numeroPeriodoGlobal)
        => SumarRango(financiamiento, numeroPeriodoGlobal, f => f.AmortizacionCapital);

    /// <summary>Saldo del préstamo al cierre del período semestral ("Prestamos x pagar" del balance).</summary>
    public static decimal SaldoAlCierreDelPeriodo(ResumenFinanciamientoDto financiamiento, int numeroPeriodoGlobal)
    {
        var tabla = financiamiento.TablaAmortizacion;
        if (tabla.Count == 0 || numeroPeriodoGlobal <= 0)
            return tabla.Count == 0 ? 0m : financiamiento.MontoPrestamo;

        var ultimoPagoDelPeriodo = numeroPeriodoGlobal * MesesPorPeriodo;
        if (ultimoPagoDelPeriodo >= tabla.Count)
            return 0m;

        // SaldoCapital de cada fila es el saldo ANTES de ese pago; el saldo al cierre
        // del período es el saldo antes del primer pago del período siguiente.
        return tabla[ultimoPagoDelPeriodo].SaldoCapital;
    }

    private static decimal SumarRango(
        ResumenFinanciamientoDto financiamiento,
        int numeroPeriodoGlobal,
        Func<FilaAmortizacionDto, decimal> selector)
    {
        if (numeroPeriodoGlobal <= 0 || financiamiento.TablaAmortizacion.Count == 0)
            return 0m;

        var desde = (numeroPeriodoGlobal - 1) * MesesPorPeriodo + 1;
        var hasta = numeroPeriodoGlobal * MesesPorPeriodo;
        return decimal.Round(
            financiamiento.TablaAmortizacion
                .Where(f => f.NumeroPago >= desde && f.NumeroPago <= hasta)
                .Sum(selector),
            2);
    }
}
