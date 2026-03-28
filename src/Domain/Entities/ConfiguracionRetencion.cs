using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class ConfiguracionRetencion : EntidadDominioBase
{
    private ConfiguracionRetencion()
    {
    }

    public ConfiguracionRetencion(
        int carreraId,
        int escenarioProyeccionId,
        int totalCiclos,
        decimal tasaRetencionPorcentaje,
        decimal tasaGraduacionPorcentaje)
    {
        CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");
        EscenarioProyeccionId = GuardiaDominio.EnteroPositivo(escenarioProyeccionId, "Escenario de proyeccion");
        TotalCiclos = GuardiaDominio.EnteroPositivo(totalCiclos, "Total de ciclos");
        ActualizarTasas(tasaRetencionPorcentaje, tasaGraduacionPorcentaje);
    }

    public int CarreraId { get; private set; }
    public int EscenarioProyeccionId { get; private set; }
    public int TotalCiclos { get; private set; }
    public decimal TasaRetencionPorcentaje { get; private set; }
    public decimal TasaGraduacionPorcentaje { get; private set; }

    public decimal EstudiantesPeriodo1 { get; private set; }
    public decimal EstudiantesPeriodo2 { get; private set; }
    public int ParalelosPeriodo1 { get; private set; }
    public int ParalelosPeriodo2 { get; private set; }

    public void ActualizarTasas(decimal tasaRetencionPorcentaje, decimal tasaGraduacionPorcentaje)
    {
        TasaRetencionPorcentaje = GuardiaDominio.Porcentaje(tasaRetencionPorcentaje, "Tasa de retencion");
        TasaGraduacionPorcentaje = GuardiaDominio.Porcentaje(tasaGraduacionPorcentaje, "Tasa de graduacion");
    }

    public void ActualizarBaseEstudiantes(decimal estudiantesPeriodo1, decimal estudiantesPeriodo2, int paralelosPeriodo1, int paralelosPeriodo2)
    {
        EstudiantesPeriodo1 = GuardiaDominio.DecimalNoNegativo(estudiantesPeriodo1, "Estudiantes periodo 1", 4);
        EstudiantesPeriodo2 = GuardiaDominio.DecimalNoNegativo(estudiantesPeriodo2, "Estudiantes periodo 2", 4);
        ParalelosPeriodo1 = GuardiaDominio.EnteroNoNegativo(paralelosPeriodo1, "Paralelos periodo 1");
        ParalelosPeriodo2 = GuardiaDominio.EnteroNoNegativo(paralelosPeriodo2, "Paralelos periodo 2");
    }
}
