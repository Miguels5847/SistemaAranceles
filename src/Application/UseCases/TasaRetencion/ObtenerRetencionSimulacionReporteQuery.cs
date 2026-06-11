using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.TasaRetencion;

/// <summary>
/// Arma la sección de retención del reporte por dirección (KAN-49) desde la configuración
/// de retención del escenario. La tabla por ciclos replica el cálculo de la pantalla de
/// simulación: el grupo de ingreso decae con la tasa de retención en la primera mitad de
/// la malla y con la de graduación en la segunda.
/// </summary>
public sealed class ObtenerRetencionSimulacionReporteQuery(
    IRepositorioConfiguracionRetencion repositorioConfiguracion)
{
    public async Task<RetencionSimulacionReporteDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        if (carreraId <= 0 || escenarioProyeccionId is null or <= 0)
            return new RetencionSimulacionReporteDto();

        var configuracion = (await repositorioConfiguracion.ListarDtoAsync(ct))
            .FirstOrDefault(c => c.CarreraId == carreraId && c.EscenarioProyeccionId == escenarioProyeccionId);
        if (configuracion is null)
            return new RetencionSimulacionReporteDto();

        var totalCiclos = Math.Max(2, configuracion.TotalCiclos);
        var mitad = totalCiclos / 2;
        var factorRetencion = configuracion.TasaRetencionPorcentaje / 100m;
        var factorGraduacion = configuracion.TasaGraduacionPorcentaje / 100m;

        var alumnos1 = new List<decimal>(totalCiclos);
        var alumnos2 = new List<decimal>(totalCiclos - 1);
        var valorPeriodo1 = configuracion.EstudiantesPeriodo1;
        var valorPeriodo2 = configuracion.EstudiantesPeriodo2;

        for (var ciclo = 1; ciclo <= totalCiclos; ciclo++)
        {
            alumnos1.Add(decimal.Round(valorPeriodo1, 0, MidpointRounding.AwayFromZero));

            if (ciclo < totalCiclos)
                alumnos2.Add(decimal.Round(valorPeriodo2, 0, MidpointRounding.AwayFromZero));

            if (ciclo >= totalCiclos)
                continue;

            var tasa = ciclo <= mitad ? factorRetencion : factorGraduacion;
            valorPeriodo1 = decimal.Round(valorPeriodo1 * tasa, 4);
            valorPeriodo2 = decimal.Round(valorPeriodo2 * tasa, 4);
        }

        return new RetencionSimulacionReporteDto
        {
            TieneDatos = true,
            TasaRetencionPorcentaje = configuracion.TasaRetencionPorcentaje,
            TasaGraduacionPorcentaje = configuracion.TasaGraduacionPorcentaje,
            MetaRetencionPorcentaje = configuracion.MetaRetencionPorcentaje,
            MetaGraduacionPorcentaje = configuracion.MetaGraduacionPorcentaje,
            TasaRetencionAplicada = configuracion.MetaRetencionPorcentaje ?? configuracion.TasaRetencionPorcentaje,
            TasaGraduacionAplicada = configuracion.MetaGraduacionPorcentaje ?? configuracion.TasaGraduacionPorcentaje,
            ParalelosPeriodo1 = configuracion.ParalelosPeriodo1,
            ParalelosPeriodo2 = configuracion.ParalelosPeriodo2,
            EstudiantesPeriodo1 = configuracion.EstudiantesPeriodo1,
            EstudiantesPeriodo2 = configuracion.EstudiantesPeriodo2,
            TotalCiclos = totalCiclos,
            AlumnosPeriodo1PorCiclo = alumnos1,
            AlumnosPeriodo2PorCiclo = alumnos2
        };
    }
}
