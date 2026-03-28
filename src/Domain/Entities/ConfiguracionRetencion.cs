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
        CarreraId = carreraId;
        EscenarioProyeccionId = escenarioProyeccionId;
        TotalCiclos = totalCiclos;
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
        ValidarPorcentaje(tasaRetencionPorcentaje, nameof(tasaRetencionPorcentaje));
        ValidarPorcentaje(tasaGraduacionPorcentaje, nameof(tasaGraduacionPorcentaje));

        TasaRetencionPorcentaje = decimal.Round(tasaRetencionPorcentaje, 4);
        TasaGraduacionPorcentaje = decimal.Round(tasaGraduacionPorcentaje, 4);
    }

    public void ActualizarBaseEstudiantes(decimal estudiantesPeriodo1, decimal estudiantesPeriodo2, int paralelosPeriodo1, int paralelosPeriodo2)
    {
        if (estudiantesPeriodo1 < 0 || estudiantesPeriodo2 < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(estudiantesPeriodo1), "Los estudiantes no pueden ser negativos.");
        }

        if (paralelosPeriodo1 < 0 || paralelosPeriodo2 < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(paralelosPeriodo1), "Los paralelos no pueden ser negativos.");
        }

        EstudiantesPeriodo1 = decimal.Round(estudiantesPeriodo1, 4);
        EstudiantesPeriodo2 = decimal.Round(estudiantesPeriodo2, 4);
        ParalelosPeriodo1 = paralelosPeriodo1;
        ParalelosPeriodo2 = paralelosPeriodo2;
    }

    private static void ValidarPorcentaje(decimal valor, string parametro)
    {
        if (valor < 0 || valor > 100)
        {
            throw new ArgumentOutOfRangeException(parametro, "El porcentaje debe estar entre 0 y 100.");
        }
    }
}
