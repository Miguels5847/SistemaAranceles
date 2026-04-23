using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class DetalleSimulacionRetencion : EntidadDominioBase
{
    private DetalleSimulacionRetencion()
    {
    }

    public DetalleSimulacionRetencion(
        int simulacionRetencionId,
        int ciclo,
        decimal estudiantesInicio,
        decimal estudiantesRetenidos,
        decimal estudiantesReprobados,
        decimal estudiantesGraduados,
        decimal costoMatriculaProyectado,
        int anioAcademico)
    {
        SimulacionRetencionId = GuardiaDominio.EnteroNoNegativo(simulacionRetencionId, "Simulacion de retencion");
        Ciclo = GuardiaDominio.EnteroPositivo(ciclo, "Ciclo");
        EstudiantesInicio = GuardiaDominio.DecimalNoNegativo(estudiantesInicio, "Estudiantes inicio", 4);
        EstudiantesRetenidos = GuardiaDominio.DecimalNoNegativo(estudiantesRetenidos, "Estudiantes retenidos", 4);
        EstudiantesReprobados = GuardiaDominio.DecimalNoNegativo(estudiantesReprobados, "Estudiantes reprobados", 4);
        EstudiantesGraduados = GuardiaDominio.DecimalNoNegativo(estudiantesGraduados, "Estudiantes graduados", 4);
        CostoMatriculaProyectado = GuardiaDominio.DecimalNoNegativo(costoMatriculaProyectado, "Costo matricula proyectado", 2);
        AnioAcademico = GuardiaDominio.EnteroPositivo(anioAcademico, "Año academico");
    }

    public int SimulacionRetencionId { get; private set; }
    public int Ciclo { get; private set; }
    public decimal EstudiantesInicio { get; private set; }
    public decimal EstudiantesRetenidos { get; private set; }
    public decimal EstudiantesReprobados { get; private set; }
    public decimal EstudiantesGraduados { get; private set; }
    public decimal CostoMatriculaProyectado { get; private set; }
    public int AnioAcademico { get; private set; }

    public decimal ObtenerTasaRetencionCiclo()
        => EstudiantesInicio <= 0 ? 0 : decimal.Round((EstudiantesRetenidos / EstudiantesInicio) * 100m, 4);

    public decimal ObtenerTasaGraduacionCiclo()
        => EstudiantesInicio <= 0 ? 0 : decimal.Round((EstudiantesGraduados / EstudiantesInicio) * 100m, 4);
}
