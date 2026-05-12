using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

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
        ProyeccionConsolidadaDto? consolidadoActual = null,
        CancellationToken cancellationToken = default)
    {
        var proyeccion = await ObtenerProyeccionAsync(carreraId, escenarioProyeccionId, cancellationToken);
        if (proyeccion is null || proyeccion.Detalles.Count == 0)
            return new ResumenSueldosVistaDto { CarreraId = carreraId };

        var periodos = ConstruirPeriodos(proyeccion);
        var estudiantesPorPeriodo = ConstruirEstudiantesPorPeriodo(proyeccion);

        var anioBase = proyeccion.AnioBase;
        var anioMax = periodos.Max(p => p.Anio);
        var registrosInflacion = await repositorioInflacion.ListarPorRangoAsync(anioBase, anioMax, cancellationToken);

        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, cancellationToken);
        var carreraNombre = carrera?.Nombre ?? string.Empty;

        var cargos = await ObtenerCargosAsync(carreraId, cancellationToken);

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

            decimal pesoUltimoPeriodo = 0m;
            decimal personasUltimoPeriodo = 0m;
            var pesosPorPeriodo = new decimal[periodos.Count];

            for (int i = 0; i < periodos.Count; i++)
            {
                var periodo = periodos[i];
                var estCarrera = estudiantesPorPeriodo.GetValueOrDefault(periodo.PeriodoAcademicoId, 0m);
                var factor = CalculoCargosFacultad.CalcularFactorInflacionEncadenado(
                    registrosInflacion,
                    anioBase,
                    periodo.Anio,
                    periodo.NumeroPeriodo);
                var personas = ObtenerPersonasPeriodo(cargo, consolidadoActual, i);

                var parametros = new ParametrosCalculoCargoFacultadDto
                {
                    EstudiantesCarreraPeriodo = estCarrera,
                    EstudiantesUnidadAcademica = parametrosBase.EstudiantesUnidadAcademica,
                    FactorInflacion = factor,
                };

                var total = CalcularTotalSemestre(cargo, parametros, personas);
                valores[i] = total;
                totalesPorPeriodo[i] += total;
                pesosPorPeriodo[i] = CalculoCargosFacultad.CalcularPeso(
                    cargo, estCarrera, parametros.EstudiantesUnidadAcademica);

                pesoUltimoPeriodo = pesosPorPeriodo[i];
                personasUltimoPeriodo = personas;
            }

            filas.Add(new FilaResumenSueldosDto
            {
                CargoId = cargo.Id,
                NombreCargo = cargo.NombreCargo,
                EsCargoDocente = cargo.EsCargoDocente,
                Peso = pesoUltimoPeriodo,
                PesosPorPeriodo = pesosPorPeriodo,
                NumeroPersonas = personasUltimoPeriodo,
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

    private async Task<ProyeccionEstudiantesDto?> ObtenerProyeccionAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken cancellationToken)
    {
        if (carreraId <= 0 || escenarioProyeccionId <= 0)
            return null;

        var proyeccionId = await repositorioProyeccionEstudiantes.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId, escenarioProyeccionId, cancellationToken);
        if (proyeccionId is null or 0)
            return null;

        return await repositorioProyeccionEstudiantes.ObtenerDtoPorIdAsync(proyeccionId.Value, cancellationToken);
    }

    private async Task<IReadOnlyList<CargoFacultad>> ObtenerCargosAsync(
        int carreraId,
        CancellationToken cancellationToken)
    {
        var cargos = await repositorioCargo.ListarPorCarreraAsync(carreraId, cancellationToken);
        if (cargos.Count > 0)
            return cargos;

        var todas = await repositorioCarrera.ListarAsync();
        foreach (var c in todas.Where(x => x.Id != carreraId))
        {
            var alternativos = await repositorioCargo.ListarPorCarreraAsync(c.Id, cancellationToken);
            if (alternativos.Count > 0)
                return alternativos;
        }

        return cargos;
    }

    private static List<PeriodoDisponibleSueldosDto> ConstruirPeriodos(ProyeccionEstudiantesDto proyeccion)
        => proyeccion.Detalles
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
            .OrderBy(p => p.Anio)
            .ThenBy(p => p.NumeroPeriodo)
            .ToList();

    private static Dictionary<int, decimal> ConstruirEstudiantesPorPeriodo(ProyeccionEstudiantesDto proyeccion)
        => proyeccion.Detalles
            .GroupBy(d => d.PeriodoAcademicoId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TotalEstudiantes));

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

    private static decimal CalcularTotalSemestre(
        CargoFacultad cargo,
        ParametrosCalculoCargoFacultadDto parametros,
        decimal personas)
    {
        var peso = CalculoCargosFacultad.CalcularPeso(cargo, parametros.EstudiantesCarreraPeriodo, parametros.EstudiantesUnidadAcademica);

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

    private static decimal ObtenerPersonasPeriodo(
        CargoFacultad cargo,
        ProyeccionConsolidadaDto? consolidadoActual,
        int periodoIndex)
    {
        if (periodoIndex < 0)
            return cargo.TipoContrato == TipoContrato.Administrativo ? cargo.CantidadDefault : 0m;

        var tipoFila = ObtenerTipoFilaDocente(cargo);
        if (tipoFila is null)
            return cargo.CantidadDefault;

        if (consolidadoActual is null)
            return 0m;

        var fila = consolidadoActual.DocentesPorPeriodo.FirstOrDefault(x =>
            x.Tipo.Equals(tipoFila, StringComparison.OrdinalIgnoreCase));

        if (fila is null || fila.Periodos.Length <= periodoIndex)
            return 0m;

        return fila.Periodos[periodoIndex];
    }

    private static string? ObtenerTipoFilaDocente(CargoFacultad cargo)
        => cargo.TipoContrato switch
        {
            TipoContrato.PhD => "TC PhD",
            TipoContrato.Mgs => "TC Mgs.",
            TipoContrato.MedioTiempo => "Medio Tiempo",
            TipoContrato.TiempoParcial => "Tiempo Parcial",
            TipoContrato.Tecnico => "Ocasional Tipo 2 (Técnico)",
            _ => null,
        };
}
