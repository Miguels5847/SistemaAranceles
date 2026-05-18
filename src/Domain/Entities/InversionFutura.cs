using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Cantidad manual proyectada para inversiones futuras de un activo fijo.
/// Los activos derivados por estudiantes/docentes se calculan al leer y no se duplican aquí.
/// </summary>
public sealed class InversionFutura : EntidadDominioBase
{
    private InversionFutura()
    {
    }

    public InversionFutura(int activoFijoId, int anio, int semestre, decimal cantidadProyectada)
    {
        CambiarActivoFijo(activoFijoId);
        CambiarPeriodo(anio, semestre);
        CambiarCantidad(cantidadProyectada);
    }

    public int ActivoFijoId { get; private set; }
    public int Anio { get; private set; }
    public int Semestre { get; private set; }
    public decimal CantidadProyectada { get; private set; }

    public decimal CalcularMontoNominal(decimal valorUnitario, decimal factorInflacion)
    {
        var factor = factorInflacion < 1m ? 1m : factorInflacion;
        return decimal.Round(CantidadProyectada * valorUnitario * factor, 2);
    }

    public void CambiarActivoFijo(int activoFijoId)
    {
        ActivoFijoId = GuardiaDominio.EnteroPositivo(activoFijoId, "Activo fijo");
    }

    public void CambiarPeriodo(int anio, int semestre)
    {
        if (anio < 2000 || anio > 2200)
        {
            throw new DominioException("Año de inversión futura fuera del rango permitido.");
        }

        if (semestre is not 1 and not 2)
        {
            throw new DominioException("Semestre de inversión futura debe ser 1 o 2.");
        }

        Anio = anio;
        Semestre = semestre;
    }

    public void CambiarCantidad(decimal cantidadProyectada)
    {
        CantidadProyectada = GuardiaDominio.DecimalNoNegativo(cantidadProyectada, "Cantidad proyectada", 4);
    }
}
