using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class CargoFacultad : EntidadDominioBase
{
    private CargoFacultad()
    {
    }

    public CargoFacultad(int carreraId, string nombreCargo, string tipoCargo, decimal sueldoBaseMensual, bool esCargoDocente)
    {
        CambiarCarrera(carreraId);
        CambiarNombreCargo(nombreCargo);
        CambiarTipoCargo(tipoCargo);
        CambiarSueldoBase(sueldoBaseMensual);
        EsCargoDocente = esCargoDocente;
    }

    public int CarreraId { get; private set; }
    public string NombreCargo { get; private set; } = string.Empty;
    public string TipoCargo { get; private set; } = string.Empty;
    public decimal SueldoBaseMensual { get; private set; }
    public bool EsCargoDocente { get; private set; }

    public void CambiarCarrera(int carreraId)
    {
        CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");
    }

    public void CambiarNombreCargo(string nombreCargo)
    {
        NombreCargo = GuardiaDominio.Requerido(nombreCargo, "Nombre del cargo", 150);
    }

    public void CambiarTipoCargo(string tipoCargo)
    {
        TipoCargo = GuardiaDominio.Requerido(tipoCargo, "Tipo de cargo", 80);
    }

    public void CambiarSueldoBase(decimal sueldoBaseMensual)
    {
        SueldoBaseMensual = GuardiaDominio.DecimalNoNegativo(sueldoBaseMensual, "Sueldo base mensual", 2);
    }

    public void CambiarEsCargoDocente(bool esCargoDocente)
    {
        EsCargoDocente = esCargoDocente;
    }
}
