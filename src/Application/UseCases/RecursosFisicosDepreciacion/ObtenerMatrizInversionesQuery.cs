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
    public async Task<MatrizInversionesDto> EjecutarAsync(int carreraId, int escenarioProyeccionId, int? aniosProyeccion = null, CancellationToken ct = default)
    {
        if (carreraId <= 0 || escenarioProyeccionId <= 0)
            return Vacia(carreraId, escenarioProyeccionId);

        var proyeccionId = await repositorioProyeccionEstudiantes.ObtenerIdPorCarreraYEscenarioAsync(carreraId, escenarioProyeccionId, ct);
        if (proyeccionId is null or <= 0)
            return Vacia(carreraId, escenarioProyeccionId);

        var proyeccion = await repositorioProyeccionEstudiantes.ObtenerDtoPorIdAsync(proyeccionId.Value, ct);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return Vacia(carreraId, escenarioProyeccionId);

        var periodos = ConstruirPeriodos(proyeccion.Detalles, aniosProyeccion).ToList();
        var activos = await repositorioActivoFijo.ListarPorCarreraAsync(carreraId, ct: ct);
        var inversiones = await repositorioInversionFutura.ListarPorActivosAsync(activos.Select(a => a.Id).ToArray(), ct);
        var manuales = inversiones.ToDictionary(i => $"{i.ActivoFijoId}|{i.Anio}|{i.Semestre}", i => i.CantidadProyectada);
        var inflaciones = await repositorioInflacion.ListarPorRangoAsync(periodos.Min(p => p.Anio), periodos.Max(p => p.Anio), ct);

        var filas = new List<FilaMatrizInversionDto>();
        foreach (var activo in activos.OrderBy(a => a.Categoria).ThenBy(a => a.Descripcion))
        {
            var celdas = new List<CeldaInversionDto>();
            foreach (var periodo in periodos)
            {
                var esInicial = periodo.NumeroPeriodo == 1;
                var cantidad = esInicial ? 0m : await ResolverCantidadAsync(activo, periodo, manuales, carreraId, escenarioProyeccionId, ct);
                var factor = CalculoInflacionAplicada.CalcularFactorPeriodo(inflaciones, proyeccion.AnioBase, periodo.Anio, periodo.Semestre);
                celdas.Add(new CeldaInversionDto
                {
                    ActivoFijoId = activo.Id,
                    Anio = periodo.Anio,
                    Semestre = periodo.Semestre,
                    NumeroPeriodo = periodo.NumeroPeriodo,
                    Cantidad = cantidad,
                    ValorUnitario = activo.ValorUnitario,
                    FactorInflacion = factor,
                    Monto = decimal.Round(cantidad * activo.ValorUnitario * factor, 2),
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

        return new MatrizInversionesDto
        {
            CarreraId = carreraId,
            EscenarioProyeccionId = escenarioProyeccionId,
            Periodos = periodos,
            Filas = filas,
            TotalesPorPeriodo = periodos.Select(p => new TotalPeriodoInversionDto
            {
                Anio = p.Anio,
                Semestre = p.Semestre,
                NumeroPeriodo = p.NumeroPeriodo,
                Etiqueta = p.Etiqueta,
                Total = decimal.Round(filas.Sum(f => f.Celdas.First(c => c.Anio == p.Anio && c.Semestre == p.Semestre).Monto), 2)
            }).ToList()
        };
    }

    private static MatrizInversionesDto Vacia(int carreraId, int escenarioId) => new()
    {
        CarreraId = carreraId,
        EscenarioProyeccionId = escenarioId
    };

    private static IEnumerable<PeriodoInversionDto> ConstruirPeriodos(IReadOnlyList<Application.DTOs.Estudiantes.DetalleProyeccionEstudiantesDto> detalles, int? anios)
    {
        var periodos = detalles.GroupBy(d => d.NumeroPeriodo).Select(g =>
        {
            var d = g.First();
            var semestre = d.NumeroPeriodo % 2 == 1 ? 1 : 2;
            return new PeriodoInversionDto
            {
                Anio = d.Anio,
                Semestre = semestre,
                NumeroPeriodo = d.NumeroPeriodo,
                Etiqueta = string.IsNullOrWhiteSpace(d.EtiquetaPeriodo) ? $"{d.Anio} {(semestre == 1 ? "ABR" : "SEP")}" : d.EtiquetaPeriodo
            };
        }).OrderBy(p => p.NumeroPeriodo).ToList();

        return anios is > 0 ? periodos.Take(anios.Value * 2) : periodos;
    }

    private async Task<decimal> ResolverCantidadAsync(ActivoFijo activo, PeriodoInversionDto periodo, IReadOnlyDictionary<string, decimal> manuales, int carreraId, int escenarioId, CancellationToken ct)
    {
        return activo.TipoCalculoCantidad switch
        {
            TipoCalculoCantidad.PorEstudiante => await servicioEstudiantesTotales.ObtenerTotalEstudiantesPorSemestreAsync(carreraId, escenarioId, periodo.Anio, periodo.Semestre, ct),
            TipoCalculoCantidad.PorDocente => await servicioDocentesTotales.ObtenerTotalDocentesPorSemestreAsync(carreraId, escenarioId, periodo.Anio, periodo.Semestre, ct),
            _ => manuales.TryGetValue($"{activo.Id}|{periodo.Anio}|{periodo.Semestre}", out var cantidad) ? cantidad : 0m
        };
    }
}
