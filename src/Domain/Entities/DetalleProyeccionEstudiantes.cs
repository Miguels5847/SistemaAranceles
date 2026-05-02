using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class DetalleProyeccionEstudiantes : EntidadDominioBase
{
    private DetalleProyeccionEstudiantes()
    {
    }

    public DetalleProyeccionEstudiantes(
        int proyeccionEstudiantesId,
        int periodoAcademicoId,
        int numeroCiclo,
        int cantidadParalelos,
        decimal totalEstudiantes)
    {
        ProyeccionEstudiantesId = GuardiaDominio.EnteroPositivo(proyeccionEstudiantesId, "Proyeccion de estudiantes");
        PeriodoAcademicoId = GuardiaDominio.EnteroPositivo(periodoAcademicoId, "Periodo academico");
        NumeroCiclo = GuardiaDominio.EnteroPositivo(numeroCiclo, "Numero de ciclo");
        CantidadParalelos = GuardiaDominio.EnteroPositivo(cantidadParalelos, "Cantidad de paralelos");
        TotalEstudiantes = GuardiaDominio.DecimalNoNegativo(totalEstudiantes, "Total de estudiantes", 4);
    }

    public int ProyeccionEstudiantesId { get; private set; }
    public int PeriodoAcademicoId { get; private set; }
    public int NumeroCiclo { get; private set; }
    public int CantidadParalelos { get; private set; }
    public decimal TotalEstudiantes { get; private set; }
}
