using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Genera (on-the-fly) la tabla de sueldos del período seleccionado para una carrera,
/// combinando catálogo de cargos + estudiantes proyectados + inflación acumulada.
/// </summary>
public sealed class GenerarTablaSueldosPeriodoQuery(
    IRepositorioCargoFacultad repositorioCargo,
    IRepositorioPeriodoAcademico repositorioPeriodo,
    IRepositorioInflacionAnual repositorioInflacion,
    IRepositorioProyeccionEstudiantes repositorioProyeccionEstudiantes,
    IRepositorioCarrera repositorioCarrera)
{
    private const int AnioBase = 2023;
    private const decimal EstudiantesUaPorDefecto = 285m;

    public async Task<SueldosPeriodoVistaDto> EjecutarAsync(
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0 || periodoAcademicoId <= 0)
            return new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodoAcademicoId };

        var periodos = await repositorioPeriodo.ListarTodosAsync(cancellationToken);
        var periodo = periodos.FirstOrDefault(p => p.Id == periodoAcademicoId);
        if (periodo is null)
            return new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodoAcademicoId };

        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, cancellationToken);
        var carreraNombre = carrera?.Nombre ?? string.Empty;

        var (inflacionAcumulada, inflacionPeriodoPct) = await CalcularInflacionAcumuladaAsync(
            periodo.Anio,
            cancellationToken);

        var estudiantesCarrera = await ObtenerEstudiantesCarreraAsync(
            carreraId,
            periodoAcademicoId,
            cancellationToken);

        var cargos = await repositorioCargo.ListarPorCarreraAsync(carreraId, cancellationToken);
        var parametros = new ParametrosCalculoCargoFacultadDto
        {
            EstudiantesCarreraPeriodo = estudiantesCarrera,
            EstudiantesUnidadAcademica = EstudiantesUaPorDefecto,
            FactorInflacion = inflacionAcumulada,
        };

        var filas = cargos
            .OrderBy(c => c.NombreCargo)
            .Select(c => Calcular(c, parametros))
            .ToList();

        return new SueldosPeriodoVistaDto
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            PeriodoAcademicoId = periodoAcademicoId,
            Anio = periodo.Anio,
            NumeroPeriodo = periodo.NumeroPeriodo,
            EtiquetaPeriodo = periodo.EtiquetaPeriodo,
            EstudiantesUA = parametros.EstudiantesUnidadAcademica,
            EstudiantesCarrera = estudiantesCarrera,
            InflacionAcumulada = inflacionAcumulada,
            InflacionPeriodoPorcentaje = inflacionPeriodoPct,
            ValorBaseDecimoCuartoAnual = parametros.ValorBaseDecimoCuartoSemestral * 2m,
            Filas = filas,
            TotalSemestrePeriodo = Math.Round(filas.Sum(f => f.TotalSemestre), 2),
        };
    }

    private async Task<(decimal Acumulada, decimal PorcentajePeriodo)> CalcularInflacionAcumuladaAsync(
        int anioPeriodo,
        CancellationToken cancellationToken)
    {
        if (anioPeriodo <= AnioBase)
            return (1m, 0m);

        var registros = await repositorioInflacion.ListarPorRangoAsync(AnioBase + 1, anioPeriodo, cancellationToken);

        decimal acumulada = 1m;
        decimal porcentajePeriodo = 0m;

        for (var anio = AnioBase + 1; anio <= anioPeriodo; anio++)
        {
            var registro = registros
                .Where(r => r.Anio == anio)
                .OrderBy(r => r.TipoFuente.Equals("estimacion", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .FirstOrDefault();

            var pct = registro?.PorcentajeInflacion ?? 0m;
            var factor = 1m + (pct / 100m);
            if (factor < 1m) factor = 1m;

            acumulada *= factor;
            if (anio == anioPeriodo)
                porcentajePeriodo = pct;
        }

        return (Math.Round(acumulada, 6), porcentajePeriodo);
    }

    private async Task<decimal> ObtenerEstudiantesCarreraAsync(
        int carreraId,
        int periodoAcademicoId,
        CancellationToken cancellationToken)
    {
        var resumenes = await repositorioProyeccionEstudiantes.ListarResumenAsync(carreraId, null, cancellationToken);
        var resumen = resumenes
            .OrderByDescending(r => r.ActualizadoEn ?? r.CreadoEn)
            .FirstOrDefault();

        if (resumen is null)
            return 0m;

        var detalle = await repositorioProyeccionEstudiantes.ObtenerDtoPorIdAsync(resumen.Id, cancellationToken);
        if (detalle is null)
            return 0m;

        return detalle.Detalles
            .Where(d => d.PeriodoAcademicoId == periodoAcademicoId)
            .Sum(d => d.TotalEstudiantes);
    }

    private static FilaSueldoPeriodoDto Calcular(CargoFacultad cargo, ParametrosCalculoCargoFacultadDto parametros)
    {
        var sueldoMensualAjustado = Math.Round(cargo.SueldoBaseMensual * parametros.FactorInflacion, 2);
        var d14SemestralAjustado = Math.Round(
            CalculoCargosFacultad.CalcularDecimoCuartoSemestral(parametros.ValorBaseDecimoCuartoSemestral) * parametros.FactorInflacion,
            2);

        var d13 = CalculoCargosFacultad.CalcularDecimoTerceroSemestral(sueldoMensualAjustado);
        var vacaciones = CalculoCargosFacultad.CalcularVacacionesSemestral(sueldoMensualAjustado);
        var fondoReserva = CalculoCargosFacultad.CalcularFondoReservaMensual(sueldoMensualAjustado, parametros.TasaFondoReserva);
        var aportePatronal = CalculoCargosFacultad.CalcularAportePatronalMensual(sueldoMensualAjustado, parametros.TasaAportePatronal);

        var peso = CalculoCargosFacultad.CalcularPeso(
            cargo,
            parametros.EstudiantesCarreraPeriodo,
            parametros.EstudiantesUnidadAcademica);

        var personas = cargo.CantidadDefault;

        var costoBaseSemestral = ((sueldoMensualAjustado + fondoReserva + aportePatronal) * 6m)
                                 + d13 + d14SemestralAjustado + vacaciones;

        var totalSemestre = Math.Round(costoBaseSemestral * personas * peso, 2);

        return new FilaSueldoPeriodoDto
        {
            CargoId = cargo.Id,
            NombreCargo = cargo.NombreCargo,
            EsCargoDocente = cargo.EsCargoDocente,
            NumeroPersonas = personas,
            Peso = peso,
            SueldoMensual = sueldoMensualAjustado,
            DecimoTerceroSemestral = d13,
            DecimoCuartoSemestral = d14SemestralAjustado,
            VacacionesSemestral = vacaciones,
            FondoReservaMensual = fondoReserva,
            AportePatronalMensual = aportePatronal,
            TotalSemestre = totalSemestre,
        };
    }
}
