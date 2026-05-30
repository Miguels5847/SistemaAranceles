using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.UseCases.CostosGastos;

public sealed class ObtenerMatrizInvVinBecasQuery(
    ObtenerDemandaProyectadaQuery obtenerDemandaProyectadaQuery,
    // Lazy rompe el ciclo de construcción en el contenedor DI:
    // InvVinBecas -> Ingresos -> ArancelEfectivo -> ArancelOptimo -> CostoCarrera -> CostosGastos -> InvVinBecas.
    // En tiempo de ejecución solo se invoca en modo Manual (sin recursión); en Automático se omite.
    Lazy<CalcularIngresosProyectadosQuery> calcularIngresosProyectadosQuery,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioInflacionAnual repositorioInflacion,
    IRepositorioConfiguracionArancelCarrera repositorioConfiguracionArancel)
{
    public async Task<MatrizInvVinBecasDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;
        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
            return Vacia(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId, escenario?.Nombre ?? string.Empty, "Selecciona un escenario para calcular Inv. Vin. Becas.");

        var demanda = await obtenerDemandaProyectadaQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertencia);
        AgregarAdvertencia(advertencias, demanda.MensajeAdvertenciaDocentes);

        if (!demanda.TieneDatos || demanda.EtiquetasPeriodos.Count == 0)
            return Vacia(carreraId, carrera?.Nombre ?? demanda.CarreraNombre, escenarioProyeccionId, demanda.EscenarioNombre, ConstruirMensaje(advertencias, "No hay demanda proyectada para consolidar Inv. Vin. Becas."));

        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        if (datos is null)
        {
            advertencias.Add("No hay Datos Institucionales vigentes. Se usaron parámetros de Costos y Gastos en cero/default.");
            datos = CrearDatosInstitucionalesFallback();
        }

        var periodos = ConstruirPeriodos(demanda);
        var factores = await CalcularFactoresInflacionAsync(periodos, datos.SemestresPorAnio, advertencias, ct);
        var inflacionesAnualesPorPeriodo = await ObtenerInflacionAnualPorPeriodoAsync(periodos, advertencias, ct);
        var estudiantes = CompletarValores(demanda.TotalesPorPeriodo, periodos.Count);
        var docentes = ObtenerDocentesRequeridosPorPeriodo(demanda, periodos.Count);

        if (datos.NumeroEstudiantesUniversidad <= 0)
            advertencias.Add("Datos Institucionales: número de estudiantes universidad debe ser > 0.");
        if (datos.NumeroDocentesUniversidad <= 0)
            advertencias.Add("Datos Institucionales: número de docentes universidad debe ser > 0.");
        if (datos.PresupuestoBaseUniversidad <= 0m)
            advertencias.Add("Presupuesto base universidad está en 0; Investigación y Vinculación quedan en 0.");
        if (datos.PresupuestoGobiernoBecas <= 0m)
            advertencias.Add("Presupuesto gobierno becas está en 0; Becas Gobierno quedan en 0.");

        var semestresPorAnio = datos.SemestresPorAnio > 0 ? datos.SemestresPorAnio : DatosInstitucionales.SemestresPorAnioPorDefecto;
        var becasInstitucionales = await ObtenerBecasInstitucionalesPorPeriodoAsync(
            carreraId, escenarioProyeccionId, periodos, advertencias, ct);

        var valores = new List<InvVinBecasPeriodoDto>();
        for (var i = 0; i < periodos.Count; i++)
        {
            var presupuestoUniversidad = decimal.Round(datos.PresupuestoBaseUniversidad / semestresPorAnio * factores[i], 2);
            var presupuestoGobierno = decimal.Round(datos.PresupuestoGobiernoBecas / semestresPorAnio * factores[i], 2);
            var investigacion = datos.NumeroEstudiantesUniversidad > 0
                ? decimal.Round(presupuestoUniversidad * datos.PorcentajeInvestigacion / 100m / datos.NumeroEstudiantesUniversidad * estudiantes[i], 2)
                : 0m;
            var vinculacion = datos.NumeroEstudiantesUniversidad > 0
                ? decimal.Round(presupuestoUniversidad * datos.PorcentajeVinculacion / 100m / datos.NumeroEstudiantesUniversidad * estudiantes[i], 2)
                : 0m;
            var becasEstudiantes = datos.NumeroEstudiantesUniversidad > 0 && presupuestoGobierno > 0m
                ? decimal.Round(presupuestoGobierno * datos.PorcentajeBecasEstudiantes / 100m / datos.NumeroEstudiantesUniversidad * estudiantes[i], 2)
                : 0m;
            var becasDocentes = datos.NumeroDocentesUniversidad > 0 && presupuestoGobierno > 0m
                ? decimal.Round(presupuestoGobierno * datos.PorcentajeBecasDocentes / 100m / datos.NumeroDocentesUniversidad * docentes[i], 2)
                : 0m;

            valores.Add(new InvVinBecasPeriodoDto
            {
                PeriodoAcademicoId = periodos[i].PeriodoAcademicoId,
                Anio = periodos[i].Anio,
                NumeroPeriodo = periodos[i].NumeroPeriodo,
                EtiquetaPeriodo = periodos[i].Etiqueta,
                EstudiantesCarrera = estudiantes[i],
                DocentesCarrera = docentes[i],
                FactorInflacion = factores[i],
                InflacionAnual = inflacionesAnualesPorPeriodo[i],
                BecasInstitucionales = becasInstitucionales[i],
                PresupuestoUniversidad = presupuestoUniversidad,
                NumeroEstudiantesUniversidad = datos.NumeroEstudiantesUniversidad,
                Investigacion = investigacion,
                Vinculacion = vinculacion,
                PresupuestoGobierno = presupuestoGobierno,
                NumeroDocentesUniversidad = datos.NumeroDocentesUniversidad,
                BecasEstudiantes = becasEstudiantes,
                BecasDocentes = becasDocentes
            });
        }

        return new MatrizInvVinBecasDto
        {
            CarreraId = carreraId,
            CarreraNombre = carrera?.Nombre ?? demanda.CarreraNombre,
            EscenarioProyeccionId = escenarioProyeccionId,
            EscenarioNombre = escenario?.Nombre ?? demanda.EscenarioNombre,
            Periodos = periodos,
            ValoresPorPeriodo = valores,
            Filas = ConstruirFilas(valores),
            MensajeAdvertencia = ConstruirMensaje(advertencias)
        };
    }

    /// <summary>
    /// Becas Institucionales por período = misma serie que Demanda e Ingresos → Ingresos Proyectados
    /// (suma de Becas de todos los ciclos por período). Si el arancel está en modo Automático Costo
    /// Carrera, no se invoca Ingresos Proyectados para evitar recursión con Costo de la Carrera.
    /// </summary>
    private async Task<IReadOnlyList<decimal>> ObtenerBecasInstitucionalesPorPeriodoAsync(
        int carreraId,
        int? escenarioProyeccionId,
        IReadOnlyList<PeriodoCostoGastoDto> periodos,
        List<string> advertencias,
        CancellationToken ct)
    {
        var ceros = Enumerable.Repeat(0m, periodos.Count).ToList();

        // Solo se consulta la configuración para detectar el modo Automático Costo Carrera y evitar
        // la recursión con Costo de la Carrera. La existencia/validez del arancel la decide Ingresos Proyectados.
        var configuracion = await repositorioConfiguracionArancel.ObtenerPorCarreraEscenarioAsync(carreraId, escenarioProyeccionId, ct);
        if (configuracion is null && escenarioProyeccionId is not null)
            configuracion = await repositorioConfiguracionArancel.ObtenerPorCarreraEscenarioAsync(carreraId, null, ct);

        if (configuracion is not null)
        {
            var modo = Enum.TryParse<ModoCalculoArancel>(configuracion.ModoCalculoArancel, ignoreCase: true, out var m)
                ? m
                : ModoCalculoArancel.Manual;
            if (modo == ModoCalculoArancel.AutomaticoCostoCarrera)
            {
                advertencias.Add("Becas institucionales no se calcularon porque el arancel está en modo Automático Costo Carrera y depende del propio costo consolidado.");
                return ceros;
            }
        }

        var ingresos = await calcularIngresosProyectadosQuery.Value.EjecutarAsync(carreraId, escenarioProyeccionId, ct);

        if (ingresos.ArancelEfectivo <= 0m)
        {
            advertencias.Add("No se pudo calcular Becas Institucionales porque no existe arancel efectivo para la carrera/escenario.");
            AgregarAdvertencia(advertencias, ingresos.MensajeAdvertencia);
            return ceros;
        }

        if (ingresos.CeldasPlanas.Count == 0)
        {
            advertencias.Add("No se pudo calcular Becas Institucionales porque no hay ingresos proyectados para esta carrera/escenario.");
            AgregarAdvertencia(advertencias, ingresos.MensajeAdvertencia);
            return ceros;
        }

        return periodos
            .Select(periodo => decimal.Round(
                ingresos.CeldasPlanas
                    .Where(c => c.PeriodoAcademicoId == periodo.PeriodoAcademicoId)
                    .Sum(c => c.Becas),
                2))
            .ToList();
    }

    private async Task<IReadOnlyList<decimal>> CalcularFactoresInflacionAsync(
        IReadOnlyList<PeriodoCostoGastoDto> periodos,
        int semestresPorAnio,
        List<string> advertencias,
        CancellationToken ct)
    {
        if (periodos.Count == 0)
            return [];

        var anioBase = periodos.Min(p => p.Anio);
        var anioMaximo = periodos.Max(p => p.Anio);
        var registros = await repositorioInflacion.ListarPorRangoAsync(anioBase, anioMaximo, ct);
        var aniosConInflacion = registros.Select(r => r.Anio).ToHashSet();
        var aniosSinInflacion = ObtenerAniosInflacionNecesarios(periodos, anioBase, semestresPorAnio)
            .Where(anio => !aniosConInflacion.Contains(anio))
            .Distinct()
            .OrderBy(anio => anio)
            .ToList();

        if (aniosSinInflacion.Count > 0)
            advertencias.Add($"No hay inflación registrada para el año {string.Join(", ", aniosSinInflacion)}; se usó factor 1.");

        return periodos
            .Select(p => CalculoInflacionAplicada.CalcularFactorPeriodo(
                registros,
                anioBase,
                p.Anio,
                NumeroPeriodoEnAnio(p.NumeroPeriodo, semestresPorAnio)))
            .ToList();
    }

    private async Task<IReadOnlyList<decimal>> ObtenerInflacionAnualPorPeriodoAsync(
        IReadOnlyList<PeriodoCostoGastoDto> periodos,
        List<string> advertencias,
        CancellationToken ct)
    {
        if (periodos.Count == 0)
            return [];

        var anioBase = periodos.Min(p => p.Anio);
        var anioMaximo = periodos.Max(p => p.Anio);
        var registros = await repositorioInflacion.ListarPorRangoAsync(anioBase, anioMaximo, ct);
        var inflacionPorAnio = registros
            .GroupBy(r => r.Anio)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(r => r.TipoFuente).First().PorcentajeInflacion);

        var aniosFaltantes = periodos
            .Select(p => p.Anio)
            .Distinct()
            .Where(anio => !inflacionPorAnio.ContainsKey(anio))
            .OrderBy(anio => anio)
            .ToList();

        foreach (var anio in aniosFaltantes)
            advertencias.Add($"No hay inflación registrada para el año {anio}; se mostró 0 en la fila de inflación anual.");

        return periodos
            .Select(p => inflacionPorAnio.TryGetValue(p.Anio, out var inflacion) ? inflacion : 0m)
            .ToList();
    }

    public static IReadOnlyList<PeriodoCostoGastoDto> ConstruirPeriodos(DemandaProyectadaDto demanda)
    {
        var total = demanda.EtiquetasPeriodos.Count;
        var periodos = new List<PeriodoCostoGastoDto>(total);
        for (var i = 0; i < total; i++)
        {
            periodos.Add(new PeriodoCostoGastoDto
            {
                PeriodoAcademicoId = i < demanda.PeriodoAcademicoIds.Count ? demanda.PeriodoAcademicoIds[i] : 0,
                Etiqueta = demanda.EtiquetasPeriodos[i],
                Anio = i < demanda.AniosPeriodos.Count ? demanda.AniosPeriodos[i] : 0,
                NumeroPeriodo = i < demanda.NumerosPeriodos.Count ? demanda.NumerosPeriodos[i] : i + 1
            });
        }

        return periodos;
    }

    public static IReadOnlyList<decimal> ObtenerDocentesRequeridosPorPeriodo(DemandaProyectadaDto demanda, int cantidadPeriodos)
    {
        var fila = demanda.DocentesPorPeriodo.FirstOrDefault(f =>
            string.Equals(f.Tipo, "Docentes Requeridos", StringComparison.OrdinalIgnoreCase));

        return CompletarValores(fila?.Periodos ?? [], cantidadPeriodos);
    }

    public static IReadOnlyList<decimal> CompletarValores(IReadOnlyList<decimal> valoresOrigen, int cantidadPeriodos)
    {
        var valores = new List<decimal>(cantidadPeriodos);
        for (var i = 0; i < cantidadPeriodos; i++)
            valores.Add(i < valoresOrigen.Count ? valoresOrigen[i] : 0m);

        return valores;
    }

    private const string GrupoInvVinBecas = "Inv. Vin. Becas";

    private static IReadOnlyList<CostoGastoRubroDto> ConstruirFilas(IReadOnlyList<InvVinBecasPeriodoDto> valores)
    {
        return
        [
            CrearFila(GrupoInvVinBecas, "Total Nº de Estudiantes", valores.Select(v => v.EstudiantesCarrera).ToList(), FormatoMatrizCostosGastos.Entero),
            CrearFila(GrupoInvVinBecas, "Becas Institucionales", valores.Select(v => v.BecasInstitucionales).ToList()),
            CrearFila(GrupoInvVinBecas, "Inflación anual / % Variación Presupuesto", valores.Select(v => v.InflacionAnual).ToList(), FormatoMatrizCostosGastos.Decimal),
            CrearFila(GrupoInvVinBecas, "Presupuesto Universidad", valores.Select(v => v.PresupuestoUniversidad).ToList()),
            CrearFila(GrupoInvVinBecas, "Nº Estudiantes Uni", valores.Select(v => v.NumeroEstudiantesUniversidad).ToList(), FormatoMatrizCostosGastos.Entero),
            CrearFila(GrupoInvVinBecas, "Investigación 5%", valores.Select(v => v.Investigacion).ToList()),
            CrearFila(GrupoInvVinBecas, "Vinculación 1%", valores.Select(v => v.Vinculacion).ToList()),
            CrearFila(GrupoInvVinBecas, "Inflación anual / % Variación Presupuesto Gobierno", valores.Select(v => v.InflacionAnual).ToList(), FormatoMatrizCostosGastos.Decimal),
            CrearFila(GrupoInvVinBecas, "Presupuesto Gobierno", valores.Select(v => v.PresupuestoGobierno).ToList()),
            CrearFila(GrupoInvVinBecas, "Nº Docentes Universidad", valores.Select(v => v.NumeroDocentesUniversidad).ToList(), FormatoMatrizCostosGastos.Entero),
            CrearFila(GrupoInvVinBecas, "Becas Estudiantes 90%", valores.Select(v => v.BecasEstudiantes).ToList()),
            CrearFila(GrupoInvVinBecas, "Becas Docentes 10%", valores.Select(v => v.BecasDocentes).ToList()),
            CrearFila(GrupoInvVinBecas, "Total Inv. Vin. Becas", valores.Select(v => v.TotalInvVinBecas).ToList(), esTotal: true)
        ];
    }

    private static CostoGastoRubroDto CrearFila(
        string grupo,
        string concepto,
        IReadOnlyList<decimal> periodos,
        string formato = FormatoMatrizCostosGastos.Moneda,
        bool esTotal = false)
        => new()
        {
            Grupo = grupo,
            Concepto = concepto,
            Periodos = periodos.Select(v => decimal.Round(v, 2)).ToList(),
            Total = decimal.Round(periodos.Sum(), 2),
            FormatoValor = formato,
            EsTotal = esTotal
        };

    private static IReadOnlyList<int> ObtenerAniosInflacionNecesarios(
        IReadOnlyList<PeriodoCostoGastoDto> periodos,
        int anioBase,
        int semestresPorAnio)
    {
        var resultado = new SortedSet<int>();
        foreach (var periodo in periodos)
        {
            var numeroPeriodoEnAnio = NumeroPeriodoEnAnio(periodo.NumeroPeriodo, semestresPorAnio);
            for (var anio = anioBase; anio <= periodo.Anio; anio++)
            {
                if (anio < periodo.Anio || numeroPeriodoEnAnio >= 2)
                    resultado.Add(anio);
            }
        }

        return resultado.ToList();
    }

    private static int NumeroPeriodoEnAnio(int numeroPeriodo, int semestresPorAnio)
    {
        var periodosPorAnio = semestresPorAnio <= 0 ? 2 : semestresPorAnio;
        return numeroPeriodo <= 0 ? 1 : ((numeroPeriodo - 1) % periodosPorAnio) + 1;
    }

    private static DatosInstitucionales CrearDatosInstitucionalesFallback()
    {
        var datos = new DatosInstitucionales(
            DateTime.Now.Year.ToString(),
            1,
            1,
            0,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            1,
            null);
        datos.CambiarParametrosCostosGastos(
            DatosInstitucionales.PresupuestoBaseUniversidadPorDefecto,
            DatosInstitucionales.PresupuestoGobiernoBecasPorDefecto,
            DatosInstitucionales.PorcentajeInvestigacionPorDefecto,
            DatosInstitucionales.PorcentajeVinculacionPorDefecto,
            DatosInstitucionales.PorcentajeBecasEstudiantesPorDefecto,
            DatosInstitucionales.PorcentajeBecasDocentesPorDefecto);
        return datos;
    }

    private static void AgregarAdvertencia(List<string> advertencias, string? mensaje)
    {
        if (!string.IsNullOrWhiteSpace(mensaje))
            advertencias.Add(mensaje.Trim());
    }

    private static string? ConstruirMensaje(List<string> advertencias, string? adicional = null)
    {
        if (!string.IsNullOrWhiteSpace(adicional))
            advertencias.Add(adicional);

        return advertencias.Count == 0
            ? null
            : string.Join(" ", advertencias.Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static MatrizInvVinBecasDto Vacia(
        int carreraId,
        string carreraNombre,
        int? escenarioId,
        string escenarioNombre,
        string? mensaje)
        => new()
        {
            CarreraId = carreraId,
            CarreraNombre = carreraNombre,
            EscenarioProyeccionId = escenarioId,
            EscenarioNombre = escenarioNombre,
            MensajeAdvertencia = mensaje
        };
}
