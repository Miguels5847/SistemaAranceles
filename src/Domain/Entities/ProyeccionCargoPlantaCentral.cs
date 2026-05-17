using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Representa el costo atribuido a una carrera por un cargo de planta central en un período académico.
/// La distribución se calcula proporcionalmente según el número de estudiantes de la carrera.
/// </summary>
public sealed class ProyeccionCargoPlantaCentral : EntidadDominioBase
{
    private ProyeccionCargoPlantaCentral()
    {
    }

    public ProyeccionCargoPlantaCentral(
        int cargoPlantaCentralId,
        int carreraId,
        int periodoAcademicoId,
        decimal proporcionAsignacion,
        decimal costoTotalSemestre)
    {
        CambiarCargoPlantaCentral(cargoPlantaCentralId);
        CambiarCarrera(carreraId);
        CambiarPeriodoAcademico(periodoAcademicoId);
        CambiarProporcionAsignacion(proporcionAsignacion);
        RecalcularCosto(costoTotalSemestre);
    }

    public int CargoPlantaCentralId { get; private set; }
    public int CarreraId { get; private set; }
    public int PeriodoAcademicoId { get; private set; }
    public decimal ProporcionAsignacion { get; private set; }
    public decimal CostoTotalSemestre { get; private set; }

    public void CambiarCargoPlantaCentral(int cargoPlantaCentralId)
    {
        CargoPlantaCentralId = GuardiaDominio.EnteroPositivo(cargoPlantaCentralId, "Cargo de planta central");
    }

    public void CambiarCarrera(int carreraId)
    {
        CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");
    }

    public void CambiarPeriodoAcademico(int periodoAcademicoId)
    {
        PeriodoAcademicoId = GuardiaDominio.EnteroPositivo(periodoAcademicoId, "Período académico");
    }

    public void CambiarProporcionAsignacion(decimal proporcion)
    {
        ProporcionAsignacion = GuardiaDominio.DecimalNoNegativo(proporcion, "Proporción de asignación", 4);
    }

    public void RecalcularCosto(decimal costoTotalSemestre)
    {
        CostoTotalSemestre = GuardiaDominio.DecimalNoNegativo(costoTotalSemestre, "Costo total semestral", 2);
    }
}
