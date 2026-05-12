using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Entities;

public sealed class CargoFacultad : EntidadDominioBase
{
    private CargoFacultad()
    {
    }

    public CargoFacultad(
        int carreraId,
        string nombreCargo,
        string tipoCargo,
        decimal sueldoBaseMensual,
        bool esCargoDocente,
        decimal cantidadDefault = 1m,
        TipoContrato tipoContrato = TipoContrato.Administrativo,
        decimal tarifaHora = 0m)
    {
        CambiarCarrera(carreraId);
        CambiarNombreCargo(nombreCargo);
        CambiarTipoCargo(tipoCargo);
        CambiarSueldoBase(sueldoBaseMensual);
        EsCargoDocente = esCargoDocente;
        CambiarCantidadDefault(cantidadDefault);
        CambiarTipoContrato(tipoContrato);
        CambiarTarifaHora(tarifaHora);
    }

    public int CarreraId { get; private set; }
    public string NombreCargo { get; private set; } = string.Empty;
    public string TipoCargo { get; private set; } = string.Empty;
    public decimal SueldoBaseMensual { get; private set; }
    public bool EsCargoDocente { get; private set; }
    public decimal CantidadDefault { get; private set; } = 1m;
    public TipoContrato TipoContrato { get; private set; } = TipoContrato.Administrativo;
    public decimal TarifaHora { get; private set; }

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
        if (string.IsNullOrWhiteSpace(tipoCargo))
        {
            TipoCargo = "No especificado";
            return;
        }

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

    public void CambiarCantidadDefault(decimal cantidad)
    {
        CantidadDefault = GuardiaDominio.DecimalNoNegativo(cantidad, "Cantidad default", 4);
    }

    public void CambiarTipoContrato(TipoContrato tipo)
    {
        TipoContrato = tipo;
    }

    public void CambiarTarifaHora(decimal tarifa)
    {
        TarifaHora = GuardiaDominio.DecimalNoNegativo(tarifa, "Tarifa hora", 4);
    }
}
