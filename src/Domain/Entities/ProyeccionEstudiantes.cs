using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class ProyeccionEstudiantes : EntidadDominioBase
{
    private ProyeccionEstudiantes()
    {
    }

    public ProyeccionEstudiantes(int carreraId, int escenarioProyeccionId, int anioBase, int semanasPorSemestre)
    {
        CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");
        EscenarioProyeccionId = GuardiaDominio.EnteroPositivo(escenarioProyeccionId, "Escenario de proyeccion");
        AnioBase = ValidarAnio(anioBase);
        SemanasPorSemestre = ValidarSemanas(semanasPorSemestre);
    }

    public int CarreraId { get; private set; }
    public int EscenarioProyeccionId { get; private set; }
    public int AnioBase { get; private set; }
    public int SemanasPorSemestre { get; private set; }

    public void ActualizarAnioBase(int anioBase)
    {
        AnioBase = ValidarAnio(anioBase);
    }

    public void ActualizarSemanas(int semanasPorSemestre)
    {
        SemanasPorSemestre = ValidarSemanas(semanasPorSemestre);
    }

    private static int ValidarAnio(int anio)
    {
        if (anio < 2012 || anio > 2050)
            throw new DominioException("El año base debe estar entre 2012 y 2050.");
        return anio;
    }

    private static int ValidarSemanas(int semanas)
    {
        if (semanas < 8 || semanas > 30)
            throw new DominioException("Las semanas por semestre deben estar entre 8 y 30.");
        return semanas;
    }
}
