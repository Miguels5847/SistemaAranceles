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
        EscenarioProyeccionId = GuardiaDominio.EnteroPositivo(escenarioProyeccionId, "Escenario de proyección");
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
        TasaRetencionPorcentaje = GuardiaDominio.Porcentaje(tasaRetencionPorcentaje, "Tasa de retención");
        TasaGraduacionPorcentaje = GuardiaDominio.Porcentaje(tasaGraduacionPorcentaje, "Tasa de graduación");
    }

    public void ActualizarBaseEstudiantes(decimal estudiantesPeriodo1, decimal estudiantesPeriodo2, int paralelosPeriodo1, int paralelosPeriodo2)
    {
        EstudiantesPeriodo1 = GuardiaDominio.DecimalNoNegativo(estudiantesPeriodo1, "Estudiantes período 1", 4);
        EstudiantesPeriodo2 = GuardiaDominio.DecimalNoNegativo(estudiantesPeriodo2, "Estudiantes período 2", 4);
        ParalelosPeriodo1 = GuardiaDominio.EnteroNoNegativo(paralelosPeriodo1, "Paralelos período 1");
        ParalelosPeriodo2 = GuardiaDominio.EnteroNoNegativo(paralelosPeriodo2, "Paralelos período 2");
    }
}
