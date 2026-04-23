using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

internal static class MotorSimulacionRetencion
{
    public static (IReadOnlyList<DetalleSimulacionRetencion> Detalles, decimal TotalRetenidos, decimal TotalGraduados)
        Ejecutar(ConfiguracionRetencion configuracion, int cohorteAnio)
    {
        var detalles = new List<DetalleSimulacionRetencion>(configuracion.TotalCiclos);
        var mitadCiclos = configuracion.TotalCiclos / 2;
        var tasaRetencion = configuracion.TasaRetencionPorcentaje / 100m;
        var tasaGraduacion = configuracion.TasaGraduacionPorcentaje / 100m;

        var estudiantesInicioPeriodo1 = configuracion.EstudiantesPeriodo1;
        var estudiantesMitadPeriodo1 = estudiantesInicioPeriodo1;
        var estudiantesFinalPeriodo1 = estudiantesInicioPeriodo1;

        for (var ciclo = 1; ciclo <= configuracion.TotalCiclos; ciclo++)
        {
            var anioAcademico = cohorteAnio + ((ciclo - 1) / 2);
            var aplicaRetencion = EsZonaRetencion(ciclo, mitadCiclos);
            var tasaAplicada = ObtenerTasaAplicada(aplicaRetencion, tasaRetencion, tasaGraduacion);
            var estudiantesSiguiente = CalcularSiguiente(
                estudiantesInicioPeriodo1,
                tasaAplicada,
                ciclo,
                configuracion.TotalCiclos);

            var retenidos = ObtenerRetenidos(aplicaRetencion, estudiantesInicioPeriodo1, estudiantesSiguiente);
            var graduados = ObtenerGraduados(aplicaRetencion, estudiantesSiguiente);
            var reprobados = decimal.Round(estudiantesInicioPeriodo1 - estudiantesSiguiente, 4);

            detalles.Add(new DetalleSimulacionRetencion(
                simulacionRetencionId: 0,
                ciclo: ciclo,
                estudiantesInicio: estudiantesInicioPeriodo1,
                estudiantesRetenidos: retenidos,
                estudiantesReprobados: reprobados,
                estudiantesGraduados: graduados,
                costoMatriculaProyectado: 0m,
                anioAcademico: anioAcademico));

            if (ciclo == mitadCiclos)
                estudiantesMitadPeriodo1 = estudiantesInicioPeriodo1;

            if (ciclo == configuracion.TotalCiclos)
                estudiantesFinalPeriodo1 = estudiantesInicioPeriodo1;

            if (ciclo < configuracion.TotalCiclos)
                estudiantesInicioPeriodo1 = estudiantesSiguiente;
        }

        return (
            detalles,
            decimal.Round(estudiantesMitadPeriodo1, 4),
            decimal.Round(estudiantesFinalPeriodo1, 4));
    }

    private static bool EsZonaRetencion(int ciclo, int mitadCiclos)
        => ciclo <= mitadCiclos;

    private static decimal ObtenerTasaAplicada(bool aplicaRetencion, decimal tasaRetencion, decimal tasaGraduacion)
        => aplicaRetencion ? tasaRetencion : tasaGraduacion;

    private static decimal CalcularSiguiente(decimal estudiantesInicio, decimal tasaAplicada, int ciclo, int totalCiclos)
        => ciclo < totalCiclos
            ? decimal.Round(estudiantesInicio * tasaAplicada, 4)
            : estudiantesInicio;

    private static decimal ObtenerRetenidos(bool aplicaRetencion, decimal estudiantesInicio, decimal estudiantesSiguiente)
        => aplicaRetencion ? estudiantesSiguiente : estudiantesInicio;

    private static decimal ObtenerGraduados(bool aplicaRetencion, decimal estudiantesSiguiente)
        => aplicaRetencion ? 0m : estudiantesSiguiente;
}
