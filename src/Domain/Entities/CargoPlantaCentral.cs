using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Representa un cargo de la planta central de la universidad con su sueldo mensual total.
/// </summary>
public sealed class CargoPlantaCentral : EntidadDominioBase
{
    private CargoPlantaCentral()
    {
    }

    public CargoPlantaCentral(string nombreCargo, decimal sueldoMensualTotal)
    {
        CambiarNombre(nombreCargo);
        CambiarSueldoMensual(sueldoMensualTotal);
    }

    public string NombreCargo { get; private set; } = string.Empty;
    public decimal SueldoMensualTotal { get; private set; }

    public void CambiarNombre(string nombreCargo)
    {
        NombreCargo = GuardiaDominio.Requerido(nombreCargo, "Nombre del cargo de planta central", 120);
    }

    public void CambiarSueldoMensual(decimal sueldoMensual)
    {
        SueldoMensualTotal = GuardiaDominio.DecimalNoNegativo(sueldoMensual, "Sueldo mensual total", 2);
    }
}
