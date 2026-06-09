using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.DemandaIngresos;
using SistemaAranceles.Application.UseCases.Inflacion;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Application.UseCases.CostosGastos;

public sealed class ObtenerMatrizInvVinBecasQuery(
    ObtenerDemandaProyectadaQuery obtenerDemandaProyectadaQuery,
    IRepositorioDatosInstitucionales repositorioDatos,
    IRepositorioCarrera repositorioCarrera,
    IRepositorioEscenarioProyeccion repositorioEscenario,
    IRepositorioInflacionAnual repositorioInflacion)
{
    public async Task<MatrizInvVinBecasDto> EjecutarAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default,
        DemandaProyectadaDto? demandaPrecalculada = null,
        IReadOnlyDictionary<int, decimal>? becasInstitucionalesPorPeriodo = null)
    {
        var carrera = await repositorioCarrera.ObtenerPorIdAsync(carreraId, ct);
        var escenario = escenarioProyeccionId is > 0
            ? await repositorioEscenario.ObtenerPorIdAsync(escenarioProyeccionId.Value, ct)
            : null;
        var advertencias = new List<string>();

        if (escenarioProyeccionId is null or <= 0)
            return Vacia(carreraId, carrera?.Nombre ?? string.Empty, escenarioProyeccionId, escenario?.Nombre ?? string.Empty, "Selecciona un escenario para calcular Inv. Vin. Becas.");

        var demanda = demandaPrecalculada
            ?? await obtenerDemandaProyectadaQuery.EjecutarAsync(carreraId, escenarioProyeccionId, ct);
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
        // KAN-44: Becas Institucionales NO es costo (es descuento al ingreso). Aquí se muestra como dato
        // REFERENCIAL el valor real que viene de Demanda/Ingresos (si el llamador lo provee); todas las
        // sumas de costo lo excluyen (CostosPorServicios/PE), así que no hay doble conteo.
        var becasInstitucionales = periodos
            .Select(p => becasInstitucionalesPorPeriodo != null
                && becasInstitucionalesPorPeriodo.TryGetValue(p.PeriodoAcademicoId, out var b)
                    ? decimal.Round(b, 2)
                    : 0m)
            .ToList();

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
