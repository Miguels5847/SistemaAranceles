using SistemaAranceles.Application.DTOs.Mantenimiento;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.Mantenimiento;

/// <summary>
/// Calcula totales de servicios/mantenimiento y proyección semestral (KAN-28).
/// Fórmula Excel: (TotalTipo / AlumnosReferenciaServicios / 2) × DemandaTotalPeriodo × FactorInflacion.
/// La demanda se consolida por período, no por ciclo, para replicar la hoja "8 Mantenimiento".
/// </summary>
public sealed class ObtenerResumenMantenimientoQuery(
    IRepositorioServicioMantenimiento repositorio,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioProyeccionEstudiantes repositorioProyeccion,
    IRepositorioInflacionAnual repositorioInflacion)
{
    public async Task<ResumenMantenimientoDto> EjecutarAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var totalServicios = await repositorio.SumarCostoAnualPorTipoAsync(
            carreraId,
            TipoRubroMantenimiento.ServicioBasico,
            escenarioProyeccionId,
            ct);

        var totalMant = await repositorio.SumarCostoAnualPorTipoAsync(
            carreraId,
            TipoRubroMantenimiento.Mantenimiento,
            escenarioProyeccionId,
            ct);

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var alumnosRef = datos?.NumeroEstudiantesUniversidad ?? 1;
        if (alumnosRef <= 0) alumnosRef = 1;

        var proyeccionId = await repositorioProyeccion.ObtenerIdPorCarreraYEscenarioAsync(carreraId, escenarioProyeccionId, ct);
        IReadOnlyList<PeriodoMantenimientoDto> periodos = [];

        if (proyeccionId.HasValue)
        {
            var proyeccion = await repositorioProyeccion.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
            if (proyeccion is not null && proyeccion.Detalles.Count > 0)
            {
                var periodosConsolidados = proyeccion.Detalles
                    .GroupBy(d => new { d.NumeroPeriodo, d.Anio, d.EtiquetaPeriodo })
                    .Select(g => new
                    {
                        g.Key.NumeroPeriodo,
                        g.Key.Anio,
                        g.Key.EtiquetaPeriodo,
                        TotalEstudiantes = g.Sum(x => x.TotalEstudiantes)
                    })
                    .OrderBy(x => x.NumeroPeriodo)
                    .ToList();

                var anioMin = periodosConsolidados.Min(d => d.Anio);
                var anioMax = periodosConsolidados.Max(d => d.Anio);
                var inflaciones = await repositorioInflacion.ListarPorRangoAsync(anioMin, anioMax, ct);

                var lista = new List<PeriodoMantenimientoDto>();
                foreach (var detalle in periodosConsolidados)
                {
                    var semestre = detalle.NumeroPeriodo % 2 == 1 ? 1 : 2;
                    var factor = CalculoInflacionAplicada.CalcularFactorPeriodo(
                        inflaciones,
                        proyeccion.AnioBase,
                        detalle.Anio,
                        semestre);

                    var demanda = Math.Round(detalle.TotalEstudiantes, 0, MidpointRounding.AwayFromZero);
                    var costoServ = decimal.Round((totalServicios / alumnosRef / 2m) * demanda * factor, 2);
                    var costoMant = decimal.Round((totalMant / alumnosRef / 2m) * demanda * factor, 2);

                    lista.Add(new PeriodoMantenimientoDto
                    {
                        Anio = detalle.Anio,
                        Semestre = semestre,
                        NumeroPeriodo = detalle.NumeroPeriodo,
                        Etiqueta = string.IsNullOrWhiteSpace(detalle.EtiquetaPeriodo)
                            ? $"{detalle.Anio} {(semestre == 1 ? "ABR" : "SEP") }"
                            : detalle.EtiquetaPeriodo,
                        DemandaPeriodo = demanda,
                        FactorInflacion = factor,
                        CostoServiciosBasicos = costoServ,
                        CostoMantenimiento = costoMant,
                    });
                }
                periodos = lista;
            }
        }

        return new ResumenMantenimientoDto
        {
            AlumnosReferenciaServicios = alumnosRef,
            TotalServiciosBasicos = totalServicios,
            TotalMantenimiento = totalMant,
            Proyeccion = periodos,
        };
    }
}
