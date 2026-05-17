using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Domain.Constantes;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

public static class CalculoCargosFacultad
{
    public static bool EsTiempoParcial(CargoFacultad cargo)
        => cargo.TipoContrato == TipoContrato.TiempoParcial;

    /// <summary>
    /// CU-SP-02 RN-79b: TP se paga por hora sin beneficios sociales (servicios profesionales).
    /// Costo semestral = TarifaHora × HorasAsignadasSemana × SemanasPorMes × 6.
    /// </summary>
    public static decimal CalcularCostoSemestralTiempoParcial(
        decimal tarifaHora,
        decimal horasAsignadasSemana,
        decimal personas,
        decimal pesoProporcional,
        decimal factorInflacion)
        => Math.Round(
            tarifaHora * horasAsignadasSemana * ConstantesDocentes.SemanasPorMes * 6m
            * personas * pesoProporcional * NormalizarFactorInflacion(factorInflacion),
            2);

    public static decimal CalcularPeso(
        CargoFacultad cargo,
        decimal estudiantesCarreraPeriodo,
        decimal estudiantesUnidadAcademica)
    {
        if (EsPesoFijo(cargo))
            return 1m;

        var denominador = estudiantesUnidadAcademica + estudiantesCarreraPeriodo;
        if (denominador <= 0m)
            return 0m;

        return Math.Round(estudiantesCarreraPeriodo / denominador, 4);
    }

    public static decimal CalcularDecimoTerceroSemestral(decimal sueldoBaseMensual)
        => Math.Round(sueldoBaseMensual / 2m, 2);

    public static decimal CalcularDecimoCuartoSemestral(decimal valorBaseDecimoCuartoSemestral)
        => Math.Round(valorBaseDecimoCuartoSemestral / 2m, 2);

    public static decimal CalcularVacacionesSemestral(decimal sueldoBaseMensual)
        => Math.Round(sueldoBaseMensual / 4m, 2);

    public static decimal CalcularFondoReservaMensual(decimal sueldoBaseMensual, decimal tasaFondoReserva)
        => Math.Round(sueldoBaseMensual * tasaFondoReserva, 2);

    public static decimal CalcularAportePatronalMensual(decimal sueldoBaseMensual, decimal tasaAportePatronal)
        => Math.Round(sueldoBaseMensual * tasaAportePatronal, 2);

    public static decimal CalcularCostoBaseSemestral(
        decimal sueldoBaseMensual,
        decimal fondoReservaMensual,
        decimal aportePatronalMensual,
        decimal decimoTerceroSemestral,
        decimal decimoCuartoSemestral,
        decimal vacacionesSemestral)
        => Math.Round(
            ((sueldoBaseMensual + fondoReservaMensual + aportePatronalMensual) * 6m)
            + decimoTerceroSemestral
            + decimoCuartoSemestral
            + vacacionesSemestral,
            2);

    public static decimal CalcularCostoTotalSemestral(
        decimal costoBaseSemestral,
        decimal pesoProporcional,
        decimal cantidadPersonas,
        decimal factorInflacion)
        => Math.Round(costoBaseSemestral * pesoProporcional * cantidadPersonas * NormalizarFactorInflacion(factorInflacion), 2);

    public static decimal CalcularFactorInflacionEncadenado(
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
                .OrderBy(r => r.TipoFuente.Equals("estimacion", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .FirstOrDefault();
            var pct = registro?.PorcentajeInflacion ?? 0m;

            if (anio == anioPeriodo)
            {
                if (numeroPeriodo >= 2)
                    factor *= 1m + (pct / 100m);
            }
            else
            {
                factor *= 1m + (pct / 100m);
            }
        }

        if (factor < 1m) factor = 1m;
        return Math.Round(factor, 6);
    }

    public static decimal NormalizarFactorInflacion(decimal factorInflacion)
        => factorInflacion < 1m ? 1m : factorInflacion;

    public static bool EsPesoFijo(CargoFacultad cargo)
    {
        if (cargo.EsCargoDocente)
            return true;

        if (cargo.NombreCargo.Equals("Director de Carrera", StringComparison.OrdinalIgnoreCase))
            return true;

        return cargo.TipoCargo.Contains("dedicacion exclusiva", StringComparison.OrdinalIgnoreCase);
    }
}