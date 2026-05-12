using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Genera (on-the-fly) la tabla de sueldos del período seleccionado para una carrera+escenario,
/// combinando catálogo de cargos + estudiantes proyectados + inflación encadenada por año.
/// </summary>
public sealed class GenerarTablaSueldosPeriodoQuery(
    IRepositorioCargoFacultad repositorioCargo,
    IRepositorioInflacionAnual repositorioInflacion,
    IRepositorioProyeccionEstudiantes repositorioProyeccionEstudiantes,
    IRepositorioCarrera repositorioCarrera)
{
    public async Task<SueldosPeriodoVistaDto> EjecutarAsync(
        int carreraId,
        int escenarioProyeccionId,
        int periodoAcademicoId,
        decimal estudiantesUA,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0 || escenarioProyeccionId <= 0 || periodoAcademicoId <= 0)
            return new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodoAcademicoId };

        var proyeccionId = await repositorioProyeccionEstudiantes.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId,
            escenarioProyeccionId,
            cancellationToken);

        if (proyeccionId is null or 0)
            return new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodoAcademicoId };

        var proyeccion = await repositorioProyeccionEstudiantes.ObtenerDtoPorIdAsync(proyeccionId.Value, cancellationToken);
        if (proyeccion is null)
            return new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodoAcademicoId };

        var detallesPeriodo = proyeccion.Detalles
            .Where(d => d.PeriodoAcademicoId == periodoAcademicoId)
            .ToList();

        if (detallesPeriodo.Count == 0)
            return new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodoAcademicoId };

        var primerDetalle = detallesPeriodo[0];
        var anioPeriodo = primerDetalle.Anio;
        var numeroPeriodo = primerDetalle.NumeroPeriodo;
        var etiquetaPeriodo = primerDetalle.EtiquetaPeriodo;
        var anioBase = proyeccion.AnioBase;

        var estudiantesCarrera = detallesPeriodo.Sum(d => d.TotalEstudiantes);

        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, cancellationToken);
        var carreraNombre = carrera?.Nombre ?? string.Empty;

        var registrosInflacion = await repositorioInflacion.ListarPorRangoAsync(anioBase, anioPeriodo, cancellationToken);
        var factorEncadenado = CalculoCargosFacultad.CalcularFactorInflacionEncadenado(
            registrosInflacion,
            anioBase,
            anioPeriodo,
            numeroPeriodo);
        var inflacionPeriodoPct = ObtenerPorcentajeAnio(registrosInflacion, anioPeriodo);

        var cargos = await repositorioCargo.ListarPorCarreraAsync(carreraId, cancellationToken);

        // Fallback: si la carrera seleccionada no tiene cargos en el catálogo,
        // usar los de cualquier carrera que sí los tenga como plantilla compartida.
        if (cargos.Count == 0)
        {
            var todasLasCarreras = await repositorioCarrera.ListarAsync();
            foreach (var c in todasLasCarreras.Where(x => x.Id != carreraId))
            {
                var alternativos = await repositorioCargo.ListarPorCarreraAsync(c.Id, cancellationToken);
                if (alternativos.Count > 0)
                {
                    cargos = alternativos;
                    break;
                }
            }
        }

        var parametros = new ParametrosCalculoCargoFacultadDto
        {
            EstudiantesCarreraPeriodo = estudiantesCarrera,
            EstudiantesUnidadAcademica = estudiantesUA < 0m ? 0m : estudiantesUA,
            FactorInflacion = factorEncadenado,
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
            Anio = anioPeriodo,
            NumeroPeriodo = numeroPeriodo,
            EtiquetaPeriodo = etiquetaPeriodo,
            EstudiantesUA = parametros.EstudiantesUnidadAcademica,
            EstudiantesCarrera = estudiantesCarrera,
            InflacionPeriodoPorcentaje = inflacionPeriodoPct,
            ValorBaseDecimoCuartoAnual = parametros.ValorBaseDecimoCuartoSemestral * 2m,
            Filas = filas,
            TotalNumeroPersonas = Math.Round(filas.Sum(f => f.NumeroPersonas), 2),
            TotalSueldoMensual = Math.Round(filas.Sum(f => f.SueldoMensual), 2),
            TotalDecimoTercero = Math.Round(filas.Sum(f => f.DecimoTerceroSemestral), 2),
            TotalDecimoCuarto = Math.Round(filas.Sum(f => f.DecimoCuartoSemestral), 2),
            TotalVacaciones = Math.Round(filas.Sum(f => f.VacacionesSemestral), 2),
            TotalFondoReserva = Math.Round(filas.Sum(f => f.FondoReservaMensual), 2),
            TotalAportePatronal = Math.Round(filas.Sum(f => f.AportePatronalMensual), 2),
            TotalSemestrePeriodo = Math.Round(filas.Sum(f => f.TotalSemestre), 2),
        };
    }

    private static decimal ObtenerPorcentajeAnio(IReadOnlyList<InflacionAnual> registros, int anio)
    {
        var registro = registros
            .Where(r => r.Anio == anio)
            .OrderBy(r => r.TipoFuente.Equals("estimacion", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
            .FirstOrDefault();
        return registro?.PorcentajeInflacion ?? 0m;
    }

    private static FilaSueldoPeriodoDto Calcular(CargoFacultad cargo, ParametrosCalculoCargoFacultadDto parametros)
    {
        var peso = CalculoCargosFacultad.CalcularPeso(
            cargo,
            parametros.EstudiantesCarreraPeriodo,
            parametros.EstudiantesUnidadAcademica);

        var personas = cargo.CantidadDefault;

        // CU-SP-02 RN-79b: TP se paga por hora sin beneficios sociales.
        // TODO Fase 4: hTP[p] vendrá del consolidador via override; por ahora HorasTPMaxSemana.
        if (CalculoCargosFacultad.EsTiempoParcial(cargo))
        {
            var tarifaAjustada = Math.Round(cargo.TarifaHora * parametros.FactorInflacion, 4);
            var totalTP = CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
                tarifaAjustada, ConstantesDocentes.HorasTPMaxSemana, personas, peso, 1m);
            var sueldoMensualEquivalente = Math.Round(
                tarifaAjustada * ConstantesDocentes.HorasTPMaxSemana * ConstantesDocentes.SemanasPorMes, 2);

            return new FilaSueldoPeriodoDto
            {
                CargoId = cargo.Id,
                NombreCargo = cargo.NombreCargo,
                EsCargoDocente = cargo.EsCargoDocente,
                NumeroPersonas = personas,
                Peso = peso,
                SueldoMensual = sueldoMensualEquivalente,
                DecimoTerceroSemestral = 0m,
                DecimoCuartoSemestral = 0m,
                VacacionesSemestral = 0m,
                FondoReservaMensual = 0m,
                AportePatronalMensual = 0m,
                TotalSemestre = totalTP,
            };
        }

        var sueldoMensualAjustado = Math.Round(cargo.SueldoBaseMensual * parametros.FactorInflacion, 2);
        var d14SemestralAjustado = Math.Round(
            CalculoCargosFacultad.CalcularDecimoCuartoSemestral(parametros.ValorBaseDecimoCuartoSemestral) * parametros.FactorInflacion,
            2);

        var d13 = CalculoCargosFacultad.CalcularDecimoTerceroSemestral(sueldoMensualAjustado);
        var vacaciones = CalculoCargosFacultad.CalcularVacacionesSemestral(sueldoMensualAjustado);
        var fondoReserva = CalculoCargosFacultad.CalcularFondoReservaMensual(sueldoMensualAjustado, parametros.TasaFondoReserva);
        var aportePatronal = CalculoCargosFacultad.CalcularAportePatronalMensual(sueldoMensualAjustado, parametros.TasaAportePatronal);

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
