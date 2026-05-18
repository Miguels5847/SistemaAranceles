using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public sealed class ObtenerMatrizInversionesQuery(
    IRepositorioActivoFijo repositorioActivoFijo,
    IRepositorioInversionFutura repositorioInversionFutura,
    IRepositorioInflacionAnual repositorioInflacion,
    IRepositorioProyeccionEstudiantes repositorioProyeccionEstudiantes,
    IServicioEstudiantesTotales servicioEstudiantesTotales,
    IServicioDocentesTotales servicioDocentesTotales)
{
    public async Task<MatrizInversionesDto> EjecutarAsync(
        int carreraId,
        int escenarioProyeccionId,
        int? aniosProyeccion = null,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0 || escenarioProyeccionId <= 0)
        {
            return new MatrizInversionesDto
            {
                CarreraId = carreraId,
                EscenarioProyeccionId = escenarioProyeccionId
            };
        }

        var proyeccionId = await repositorioProyeccionEstudiantes.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId,
            escenarioProyeccionId,
            cancellationToken);

        if (proyeccionId is null or <= 0)
        {
            return new MatrizInversionesDto
            {
                CarreraId = carreraId,
                EscenarioProyeccionId = escenarioProyeccionId
            };
        }

        var proyeccion = await repositorioProyeccionEstudiantes.ObtenerDtoPorIdAsync(proyeccionId.Value, cancellationToken);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
        {
            return new MatrizInversionesDto
            {
                CarreraId = carreraId,
                EscenarioProyeccionId = escenarioProyeccionId
            };
        }

        var periodos = ConstruirPeriodos(proyeccion.Detalles, aniosProyeccion).ToList();
        if (periodos.Count == 0)
        {
            return new MatrizInversionesDto
            {
                CarreraId = carreraId,
                EscenarioProyeccionId = escenarioProyeccionId
            };
        }

        var activos = await repositorioActivoFijo.ListarPorCarreraAsync(carreraId, cancellationToken: cancellationToken);
        var activoIds = activos.Select(a => a.Id).ToArray();
        var inversiones = await repositorioInversionFutura.ListarPorActivosAsync(activoIds, cancellationToken);
        var inversionesPorClave = inversiones.ToDictionary(
            i => (i.ActivoFijoId, i.Anio, i.Semestre),
            i => i.CantidadProyectada);

        var anioMin = periodos.Min(p => p.Anio);
        var anioMax = periodos.Max(p => p.Anio);
        var inflaciones = await repositorioInflacion.ListarPorRangoAsync(anioMin, anioMax, cancellationToken);

        var filas = new List<FilaMatrizInversionDto>();
        foreach (var activo in activos.OrderBy(a => a.Categoria).ThenBy(a => a.Descripcion))
        {
            var celdas = new List<CeldaInversionDto>();
            foreach (var periodo in periodos)
            {
                var esInicial = periodo.NumeroPeriodo == 1;
                var factorInflacion = CalculoInflacionAplicada.CalcularFactorPeriodo(
                    inflaciones,
                    proyeccion.AnioBase,
                    periodo.Anio,
                    periodo.Semestre);

                var cantidad = await ResolverCantidadAsync(
                    activo,
                    periodo,
                    esInicial,
                    inversionesPorClave,
                    carreraId,
                    escenarioProyeccionId,
                    cancellationToken);

                var monto = decimal.Round(cantidad * activo.ValorUnitario * factorInflacion, 2);

                celdas.Add(new CeldaInversionDto
                {
                    ActivoFijoId = activo.Id,
                    Anio = periodo.Anio,
                    Semestre = periodo.Semestre,
                    NumeroPeriodo = periodo.NumeroPeriodo,
                    Cantidad = cantidad,
                    ValorUnitario = activo.ValorUnitario,
                    FactorInflacion = factorInflacion,
                    Monto = monto,
                    EsEditable = !esInicial && activo.TipoCalculoCantidad is TipoCalculoCantidad.Manual or TipoCalculoCantidad.PorHito,
                    EsPeriodoInicial = esInicial,
                    OrigenCantidad = MapeoActivoFijo.NombreTipoCalculo(activo.TipoCalculoCantidad)
                });
            }

            filas.Add(new FilaMatrizInversionDto
            {
                ActivoFijoId = activo.Id,
                Descripcion = activo.Descripcion,
                Categoria = activo.Categoria,
                CategoriaNombre = MapeoActivoFijo.NombreCategoria(activo.Categoria),
                TipoCalculoCantidad = activo.TipoCalculoCantidad,
                TipoCalculoNombre = MapeoActivoFijo.NombreTipoCalculo(activo.TipoCalculoCantidad),
                ValorUnitario = activo.ValorUnitario,
                Celdas = celdas
            });
        }

        var totales = periodos
            .Select(p => new TotalPeriodoInversionDto
            {
                Anio = p.Anio,
                Semestre = p.Semestre,
                NumeroPeriodo = p.NumeroPeriodo,
                Etiqueta = p.Etiqueta,
                Total = decimal.Round(filas.Sum(f => f.Celdas.First(c => c.Anio == p.Anio && c.Semestre == p.Semestre).Monto), 2)
            })
            .ToList();

        return new MatrizInversionesDto
        {
            CarreraId = carreraId,
            EscenarioProyeccionId = escenarioProyeccionId,
            Periodos = periodos,
            Filas = filas,
            TotalesPorPeriodo = totales
        };
    }

    private static IEnumerable<PeriodoInversionDto> ConstruirPeriodos(
        IReadOnlyList<Application.DTOs.Estudiantes.DetalleProyeccionEstudiantesDto> detalles,
        int? aniosProyeccion)
    {
        var periodos = detalles
            .GroupBy(d => d.NumeroPeriodo)
            .Select(g =>
            {
                var d = g.First();
                return new PeriodoInversionDto
                {
                    Anio = d.Anio,
                    Semestre = d.NumeroPeriodo % 2 == 1 ? 1 : 2,
                    NumeroPeriodo = d.NumeroPeriodo,
                    Etiqueta = string.IsNullOrWhiteSpace(d.EtiquetaPeriodo)
                        ? $"{d.Anio} {(d.NumeroPeriodo % 2 == 1 ? "ABR" : "SEP")}"
                        : d.EtiquetaPeriodo
                };
            })
            .OrderBy(p => p.NumeroPeriodo)
            .ToList();

        if (aniosProyeccion is > 0)
        {
            periodos = periodos.Take(aniosProyeccion.Value * 2).ToList();
        }

        return periodos;
    }

    private async Task<decimal> ResolverCantidadAsync(
        ActivoFijo activo,
        PeriodoInversionDto periodo,
        bool esInicial,
        IReadOnlyDictionary<(int ActivoFijoId, int Anio, int Semestre), decimal> inversionesPorClave,
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken cancellationToken)
    {
        if (esInicial)
        {
            return 0m;
        }

        return activo.TipoCalculoCantidad switch
        {
            TipoCalculoCantidad.PorEstudiante => await servicioEstudiantesTotales.ObtenerTotalEstudiantesPorSemestreAsync(
                carreraId,
                escenarioProyeccionId,
                periodo.Anio,
                periodo.Semestre,
                cancellationToken),
            TipoCalculoCantidad.PorDocente => await servicioDocentesTotales.ObtenerTotalDocentesPorSemestreAsync(
                carreraId,
                escenarioProyeccionId,
                periodo.Anio,
                periodo.Semestre,
                cancellationToken),
            _ => inversionesPorClave.TryGetValue((activo.Id, periodo.Anio, periodo.Semestre), out var cantidad)
                ? cantidad
                : 0m
        };
    }
}
