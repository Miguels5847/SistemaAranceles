using SistemaAranceles.Application.DTOs.Estudiantes;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

/// <summary>
/// Replica la lógica de la hoja "1 Estudiantes" de la Matriz Financiera.
///
/// TABLA DOCENTES — reglas Excel:
///   fila16[p]  = horas docencia asistida SEMESTRAL por período (input editable, amarillo)
///   fila20[p]  = horas aplicación práctica SEMESTRAL por período (input editable, amarillo)
///   DocHBase[p] = fila16[p] / semanas * paralelos[p]   → h/semana acumulables
///   TecHBase[p] = fila20[p] / semanas * paralelos[p]
///   acumDoc[p]  = Σ(q=1..p) DocHBase[q]                → fila 15 acumulado
///   acumTec[p]  = Σ(q=1..p) TecHBase[q]                → fila 19 acumulado
///   docTotal[p] = Ceiling(acumDoc[p] / horasDocSemana)  → personas enteras (J24=18)
///   tecTotal[p] = Ceiling(acumTec[p] / horasTecSemana)  → personas enteras (J30=40)
///   tcMgs[p]    = Ceiling(docTotal[p] * 0.60)
///   phd[p]      = Ceiling(tcMgs[p]    * 0.40)
///   tParcial[p] = docTotal[p] - tcMgs[p] - phd[p]
/// </summary>
public static class ConsolidadorProyeccionEstudiantes
{
    // ── Valores por defecto (editables en la UI) ─────────────────────────
    private const decimal HorasDocSemanaDefault = 18m;
    private const decimal HorasTecSemanaDefault = 40m;
    private const decimal PctMgs = 0.60m;
    private const decimal PctPhd = 0.40m; // sobre TC Mgs → 24 % del total

    // Fila 16: horas docencia asistida SEMESTRAL por período (valores del Excel)
    private static readonly decimal[] DocHorasSemestralesDefault =
        [288m, 336m, 320m, 336m, 336m, 320m, 304m, 256m];

    // Fila 20: horas aplicación práctica SEMESTRAL por período
    private static readonly decimal[] TecHorasSemestralesDefault =
        [160m, 176m, 192m, 208m, 240m, 264m, 336m, 400m];

    // ─────────────────────────────────────────────────────────────────────

    public static ProyeccionConsolidadaDto Calcular(
        ProyeccionEstudiantesDto proyeccion,
        int     paralelosPeriodo1,
        int     paralelosPeriodo2,
        decimal tasaRetencionMeta,
        decimal tasaGraduacionMeta,
        decimal[]? horasDocSemestralesOverride = null,
        decimal[]? horasTecSemestralesOverride = null,
        decimal    horasDocSemanaOverride       = HorasDocSemanaDefault,
        decimal    horasTecSemanaOverride        = HorasTecSemanaDefault)
    {
        var sem          = proyeccion.SemanasPorSemestre;
        var detalles     = proyeccion.Detalles ?? [];
        var totalPeriodos = detalles.Select(d => d.NumeroPeriodo).DefaultIfEmpty(0).Max();
        var totalCiclos   = detalles.Select(d => d.NumeroCiclo).DefaultIfEmpty(0).Max();

        // Inputs efectivos (override > default)
        var docSemestrales = horasDocSemestralesOverride ?? DocHorasSemestralesDefault;
        var tecSemestrales = horasTecSemestralesOverride ?? TecHorasSemestralesDefault;
        var hDoc = horasDocSemanaOverride;
        var hTec = horasTecSemanaOverride;

        var anios = totalPeriodos > 0
            ? Enumerable.Range(0, (totalPeriodos + 1) / 2)
                        .Select(i => proyeccion.AnioBase + i)
                        .ToList()
            : [];

        // Paralelos 0-based por período
        var paralelosPorPeriodo = Enumerable.Range(1, totalPeriodos)
            .Select(p => Par(p, paralelosPeriodo1, paralelosPeriodo2))
            .ToArray();

        // ── Acumulados semanales (fila 15 y fila 19 del Excel) ───────────
        // DocHBase[p] = fila16[p] / sem * paralelos[p]
        // acumDoc[p]  = acumDoc[p-1] + DocHBase[p]
        var acumDocH = new decimal[totalPeriodos + 1];
        var acumTecH = new decimal[totalPeriodos + 1];

        for (var p = 1; p <= totalPeriodos; p++)
        {
            var par  = paralelosPorPeriodo[p - 1];
            var i    = p - 1;
            var dBase = GetD(docSemestrales, i) / sem * par;
            var tBase = GetD(tecSemestrales, i) / sem * par;
            acumDocH[p] = acumDocH[p - 1] + dBase;
            acumTecH[p] = acumTecH[p - 1] + tBase;
        }

        // ── Tabla 5: consumo por período ─────────────────────────────────
        var tablaPeriodos = new List<FilaConsumoPeriodicDto>(totalPeriodos);
        decimal totalHDocAcum = 0, totalHTecAcum = 0;

        for (var p = 1; p <= totalPeriodos; p++)
        {
            var i    = p - 1;
            var hDocPer = R(GetD(docSemestrales, i));
            var hTecPer = R(GetD(tecSemestrales, i));
            totalHDocAcum += hDocPer;
            totalHTecAcum += hTecPer;

            tablaPeriodos.Add(new FilaConsumoPeriodicDto
            {
                Periodo      = p,
                Anio         = AnioDePeriodo(proyeccion.AnioBase, p),
                Semestre     = p % 2 == 1 ? "ABR" : "SEP",
                // Docentes/Técnicos en tabla 5 = fraccionarios (para información)
                Docentes     = Rd(acumDocH[p] / hDoc),
                Tecnicos     = Rd(acumTecH[p] / hTec, 3),
                HorasDocencia = hDocPer,
                HorasPractica = hTecPer
            });
        }

        // ── Tabla 1 NUEVA: matrícula por PERÍODO ─────────────────────────
        var filasPeriodo = new List<FilaMatriculaPeriodoDto>(totalCiclos + 1);

        for (var ciclo = 1; ciclo <= totalCiclos; ciclo++)
        {
            var vals = Enumerable.Range(1, totalPeriodos)
                .Select(p =>
                {
                    if (ciclo > p) return 0m;
                    return R(detalles
                        .Where(d => d.NumeroCiclo == ciclo && d.NumeroPeriodo == p)
                        .Sum(d => d.TotalEstudiantes));
                })
                .ToArray();

            filasPeriodo.Add(new FilaMatriculaPeriodoDto
            {
                Ciclo    = $"{ciclo} CICLO",
                Periodos = vals,
                Total    = R(vals.Sum())
            });
        }

        var totalesPorPeriodo = Enumerable.Range(0, totalPeriodos)
            .Select(i => R(filasPeriodo.Sum(f => f.Periodos[i])))
            .ToArray();

        filasPeriodo.Add(new FilaMatriculaPeriodoDto
        {
            Ciclo    = "Total Nº de Estudiantes",
            Periodos = totalesPorPeriodo,
            Total    = R(totalesPorPeriodo.Sum())
        });

        // ── Tabla 1 LEGADA: matrícula por AÑO ───────────────────────────
        var filasMatricula = new List<FilaMatriculaAnualDto>(totalCiclos + 1);

        for (var ciclo = 1; ciclo <= totalCiclos; ciclo++)
        {
            var vals = anios
                .Select(anio => detalles
                    .Where(d => d.NumeroCiclo == ciclo &&
                                AnioDePeriodo(proyeccion.AnioBase, d.NumeroPeriodo) == anio)
                    .Sum(d => d.TotalEstudiantes))
                .ToList();

            filasMatricula.Add(new FilaMatriculaAnualDto
            {
                Ciclo = $"Ciclo {ciclo}",
                Anio1 = R(GetList(vals, 0)),
                Anio2 = R(GetList(vals, 1)),
                Anio3 = R(GetList(vals, 2)),
                Anio4 = R(GetList(vals, 3)),
                Total = R(vals.Sum())
            });
        }

        filasMatricula.Add(new FilaMatriculaAnualDto
        {
            Ciclo = "TOTAL",
            Anio1 = R(filasMatricula.Sum(f => f.Anio1)),
            Anio2 = R(filasMatricula.Sum(f => f.Anio2)),
            Anio3 = R(filasMatricula.Sum(f => f.Anio3)),
            Anio4 = R(filasMatricula.Sum(f => f.Anio4)),
            Total = R(filasMatricula.Sum(f => f.Total))
        });

        // ── Tabla 2 NUEVA: docentes/técnicos por PERÍODO ─────────────────
        // FIX: usa Ceiling → no existen 1.04 docentes, siempre enteros.
        // Estructura: una columna por período (igual que matrícula).
        //
        //  Fila: Total Docentes   = Ceiling(acumDoc[p] / J24)
        //  Fila: TC Mgs (60 %)    = Ceiling(docTotal * 0.60)
        //  Fila: TC PhD (24 %)    = Ceiling(tcMgs    * 0.40)
        //  Fila: T. Parcial(16 %) = docTotal - tcMgs - phd
        //  Fila: Técnico Docente  = Ceiling(acumTec[p] / J30)

        var docTotalPorPeriodo  = new int[totalPeriodos];
        var tcMgsPorPeriodo     = new int[totalPeriodos];
        var phdPorPeriodo       = new int[totalPeriodos];
        var tParcialPorPeriodo  = new int[totalPeriodos];
        var tecPorPeriodo       = new int[totalPeriodos];

        for (var p = 1; p <= totalPeriodos; p++)
        {
            var i    = p - 1;
            var dTot = Ceil(acumDocH[p] / hDoc);
            var mgs  = Ceil(dTot * PctMgs);
            var phd  = Ceil(mgs  * PctPhd);
            var par  = dTot - mgs - phd;
            var tec  = Ceil(acumTecH[p] / hTec);

            docTotalPorPeriodo[i] = dTot;
            tcMgsPorPeriodo[i]    = mgs;
            phdPorPeriodo[i]      = phd;
            tParcialPorPeriodo[i] = par;
            tecPorPeriodo[i]      = tec;
        }

        var filasDocentesPeriodo = new List<FilaDocentePeriodoDto>
        {
            new() { Tipo = "TC PhD (24 %)",       Periodos = phdPorPeriodo,      Total = phdPorPeriodo.Max()      },
            new() { Tipo = "TC Mgs (60 %)",        Periodos = tcMgsPorPeriodo,    Total = tcMgsPorPeriodo.Max()    },
            new() { Tipo = "T. Parcial (16 %)",    Periodos = tParcialPorPeriodo, Total = tParcialPorPeriodo.Max() },
            new() { Tipo = "Técnico Docente",       Periodos = tecPorPeriodo,      Total = tecPorPeriodo.Max()      },
            new() { Tipo = "TOTAL Docentes",        Periodos = docTotalPorPeriodo, Total = docTotalPorPeriodo.Max() }
        };

        // ── Tabla 2 LEGADA: docentes por AÑO (compatibilidad) ───────────
        var docAnio = anios
            .Select(anio => tablaPeriodos
                .Where(t => t.Anio == anio)
                .Max(t => t.Docentes))       // máximo del año, no suma
            .ToList();

        var tecAnio = anios
            .Select(anio => tablaPeriodos
                .Where(t => t.Anio == anio)
                .Max(t => t.Tecnicos))
            .ToList();

        var parcPct = 1m - PctMgs - PctMgs * PctPhd;

        var filasDocentes = new List<FilaDocenteAnualDto>
        {
            FilaDoc("TC PhD (24 %)",     docAnio, tecAnio, d => R(d * PctMgs * PctPhd)),
            FilaDoc("TC Mgs (60 %)",     docAnio, tecAnio, d => R(d * PctMgs)),
            FilaDoc("T. Parcial (16 %)", docAnio, tecAnio, d => R(d * parcPct)),
            FilaDoc("Técnico",           docAnio, tecAnio, _ => 0m, t => R(t, 3)),
            FilaDoc("TOTAL Docentes",    docAnio, tecAnio, d => R(d))
        };

        // ── Indicadores ───────────────────────────────────────────────────
        var p1Total   = detalles.Where(d => d.NumeroPeriodo == 1).Sum(d => d.TotalEstudiantes);
        var pFinalTot = detalles.Where(d => d.NumeroPeriodo == totalPeriodos).Sum(d => d.TotalEstudiantes);
        var ingresados = detalles
            .Where(d => d.NumeroCiclo == 1 && d.NumeroPeriodo % 2 == 1)
            .Sum(d => d.TotalEstudiantes);

        var indicadores = new IndicadoresProyeccionDto
        {
            TotalIngresados       = R(ingresados),
            TituladosEsperados    = R(ingresados * tasaGraduacionMeta / 100m),
            MatriculaPeriodo1     = R(p1Total),
            MatriculaPeriodoFinal = R(pFinalTot),
            CrecimientoMatricula  = p1Total > 0 ? R(pFinalTot / p1Total) : 0m,
            TasaRetencionMeta     = tasaRetencionMeta,
            TasaGraduacionMeta    = tasaGraduacionMeta
        };

        return new ProyeccionConsolidadaDto
        {
            LabelAnio1 = anios.Count > 0 ? anios[0].ToString() : "—",
            LabelAnio2 = anios.Count > 1 ? anios[1].ToString() : "—",
            LabelAnio3 = anios.Count > 2 ? anios[2].ToString() : "—",
            LabelAnio4 = anios.Count > 3 ? anios[3].ToString() : "—",
            ParalelosPorPeriodo     = paralelosPorPeriodo,
            MatriculaPorPeriodo     = filasPeriodo,
            MatriculaPorAnio        = filasMatricula,
            DocentesPorPeriodo      = filasDocentesPeriodo,
            DocentesPorAnio         = filasDocentes,
            TotalHorasDocencia      = R(totalHDocAcum),
            TotalHorasPractica      = R(totalHTecAcum),
            TotalHorasCombinado     = R(totalHDocAcum + totalHTecAcum),
            Indicadores             = indicadores,
            TablaPeriodos           = tablaPeriodos,
            HorasDocenciaSemestral  = docSemestrales,
            HorasPracticaSemestral  = tecSemestrales,
            HorasDocenteSemana      = hDoc,
            HorasTecnicoSemana      = hTec
        };
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static int  Par(int p, int par1, int par2) => p % 2 == 1 ? par1 : par2;

    private static int  AnioDePeriodo(int anioBase, int p) => anioBase + (p - 1) / 2;

    private static decimal GetD(decimal[] arr,  int i) => i < arr.Length  ? arr[i]  : arr[^1];
    private static decimal GetList(IList<decimal> l, int i) => i < l.Count ? l[i] : 0m;

    /// <summary>Redondeo normal para valores informativos/horas.</summary>
    private static decimal R(decimal v, int dec = 2) => decimal.Round(v, dec);
    private static decimal Rd(decimal v, int dec = 2) => decimal.Round(v, dec);

    /// <summary>Ceiling a entero — docentes son PERSONAS, no fracciones.</summary>
    private static int Ceil(decimal v) => (int)Math.Ceiling(v);

    private static FilaDocenteAnualDto FilaDoc(
        string tipo,
        IList<decimal> docAnio,
        IList<decimal> tecAnio,
        Func<decimal, decimal> docFn,
        Func<decimal, decimal>? tecFn = null)
    {
        bool esTec = tecFn is not null;
        decimal Val(int idx) => esTec
            ? tecFn!(GetList(tecAnio, idx))
            : docFn(GetList(docAnio, idx));
        decimal total = esTec
            ? tecFn!(tecAnio.Sum())
            : docFn(docAnio.Sum());

        return new FilaDocenteAnualDto
        {
            Tipo  = tipo,
            Anio1 = Val(0),
            Anio2 = Val(1),
            Anio3 = Val(2),
            Anio4 = Val(3),
            Total = total
        };
    }
}
