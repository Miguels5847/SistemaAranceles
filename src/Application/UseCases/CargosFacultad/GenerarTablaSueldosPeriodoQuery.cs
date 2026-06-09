using System.Globalization;
using System.Text;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Constantes;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

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
    public sealed class ContextoSueldosCarrera
    {
        public required ProyeccionEstudiantesDto Proyeccion { get; init; }
        public required string CarreraNombre { get; init; }
        public required IReadOnlyList<InflacionAnual> RegistrosInflacion { get; init; }
        public required IReadOnlyList<CargoFacultad> Cargos { get; init; }
    }

    public async Task<SueldosPeriodoVistaDto> EjecutarAsync(
        int carreraId,
        int escenarioProyeccionId,
        int periodoAcademicoId,
        decimal estudiantesUA,
        ProyeccionConsolidadaDto? consolidadoActual = null,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0 || escenarioProyeccionId <= 0 || periodoAcademicoId <= 0)
            return new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodoAcademicoId };

        var contexto = await PrepararContextoAsync(carreraId, escenarioProyeccionId, cancellationToken);
        if (contexto is null)
            return new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodoAcademicoId };

        return GenerarParaPeriodo(contexto, carreraId, periodoAcademicoId, estudiantesUA, consolidadoActual);
    }

    /// <summary>
    /// Carga (una sola vez) los datos invariantes por período: proyección, carrera, cargos e
    /// inflación del horizonte completo. Permite calcular todos los períodos en memoria sin re-consultar
    /// la base por cada período (evita N+1 al consolidar la matriz de Costos y Gastos).
    /// </summary>
    public async Task<ContextoSueldosCarrera?> PrepararContextoAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken cancellationToken = default)
    {
        if (carreraId <= 0 || escenarioProyeccionId <= 0)
            return null;

        var proyeccionId = await repositorioProyeccionEstudiantes.ObtenerIdPorCarreraYEscenarioAsync(
            carreraId,
            escenarioProyeccionId,
            cancellationToken);

        if (proyeccionId is null or 0)
            return null;

        var proyeccion = await repositorioProyeccionEstudiantes.ObtenerDtoPorIdAsync(proyeccionId.Value, cancellationToken);
        if (proyeccion is null)
            return null;

        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, cancellationToken);

        var anioBase = proyeccion.AnioBase;
        var anioMaximo = proyeccion.Detalles.Count > 0
            ? proyeccion.Detalles.Max(d => d.Anio)
            : anioBase;
        var registrosInflacion = await repositorioInflacion.ListarPorRangoAsync(anioBase, anioMaximo, cancellationToken);

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

        return new ContextoSueldosCarrera
        {
            Proyeccion = proyeccion,
            CarreraNombre = carrera?.Nombre ?? string.Empty,
            RegistrosInflacion = registrosInflacion,
            Cargos = cargos
        };
    }

    /// <summary>
    /// Calcula la tabla de sueldos de un período en memoria a partir del contexto precargado.
    /// </summary>
    public SueldosPeriodoVistaDto GenerarParaPeriodo(
        ContextoSueldosCarrera contexto,
        int carreraId,
        int periodoAcademicoId,
        decimal estudiantesUA,
        ProyeccionConsolidadaDto? consolidadoActual = null)
    {
        if (periodoAcademicoId <= 0)
            return new SueldosPeriodoVistaDto { CarreraId = carreraId, PeriodoAcademicoId = periodoAcademicoId };

        var proyeccion = contexto.Proyeccion;
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

        var factorEncadenado = CalculoCargosFacultad.CalcularFactorInflacionEncadenado(
            contexto.RegistrosInflacion,
            anioBase,
            anioPeriodo,
            numeroPeriodo);
        var inflacionPeriodoPct = ObtenerPorcentajeAnio(contexto.RegistrosInflacion, anioPeriodo);

        var parametros = new ParametrosCalculoCargoFacultadDto
        {
            EstudiantesCarreraPeriodo = estudiantesCarrera,
            EstudiantesUnidadAcademica = estudiantesUA < 0m ? 0m : estudiantesUA,
            FactorInflacion = factorEncadenado,
        };

        var periodoIndex = ObtenerIndicePeriodo(proyeccion.Detalles, periodoAcademicoId);
        var filas = contexto.Cargos
            .OrderBy(c => c.NombreCargo)
            .Select(c => Calcular(c, parametros, consolidadoActual, periodoIndex))
            .ToList();

        return new SueldosPeriodoVistaDto
        {
            CarreraId = carreraId,
            CarreraNombre = contexto.CarreraNombre,
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

    private static FilaSueldoPeriodoDto Calcular(
        CargoFacultad cargo,
        ParametrosCalculoCargoFacultadDto parametros,
        ProyeccionConsolidadaDto? consolidadoActual,
        int periodoIndex)
    {
        var peso = CalculoCargosFacultad.CalcularPeso(
            cargo,
            parametros.EstudiantesCarreraPeriodo,
            parametros.EstudiantesUnidadAcademica);

        var personas = ObtenerPersonasPeriodo(cargo, consolidadoActual, periodoIndex);

        // CU-SP-02 RN-79b: TP se paga por hora sin beneficios sociales.
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

    private static int ObtenerIndicePeriodo(IReadOnlyList<DetalleProyeccionEstudiantesDto> detalles, int periodoAcademicoId)
    {
        // El consolidador indexa por NumeroPeriodo: período N → índice (N-1)
        // NO por posición en la lista de detalles (que puede tener períodos faltantes)
        var detalle = detalles.FirstOrDefault(d => d.PeriodoAcademicoId == periodoAcademicoId);
        return detalle is null ? -1 : detalle.NumeroPeriodo - 1;
    }

    private static decimal ObtenerPersonasPeriodo(
        CargoFacultad cargo,
        ProyeccionConsolidadaDto? consolidadoActual,
        int periodoIndex)
    {
        if (cargo.EsCargoDocente)
        {
            if (periodoIndex < 0 || consolidadoActual is null)
                return 0m;

            if (cargo.TipoContrato == TipoContrato.Tecnico || EsOcasionalTipo2(cargo.NombreCargo))
                return ObtenerEquivalenteTecnicoPeriodo(consolidadoActual, periodoIndex);

            var tipoFila = ObtenerTipoFilaDocente(cargo);
            if (tipoFila is null)
                return 0m;

            var fila = consolidadoActual.DocentesPorPeriodo.FirstOrDefault(x =>
                x.Tipo.Equals(tipoFila, StringComparison.OrdinalIgnoreCase));

            if (fila is null || fila.Periodos.Length <= periodoIndex)
                return 0m;

            return fila.Periodos[periodoIndex];
        }

        if (periodoIndex < 0)
            return cargo.CantidadDefault;

        if (consolidadoActual is null)
            return cargo.CantidadDefault;

        return cargo.CantidadDefault;
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

    private static decimal ObtenerEquivalenteTecnicoPeriodo(
        ProyeccionConsolidadaDto consolidadoActual,
        int periodoIndex)
    {
        if (periodoIndex < 0)
            return 0m;

        var horasTecnico = consolidadoActual.HorasTecnicoSemana > 0m
            ? consolidadoActual.HorasTecnicoSemana
            : 40m;

        var filaHorasPractica = consolidadoActual.TablaHoras.FirstOrDefault(f =>
        {
            var etiqueta = NormalizarTexto(f.Etiqueta);
            return etiqueta.Contains("practica", StringComparison.Ordinal)
                   && etiqueta.Contains("acumuladas", StringComparison.Ordinal);
        });

        if (filaHorasPractica is null || filaHorasPractica.Valores.Length <= periodoIndex)
            return 0m;

        var horasPracticaAcumuladas = filaHorasPractica.Valores[periodoIndex];
        return horasPracticaAcumuladas <= 0m
            ? 0m
            : Math.Round(horasPracticaAcumuladas / horasTecnico, 4);
    }

    private static bool EsOcasionalTipo2(string nombreCargo)
    {
        var normalizado = NormalizarTexto(nombreCargo);
        return normalizado.Contains("ocasional tipo 2", StringComparison.Ordinal)
               || normalizado.Contains("tecnico docente", StringComparison.Ordinal);
    }

    private static string NormalizarTexto(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return string.Empty;

        var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        var separadorPendiente = false;

        foreach (var c in descompuesto)
        {
            var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
            if (categoria == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c))
            {
                if (separadorPendiente && sb.Length > 0)
                    sb.Append(' ');

                sb.Append(c);
                separadorPendiente = false;
            }
            else
            {
                separadorPendiente = true;
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
