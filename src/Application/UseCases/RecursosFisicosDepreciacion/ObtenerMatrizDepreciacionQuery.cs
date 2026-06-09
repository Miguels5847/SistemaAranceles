using SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.RecursosFisicosDepreciacion;

public sealed class ObtenerMatrizDepreciacionQuery(
    ObtenerMatrizInversionesQuery obtenerMatrizInversionesQuery,
    IRepositorioActivoFijo repositorioActivoFijo)
{
    public async Task<MatrizDepreciacionDto> EjecutarAsync(int carreraId, int escenarioProyeccionId, int? aniosProyeccion = null, CancellationToken ct = default, MatrizInversionesDto? inversionesPrecalculada = null)
    {
        var matrizInversiones = inversionesPrecalculada
            ?? await obtenerMatrizInversionesQuery.EjecutarAsync(carreraId, escenarioProyeccionId, aniosProyeccion, ct);
        if (matrizInversiones.Periodos.Count == 0 || matrizInversiones.Filas.Count == 0)
            return Vacia(carreraId, escenarioProyeccionId);

        var periodos = matrizInversiones.Periodos
            .Select(p => new PeriodoDepreciacionDto
            {
                Anio = p.Anio,
                Semestre = p.Semestre,
                NumeroPeriodo = p.NumeroPeriodo,
                Etiqueta = p.Etiqueta
            })
            .ToList();

        var activos = await repositorioActivoFijo.ListarPorCarreraAsync(carreraId, ct: ct);
        var activosPorId = activos.ToDictionary(a => a.Id);

        var filas = new List<FilaMatrizDepreciacionDto>();
        foreach (var filaInversion in matrizInversiones.Filas)
        {
            if (!activosPorId.TryGetValue(filaInversion.ActivoFijoId, out var activo))
                continue;

            var montosPorPeriodo = filaInversion.Celdas
                .ToDictionary(c => c.NumeroPeriodo, c => c.Monto);

            var celdas = CalculadoraDepreciacion.CalcularFila(
                periodos,
                montosPorPeriodo,
                activo.VidaUtilAnios,
                activo.PorcentajeResidual);

            var valorInicial = filaInversion.Celdas.FirstOrDefault(c => c.NumeroPeriodo == 1)?.Monto ?? 0m;

            filas.Add(new FilaMatrizDepreciacionDto
            {
                ActivoFijoId = filaInversion.ActivoFijoId,
                Descripcion = filaInversion.Descripcion,
                Categoria = filaInversion.Categoria,
                CategoriaNombre = filaInversion.CategoriaNombre,
                ValorInicial = valorInicial,
                ValorResidual = CalculadoraDepreciacion.CalcularValorResidual(valorInicial, activo.PorcentajeResidual),
                VidaUtilAnios = activo.VidaUtilAnios,
                Celdas = celdas
            });
        }

        var totales = periodos.Select(p => new TotalPeriodoDepreciacionDto
        {
            Anio = p.Anio,
            Semestre = p.Semestre,
            NumeroPeriodo = p.NumeroPeriodo,
            Etiqueta = p.Etiqueta,
            DepreciacionPeriodo = decimal.Round(
                filas.Sum(f => f.Celdas.First(c => c.NumeroPeriodo == p.NumeroPeriodo).DepreciacionPeriodo),
                2),
            DepreciacionAcumulada = decimal.Round(
                filas.Sum(f => f.Celdas.First(c => c.NumeroPeriodo == p.NumeroPeriodo).DepreciacionAcumulada),
                2)
        }).ToList();

        return new MatrizDepreciacionDto
        {
            CarreraId = carreraId,
            EscenarioProyeccionId = escenarioProyeccionId,
            Periodos = periodos,
            Filas = filas,
            TotalesPorPeriodo = totales
        };
    }

    private static MatrizDepreciacionDto Vacia(int carreraId, int escenarioId) => new()
    {
        CarreraId = carreraId,
        EscenarioProyeccionId = escenarioId
    };
}
