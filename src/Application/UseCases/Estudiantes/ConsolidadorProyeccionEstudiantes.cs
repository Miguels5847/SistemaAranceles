using SistemaAranceles.Application.DTOs.Estudiantes;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

/// <summary>
/// Replica la lógica de la hoja "1 Estudiantes" de la Matriz Financiera.
///
/// TABLA HORAS — reglas Excel (valores de referencia Sistemas Computacionales):
///   Fila 16 = horas docencia asistida SEMESTRAL por período (input amarillo)
///             [288, 336, 320, 336, 336, 320, 304, 256]
///   Fila 17 = Fila16[p] / semanas * paralelos[p]       (h/semana nuevas)
///             [18, 42, 20, 42, 21, 40, 19, 32]
///   Fila 15 = acumulado de Fila17                       (h/semana totales)
///             [18, 60, 80, 122, 143, 183, 202, 234]
///
///   Fila 20 = horas práctica SEMESTRAL por período (input amarillo)
///             [160, 176, 192, 208, 240, 264, 336, 400]
///   Fila 21 = Fila20[p] / semanas * paralelos[p]
///             [10, 22, 12, 26, 15, 33, 21, 50]
///   Fila 19 = acumulado de Fila21
///             [10, 32, 44, 70, 85, 118, 139, 189]
///
/// TABLA DOCENTES — reglas Excel:
///   Fila 24 (docTotal) = Ceil(Fila15[p] / J24)         → 1, 4, 5, 7, 8, 11, 12, 13
///   Fila 26 (tcMgs)    = Ceil(docTotal * 0.60)
///   Fila 25 (phd)      = Ceil(tcMgs   * 0.40)
///   Fila 28 (parcial)  = Ceil(docTotal) - tcMgs - phd  (≥ 0 siempre)
///   Fila 30 (tecnico)  = Ceil(Fila19[p] / J30)
///
///   INVARIANTE: phd + tcMgs + parcial == Ceil(docTotal)  en cada período.
///   INVARIANTE: ningún valor es negativo.
/// </summary>
public static class ConsolidadorProyeccionEstudiantes
{
    private const decimal HorasDocSemanaDefault = 18m;
    private const decimal HorasTecSemanaDefault = 40m;

    // Fila 16 por defecto (input editable)
    private static readonly decimal[] DocHorasSemestralesDefault =
        [288m, 336m, 320m, 336m, 336m, 320m, 304m, 256m];

    // Fila 20 por defecto (input editable)
    private static readonly decimal[] TecHorasSemestralesDefault =
        [160m, 176m, 192m, 208m, 240m, 264m, 336m, 400m];

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
        var sem           = proyeccion.SemanasPorSemestre;
        var detalles      = proyeccion.Detalles ?? [];
        var totalPeriodos = detalles.Select(d => d.NumeroPeriodo).DefaultIfEmpty(0).Max();
        var totalCiclos   = detalles.Select(d => d.NumeroCiclo).DefaultIfEmpty(0).Max();

        var docSem = horasDocSemestralesOverride ?? DocHorasSemestralesDefault;
        var tecSem = horasTecSemestralesOverride ?? TecHorasSemestralesDefault;
        var hDoc   = horasDocSemanaOverride;
        var hTec   = horasTecSemanaOverride;

        var anios = totalPeriodos > 0
            ? Enumerable.Range(0, (totalPeriodos + 1) / 2)
                        .Select(i => proyeccion.AnioBase + i).ToList()
            : [];

        // Paralelos por período (0-based)
        var paralelos = Enumerable.Range(1, totalPeriodos)
            .Select(p => Par(p, paralelosPeriodo1, paralelosPeriodo2))
            .ToArray();

        // ── Filas 17/21 (h/semana nuevas por período) ───────────────────────────
        var fila17 = new decimal[totalPeriodos];
        var fila21 = new decimal[totalPeriodos];
        for (var p = 0; p < totalPeriodos; p++)
        {
            fila17[p] = GetD(docSem, p) / sem * paralelos[p];
            fila21[p] = GetD(tecSem, p) / sem * paralelos[p];
        }

        // ── Filas 15/19 (acumulados) ─────────────────────────────────────
        var fila15 = new decimal[totalPeriodos];
        var fila19 = new decimal[totalPeriodos];
        fila15[0] = fila17[0];
        fila19[0] = fila21[0];
        for (var p = 1; p < totalPeriodos; p++)
        {
            fila15[p] = fila15[p - 1] + fila17[p];
            fila19[p] = fila19[p - 1] + fila21[p];
        }

        // ── Tabla horas (6 filas: 15, 16, 17, 19, 20, 21) ────────────────────
        var tablaHoras = new List<FilaHorasDto>
        {
            new() { Etiqueta = "Nº Horas Clase Docencia Asistida x semana Acumuladas",
                    EsInput  = false,
                    Valores  = fila15.Select(v => decimal.Round(v, 2)).ToArray() },
            new() { Etiqueta = "N° de Horas con Docencia Asistida SEMESTRAL",
                    EsInput  = true,
                    Valores  = Enumerable.Range(0, totalPeriodos).Select(p => GetD(docSem, p)).ToArray() },
            new() { Etiqueta = "N° Horas Docencia Asistida por semana y por paralelos",
                    EsInput  = false,
                    Valores  = fila17.Select(v => decimal.Round(v, 2)).ToArray() },
            new() { Etiqueta = "Nº Horas Clase Aplicac. Práctica x semana Acumuladas",
                    EsInput  = false,
                    Valores  = fila19.Select(v => decimal.Round(v, 2)).ToArray() },
            new() { Etiqueta = "N° Horas Aplicación Práctica SEMESTRAL",
                    EsInput  = true,
                    Valores  = Enumerable.Range(0, totalPeriodos).Select(p => GetD(tecSem, p)).ToArray() },
            new() { Etiqueta = "N° Horas Aplicación Práctica por semana y por paralelos",
                    EsInput  = false,
                    Valores  = fila21.Select(v => decimal.Round(v, 2)).ToArray() },
        };

        // ── Tabla consumo por período (Tab 5) ────────────────────────────
        // Docentes y Técnicos son personas enteras → siempre Math.Ceiling
        var tablaPeriodos = new List<FilaConsumoPeriodicDto>(totalPeriodos);
        decimal totalHDocAcum = 0, totalHTecAcum = 0;
        for (var p = 0; p < totalPeriodos; p++)
        {
            var hDocPer = decimal.Round(GetD(docSem, p), 0);
            var hTecPer = decimal.Round(GetD(tecSem, p), 0);
            totalHDocAcum += hDocPer;
            totalHTecAcum += hTecPer;
            tablaPeriodos.Add(new FilaConsumoPeriodicDto
            {
                Periodo       = p + 1,
                Anio          = AnioDePeriodo(proyeccion.AnioBase, p + 1),
                Semestre      = p % 2 == 0 ? "Abril" : "Septiembre",
                Docentes      = Ceil(fila15[p] / hDoc),          // entero: nunca 3.33
                Tecnicos      = Ceil(fila19[p] / hTec),          // entero: nunca 0.25
                HorasDocencia = hDocPer,
                HorasPractica = hTecPer
            });
        }

        // ── Docentes requeridos por período (Tabla 2) ──────────────────────
        //
        // ALGORITMO SIN NEGATIVOS:
        //   docTotal[p] = Ceil(fila15[p] / J24)           → 1, 4, 5, 7, 8, 11, 12, 13
        //   tcMgs[p]    = Ceil(docTotal[p] * 0.60)         → 60 %
        //   phd[p]      = Ceil(tcMgs[p]   * 0.40)         → 40 % de Mgs = 24 % del total
        //   parcial[p]  = Max(0, docTotal[p] - tcMgs[p] - phd[p])  → residuo, nunca negativo
        //   tecnico[p]  = Ceil(fila19[p] / J30)
        //
        // INVARIANTE: phd + tcMgs + parcial == docTotal  en cada período.
        var docTotalArr = new int[totalPeriodos];
        var tcMgsArr    = new int[totalPeriodos];
        var phdArr      = new int[totalPeriodos];
        var parcialArr  = new int[totalPeriodos];
        var tecnicoArr  = new int[totalPeriodos];

        for (var p = 0; p < totalPeriodos; p++)
        {
            var dTot = Ceil(fila15[p] / hDoc);
            var mgs  = Ceil(dTot * 0.60m);
            var phd  = Ceil(mgs  * 0.40m);
            var par  = Math.Max(0, dTot - mgs - phd);
            var tec  = Ceil(fila19[p] / hTec);

            docTotalArr[p] = dTot;
            tcMgsArr[p]    = mgs;
            phdArr[p]      = phd;
            parcialArr[p]  = par;
            tecnicoArr[p]  = tec;
        }

        var filasDocentesPeriodo = new List<FilaDocentePeriodoDto>
        {
            new() { Tipo = "Docentes Requeridos",       Periodos = docTotalArr, Total = docTotalArr[^1] },
            new() { Tipo = "TC PhD",                    Periodos = phdArr,      Total = phdArr[^1]      },
            new() { Tipo = "TC Mgs.",                   Periodos = tcMgsArr,    Total = tcMgsArr[^1]    },
            new() { Tipo = "Medio Tiempo",              Periodos = new int[totalPeriodos], Total = 0    },
            new() { Tipo = "Tiempo Parcial",            Periodos = parcialArr,  Total = parcialArr[^1]  },
            new() { Tipo = "Ocasional Tipo 2 (Técnico)",Periodos = tecnicoArr,  Total = tecnicoArr[^1]  },
        };

        // ── Matrícula por período ───────────────────────────────────────
        var filasPeriodo = new List<FilaMatriculaPeriodoDto>(totalCiclos + 1);
        for (var ciclo = 1; ciclo <= totalCiclos; ciclo++)
        {
            var vals = Enumerable.Range(1, totalPeriodos)
                .Select(p => ciclo > p ? 0m :
                    decimal.Round(detalles
                        .Where(d => d.NumeroCiclo == ciclo && d.NumeroPeriodo == p)
                        .Sum(d => d.TotalEstudiantes), 2))
                .ToArray();
            filasPeriodo.Add(new FilaMatriculaPeriodoDto
            {
                Ciclo    = $"{ciclo} CICLO",
                Periodos = vals,
                Total    = decimal.Round(vals.Sum(), 2)
            });
        }

        var totalesPorPeriodo = Enumerable.Range(0, totalPeriodos)
            .Select(i => decimal.Round(filasPeriodo.Sum(f => f.Periodos[i]), 2)).ToArray();
        filasPeriodo.Add(new FilaMatriculaPeriodoDto
        {
            Ciclo    = "Total Nº de Estudiantes",
            Periodos = totalesPorPeriodo,
            Total    = decimal.Round(totalesPorPeriodo.Sum(), 2)
        });

        // ── Matrícula por año (legado) ─────────────────────────────────
        var filasMatricula = new List<FilaMatriculaAnualDto>(totalCiclos + 1);
        for (var ciclo = 1; ciclo <= totalCiclos; ciclo++)
        {
            var vals = anios.Select(anio =>
                detalles.Where(d => d.NumeroCiclo == ciclo &&
                               AnioDePeriodo(proyeccion.AnioBase, d.NumeroPeriodo) == anio)
                        .Sum(d => d.TotalEstudiantes)).ToList();
            filasMatricula.Add(new FilaMatriculaAnualDto
            {
                Ciclo = $"Ciclo {ciclo}",
                Anio1 = decimal.Round(GetList(vals, 0), 2),
                Anio2 = decimal.Round(GetList(vals, 1), 2),
                Anio3 = decimal.Round(GetList(vals, 2), 2),
                Anio4 = decimal.Round(GetList(vals, 3), 2),
                Total = decimal.Round(vals.Sum(), 2)
            });
        }
        filasMatricula.Add(new FilaMatriculaAnualDto
        {
            Ciclo = "TOTAL",
            Anio1 = decimal.Round(filasMatricula.Sum(f => f.Anio1), 2),
            Anio2 = decimal.Round(filasMatricula.Sum(f => f.Anio2), 2),
            Anio3 = decimal.Round(filasMatricula.Sum(f => f.Anio3), 2),
            Anio4 = decimal.Round(filasMatricula.Sum(f => f.Anio4), 2),
            Total = decimal.Round(filasMatricula.Sum(f => f.Total), 2)
        });

        // ── Docentes por año (legado) ─────────────────────────────────
        var docAnio = anios.Select(anio =>
            tablaPeriodos.Where(t => t.Anio == anio).Max(t => t.Docentes)).ToList();
        var tecAnio = anios.Select(anio =>
            tablaPeriodos.Where(t => t.Anio == anio).Max(t => t.Tecnicos)).ToList();
        const decimal pctMgs = 0.60m;
        const decimal pctPhd = 0.40m;
        var parcPct = 1m - pctMgs - pctMgs * pctPhd;
        var filasDocentes = new List<FilaDocenteAnualDto>
        {
            FilaDoc("TC PhD (24 %)",     docAnio, tecAnio, d => decimal.Round(d * pctMgs * pctPhd, 2)),
            FilaDoc("TC Mgs (60 %)",     docAnio, tecAnio, d => decimal.Round(d * pctMgs, 2)),
            FilaDoc("T. Parcial (16 %)", docAnio, tecAnio, d => decimal.Round(d * parcPct, 2)),
            FilaDoc("Técnico",           docAnio, tecAnio, _ => 0m, t => decimal.Round(t, 0)),
            FilaDoc("TOTAL Docentes",    docAnio, tecAnio, d => decimal.Round(d, 2))
        };

        // ── Indicadores ────────────────────────────────────────────────
        var p1Total   = detalles.Where(d => d.NumeroPeriodo == 1).Sum(d => d.TotalEstudiantes);
        var pFinalTot = detalles.Where(d => d.NumeroPeriodo == totalPeriodos).Sum(d => d.TotalEstudiantes);
        var ingresados = detalles
            .Where(d => d.NumeroCiclo == 1 && d.NumeroPeriodo % 2 == 1)
            .Sum(d => d.TotalEstudiantes);
        var indicadores = new IndicadoresProyeccionDto
        {
            TotalIngresados       = decimal.Round(ingresados, 2),
            TituladosEsperados    = decimal.Round(ingresados * tasaGraduacionMeta / 100m, 2),
            MatriculaPeriodo1     = decimal.Round(p1Total, 2),
            MatriculaPeriodoFinal = decimal.Round(pFinalTot, 2),
            CrecimientoMatricula  = p1Total > 0 ? decimal.Round(pFinalTot / p1Total, 2) : 0m,
            TasaRetencionMeta     = tasaRetencionMeta,
            TasaGraduacionMeta    = tasaGraduacionMeta
        };

        return new ProyeccionConsolidadaDto
        {
            LabelAnio1 = anios.Count > 0 ? anios[0].ToString() : "—",
            LabelAnio2 = anios.Count > 1 ? anios[1].ToString() : "—",
            LabelAnio3 = anios.Count > 2 ? anios[2].ToString() : "—",
            LabelAnio4 = anios.Count > 3 ? anios[3].ToString() : "—",
            ParalelosPorPeriodo     = paralelos,
            MatriculaPorPeriodo     = filasPeriodo,
            MatriculaPorAnio        = filasMatricula,
            DocentesPorPeriodo      = filasDocentesPeriodo,
            DocentesPorAnio         = filasDocentes,
            TablaHoras              = tablaHoras,
            TotalHorasDocencia      = decimal.Round(totalHDocAcum, 0),
            TotalHorasPractica      = decimal.Round(totalHTecAcum, 0),
            TotalHorasCombinado     = decimal.Round(totalHDocAcum + totalHTecAcum, 0),
            Indicadores             = indicadores,
            TablaPeriodos           = tablaPeriodos,
            HorasDocenciaSemestral  = docSem,
            HorasPracticaSemestral  = tecSem,
            HorasDocenteSemana      = hDoc,
            HorasTecnicoSemana      = hTec
        };
    }

    private static int Par(int p, int par1, int par2) => p % 2 == 1 ? par1 : par2;
    private static int AnioDePeriodo(int anioBase, int p) => anioBase + (p - 1) / 2;
    private static decimal GetD(decimal[] arr, int i) => i < arr.Length ? arr[i] : arr[^1];
    private static decimal GetList(IList<decimal> l, int i) => i < l.Count ? l[i] : 0m;
    private static int Ceil(decimal v) => (int)Math.Ceiling(v);

    private static FilaDocenteAnualDto FilaDoc(
        string tipo, IList<decimal> docAnio, IList<decimal> tecAnio,
        Func<decimal, decimal> docFn, Func<decimal, decimal>? tecFn = null)
    {
        bool esTec = tecFn is not null;
        decimal Val(int idx) => esTec ? tecFn!(GetList(tecAnio, idx)) : docFn(GetList(docAnio, idx));
        return new FilaDocenteAnualDto
        {
            Tipo  = tipo,
            Anio1 = Val(0), Anio2 = Val(1), Anio3 = Val(2), Anio4 = Val(3),
            Total = esTec ? tecFn!(tecAnio.Sum()) : docFn(docAnio.Sum())
        };
    }
}
