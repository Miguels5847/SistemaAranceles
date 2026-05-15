using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Application.UseCases.SueldosPlantaCentral;

public sealed class CalcularAportePlantaCentralCarreraQuery(
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioCarrera repositorioCarrera)
{
    public async Task<AportePlantaCentralCarreraDto> EjecutarAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0)
        {
            throw new DominioException("CarreraId es obligatorio.");
        }

        if (escenarioProyeccionId <= 0)
        {
            throw new DominioException("EscenarioProyeccionId es obligatorio.");
        }

        var datos = await repositorioDatos.ObtenerVigenteAsync(cancellationToken)
            ?? throw new DominioException("No existe registro de Datos Institucionales vigente. Configure el modulo Datos Institucionales antes de calcular el aporte.");

        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, cancellationToken)
            ?? throw new DominioException($"Carrera {carreraId} no existe.");

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId, escenarioProyeccionId, cancellationToken)
            ?? throw new DominioException("No existe proyeccion de estudiantes para la combinacion carrera/escenario.");

        var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId, cancellationToken)
            ?? throw new DominioException("Proyeccion de estudiantes no encontrada.");

        var totalMensualPc = datos.TotalMensualPlantaCentral;
        var totalAnualPc = datos.TotalAnualPlantaCentral;
        var estudiantesUniversidad = datos.NumeroEstudiantesUniversidad;

        var periodos = proyeccion.Detalles
            .OrderBy(d => d.Anio)
            .ThenBy(d => d.NumeroPeriodo)
            .Select(d =>
            {
                var aporteSemestral = estudiantesUniversidad <= 0
                    ? 0m
                    : Math.Round((totalMensualPc / estudiantesUniversidad) * d.TotalEstudiantes * 6m, 2);

                var porcentaje = totalAnualPc <= 0m
                    ? 0m
                    : Math.Round(aporteSemestral / totalAnualPc, 6);

                return new AportePeriodoDto
                {
                    PeriodoAcademicoId = d.PeriodoAcademicoId,
                    Etiqueta = d.EtiquetaPeriodo,
                    Anio = d.Anio,
                    NumeroPeriodoEnAnio = d.NumeroPeriodo,
                    AlumnosCarrera = d.TotalEstudiantes,
                    AporteSemestral = aporteSemestral,
                    PorcentajeSobreTotalAnual = porcentaje,
                };
            })
            .ToList();

        var aporteAcumulado = periodos.Sum(p => p.AporteSemestral);
        var aniosCubiertos = periodos.Select(p => p.Anio).Distinct().Count();
        var aportePromedioAnual = aniosCubiertos <= 0
            ? 0m
            : Math.Round(aporteAcumulado / aniosCubiertos, 2);
        var porcentajePromedio = totalAnualPc <= 0m
            ? 0m
            : Math.Round(aportePromedioAnual / totalAnualPc, 6);

        return new AportePlantaCentralCarreraDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera.Nombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            PeriodoInstitucionalVigente = datos.Periodo,
            TotalMensualPlantaCentral = totalMensualPc,
            TotalAnualPlantaCentral = totalAnualPc,
            EstudiantesUniversidad = estudiantesUniversidad,
            Periodos = periodos,
            AporteAcumulado = aporteAcumulado,
            AportePromedioAnual = aportePromedioAnual,
            PorcentajePromedioSobreTotalAnual = porcentajePromedio,
        };
    }
}
