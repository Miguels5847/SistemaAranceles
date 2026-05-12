using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CargosFacultad;

/// <summary>
/// Genera el pivot completo "Resumen Sueldos": filas=cargos, columnas=todos los períodos
/// de la proyección de estudiantes (carrera+escenario). Cada celda es el costo semestral
/// del cargo en ese período aplicando inflación encadenada por año.
/// </summary>
public sealed class GenerarResumenSueldosQuery(
    IRepositorioCargoFacultad repositorioCargo,
    IRepositorioInflacionAnual repositorioInflacion,
    IRepositorioProyeccionEstudiantes repositorioProyeccionEstudiantes,
    IRepositorioCarrera repositorioCarrera)
{
    private static readonly string[] OrdenCargos =
    [
        "Decano",
        "Subdecano",
        "Director de Carrera",
        "Secretario",
        "Auxiliar de Secretaria",
        "Coordinador",
        "Bienestar Estudiantil",
        "Tiempo Completo PhD",
        "Tiempo Completo Mgs.",
        "Medio Tiempo",
        "Tiempo Parcial",
        "Ocasional Tipo 2 (Técnico Docente)",
        "Bibliotecario",
        "Auxiliar de Servicio",
        "Guardia",
    ];

    public async Task<ResumenSueldosVistaDto> EjecutarAsync(
        int carreraId,
        int escenarioProyeccionId,
        decimal estudiantesUA,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0 || escenarioProyeccionId <= 0)
            return new ResumenSueldosVistaDto { CarreraId = carreraId };

        var proyeccionId = await repositorioProyeccionEstudiantes.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId, escenarioProyeccionId, cancellationToken);
        if (proyeccionId is null or 0)
            return new ResumenSueldosVistaDto { CarreraId = carreraId };

        var proyeccion = await repositorioProyeccionEstudiantes.ObtenerDtoPorIdAsync(proyeccionId.Value, cancellationToken);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return new ResumenSueldosVistaDto { CarreraId = carreraId };

        var periodos = proyeccion.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .Select(g =>
            {
                var p = g.First();
                return new PeriodoDisponibleSueldosDto
                {
                    PeriodoAcademicoId = p.PeriodoAcademicoId,
                    Anio = p.Anio,
                    NumeroPeriodo = p.NumeroPeriodo,
                    EtiquetaPeriodo = p.EtiquetaPeriodo,
                };
            })
            .OrderBy(p => p.Anio).ThenBy(p => p.NumeroPeriodo)
            .ToList();

        var estudiantesPorPeriodo = proyeccion.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalEstudiantes));

        var anioBase = proyeccion.AnioBase;
        var anioMax = periodos.Max(p => p.Anio);
        var registrosInflacion = await repositorioInflacion.ListarPorRangoAsync(anioBase, anioMax, cancellationToken);

        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, cancellationToken);
        var carreraNombre = carrera?.Nombre ?? string.Empty;

        var cargos = await repositorioCargo.ListarPorCarreraAsync(carreraId, cancellationToken);
        if (cargos.Count == 0)
        {
            var todas = await repositorioCarrera.ListarAsync();
            foreach (var c in todas.Where(x => x.Id != carreraId))
            {
                var alt = await repositorioCargo.ListarPorCarreraAsync(c.Id, cancellationToken);
                if (alt.Count > 0) { cargos = alt; break; }
            }
        }

        var cargosOrdenados = OrdenarCargos(cargos);
        var parametrosBase = new ParametrosCalculoCargoFacultadDto
        {
            EstudiantesUnidadAcademica = estudiantesUA < 0m ? 0m : estudiantesUA,
        };

        var filas = new List<FilaResumenSueldosDto>(cargosOrdenados.Count);
        var totalesPorPeriodo = new decimal[periodos.Count];

        foreach (var cargo in cargosOrdenados)
        {
            var valores = new decimal[periodos.Count];

            decimal pesoPrimerPeriodo = 0m;

            for (int i = 0; i < periodos.Count; i++)
            {
                var periodo = periodos[i];
                var factor = CalcularFactorEncadenado(registrosInflacion, anioBase, periodo.Anio, periodo.NumeroPeriodo);
                var estCarrera = estudiantesPorPeriodo.GetValueOrDefault(periodo.PeriodoAcademicoId, 0m);

                var parametros = new ParametrosCalculoCargoFacultadDto
                {
                    EstudiantesCarreraPeriodo = estCarrera,
                    EstudiantesUnidadAcademica = parametrosBase.EstudiantesUnidadAcademica,
                    FactorInflacion = factor,
                };

                var total = CalcularTotalSemestre(cargo, parametros);
                valores[i] = total;
                totalesPorPeriodo[i] += total;

                if (i == 0)
                    pesoPrimerPeriodo = CalculoCargosFacultad.CalcularPeso(
                        cargo, estCarrera, parametros.EstudiantesUnidadAcademica);
            }

            filas.Add(new FilaResumenSueldosDto
            {
                CargoId = cargo.Id,
                NombreCargo = cargo.NombreCargo,
                EsCargoDocente = cargo.EsCargoDocente,
                Peso = pesoPrimerPeriodo,
                NumeroPersonas = cargo.CantidadDefault,
                ValoresPorPeriodo = valores,
                TotalFila = Math.Round(valores.Sum(), 2),
            });
        }

        for (int i = 0; i < totalesPorPeriodo.Length; i++)
            totalesPorPeriodo[i] = Math.Round(totalesPorPeriodo[i], 2);

        var ultimoPeriodo = periodos[^1];
        return new ResumenSueldosVistaDto
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            Periodos = periodos,
            Filas = filas,
            TotalesPorPeriodo = totalesPorPeriodo,
            GranTotal = Math.Round(totalesPorPeriodo.Sum(), 2),
            TotalSemestralPeriodoFinal = totalesPorPeriodo[^1],
            EtiquetaPeriodoFinal = $"P{ultimoPeriodo.NumeroPeriodo}-{ultimoPeriodo.Anio}",
        };
    }

    private static List<CargoFacultad> OrdenarCargos(IReadOnlyList<CargoFacultad> cargos)
    {
        var indice = OrdenCargos
            .Select((nombre, i) => (nombre, i))
            .ToDictionary(x => x.nombre, x => x.i, StringComparer.OrdinalIgnoreCase);

        return cargos
            .OrderBy(c => indice.TryGetValue(c.NombreCargo, out var idx) ? idx : int.MaxValue)
            .ThenBy(c => c.NombreCargo)
            .ToList();
    }

    private static decimal CalcularFactorEncadenado(
        IReadOnlyList<InflacionAnual> registros,
        int anioBase,
        int anioPeriodo,
        int numeroPeriodo)
    {
        if (anioPeriodo < anioBase)
            return 1m;

        decimal factor = 1m;
        for (var anio = anioBase; anio <= anioPeriodo; anio++)
        {
            var registro = registros
                .Where(r => r.Anio == anio)
                .OrderBy(r => r.TipoFuente.Equals("estimacion", StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                .FirstOrDefault();
            var pct = registro?.PorcentajeInflacion ?? 0m;

            if (anio == anioPeriodo)
            {
                if (numeroPeriodo >= 2)
                    factor *= 1m + (pct / 100m);
            }
            else
            {
                factor *= 1m + (pct / 100m);
            }
        }

        if (factor < 1m) factor = 1m;
        return Math.Round(factor, 6);
    }

    private static decimal CalcularTotalSemestre(CargoFacultad cargo, ParametrosCalculoCargoFacultadDto parametros)
    {
        var peso = CalculoCargosFacultad.CalcularPeso(cargo, parametros.EstudiantesCarreraPeriodo, parametros.EstudiantesUnidadAcademica);
        var personas = cargo.CantidadDefault;

        // CU-SP-02 RN-79b: TP por hora sin beneficios. TODO Fase 5: hTP[p] real desde consolidador refactorizado.
        if (CalculoCargosFacultad.EsTiempoParcial(cargo))
        {
            var tarifaAjustada = Math.Round(cargo.TarifaHora * parametros.FactorInflacion, 4);
            return CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
                tarifaAjustada, ConstantesDocentes.HorasTPMaxSemana, personas, peso, 1m);
        }

        var sueldoMensualAjustado = Math.Round(cargo.SueldoBaseMensual * parametros.FactorInflacion, 2);
        var d14SemestralAjustado = Math.Round(
            CalculoCargosFacultad.CalcularDecimoCuartoSemestral(parametros.ValorBaseDecimoCuartoSemestral) * parametros.FactorInflacion, 2);

        var d13 = CalculoCargosFacultad.CalcularDecimoTerceroSemestral(sueldoMensualAjustado);
        var vacaciones = CalculoCargosFacultad.CalcularVacacionesSemestral(sueldoMensualAjustado);
        var fondoReserva = CalculoCargosFacultad.CalcularFondoReservaMensual(sueldoMensualAjustado, parametros.TasaFondoReserva);
        var aportePatronal = CalculoCargosFacultad.CalcularAportePatronalMensual(sueldoMensualAjustado, parametros.TasaAportePatronal);

        var costoBaseSemestral = ((sueldoMensualAjustado + fondoReserva + aportePatronal) * 6m)
                                 + d13 + d14SemestralAjustado + vacaciones;

        return Math.Round(costoBaseSemestral * personas * peso, 2);
    }
}
