using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class ProyeccionCargoFacultad : EntidadDominioBase
{
    private ProyeccionCargoFacultad()
    {
    }

    public ProyeccionCargoFacultad(
        int cargoFacultadId,
        int periodoAcademicoId,
        decimal cantidadPersonas,
        decimal factorPonderacion,
        decimal factorInflacion,
        decimal costoTotalSemestre)
    {
        CambiarCargoFacultad(cargoFacultadId);
        CambiarPeriodoAcademico(periodoAcademicoId);
        CambiarCantidadPersonas(cantidadPersonas);
        CambiarFactorPonderacion(factorPonderacion);
        CambiarFactorInflacion(factorInflacion);
        RecalcularCosto(costoTotalSemestre);
    }

    public int CargoFacultadId { get; private set; }
    public int PeriodoAcademicoId { get; private set; }
    public decimal CantidadPersonas { get; private set; }
    public decimal FactorPonderacion { get; private set; }
    public decimal FactorInflacion { get; private set; }
    public decimal CostoTotalSemestre { get; private set; }

    public void CambiarCargoFacultad(int cargoFacultadId)
    {
        CargoFacultadId = GuardiaDominio.EnteroPositivo(cargoFacultadId, "Cargo de facultad");
    }

    public void CambiarPeriodoAcademico(int periodoAcademicoId)
    {
        PeriodoAcademicoId = GuardiaDominio.EnteroPositivo(periodoAcademicoId, "Periodo academico");
    }

    public void CambiarCantidadPersonas(decimal cantidadPersonas)
    {
        CantidadPersonas = GuardiaDominio.DecimalNoNegativo(cantidadPersonas, "Cantidad de personas", 4);
    }

    public void CambiarFactorPonderacion(decimal factorPonderacion)
    {
        FactorPonderacion = GuardiaDominio.DecimalNoNegativo(factorPonderacion, "Factor de ponderacion", 4);
    }

    public void CambiarFactorInflacion(decimal factorInflacion)
    {
        FactorInflacion = GuardiaDominio.DecimalNoNegativo(factorInflacion, "Factor de inflacion", 6);
    }

    public void RecalcularCosto(decimal costoTotalSemestre)
    {
        CostoTotalSemestre = GuardiaDominio.DecimalNoNegativo(costoTotalSemestre, "Costo total semestre", 2);
    }
}
