using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.Mantenimiento;

/// <summary>
/// Calcula totales de servicios/mantenimiento y proyección semestral (KAN-28).
/// Fórmula: (TotalTipo / AlumnosUniversidad / 2) × DemandaPeriodo × FactorInflacion
/// </summary>
public sealed class ObtenerResumenMantenimientoQuery(
    IRepositorioServicioMantenimiento repositorio,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IServicioInflacionFactor servicioInflacion)
{
    public async Task<ResumenMantenimientoDto> EjecutarAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var totalServicios = await repositorio.SumarCostoAnualPorTipoAsync(carreraId, TipoRubroMantenimiento.ServicioBasico, ct);
        var totalMant = await repositorio.SumarCostoAnualPorTipoAsync(carreraId, TipoRubroMantenimiento.Mantenimiento, ct);

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var alumnosRef = datos?.NumeroEstudiantesUniversidad ?? 1;
        if (alumnosRef <= 0) alumnosRef = 1;

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(carreraId, escenarioProyeccionId, ct);
        IReadOnlyList<PeriodoMantenimientoDto> periodos = [];

        if (proyeccionId.HasValue)
        {
            var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
            if (proyeccion is not null)
            {
                var lista = new List<PeriodoMantenimientoDto>();
                foreach (var detalle in proyeccion.Detalles.OrderBy(d => d.Anio).ThenBy(d => d.NumeroPeriodo))
                {
                    var factor = await servicioInflacion.ObtenerFactorAcumuladoAsync(detalle.Anio, detalle.NumeroPeriodo, ct);
                    var demanda = detalle.TotalEstudiantes;

                    var costoServ = decimal.Round((totalServicios / alumnosRef / 2m) * demanda * factor, 2);
                    var costoMant = decimal.Round((totalMant / alumnosRef / 2m) * demanda * factor, 2);

                    lista.Add(new PeriodoMantenimientoDto
                    {
                        Anio = detalle.Anio,
                        NumeroPeriodo = detalle.NumeroPeriodo,
                        Etiqueta = detalle.EtiquetaPeriodo,
                        CostoServiciosBasicos = costoServ,
                        CostoMantenimiento = costoMant,
                    });
                }
                periodos = lista;
            }
        }

        return new ResumenMantenimientoDto
        {
            TotalServiciosBasicos = totalServicios,
            TotalMantenimiento = totalMant,
            Proyeccion = periodos,
        };
    }
}
