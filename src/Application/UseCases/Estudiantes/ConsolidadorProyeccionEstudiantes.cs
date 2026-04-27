using SistemaAranceles.Application.DTOs.Estudiantes;

namespace SistemaAranceles.Application.UseCases.Estudiantes;

/// <summary>
/// Genera los 5 cuadros consolidados del RF-ES-01/02/03 replicando la lógica de
/// "1 Estudiantes" de la Matriz Financiera (Sistemas Computacionales).
///
/// Constantes Excel reproducidas:
///   DocHBase[p]  = fila16[p] / SEMANAS  → h/sem por paralelo para docencia
///   TecHBase[p]  = fila20[p] / SEMANAS  → h/sem por paralelo para práctica
///   fila15[p]    = Σ(q=1..p) DocHBase[q] * par[q]   (acumulado semanal docencia)
///   fila19[p]    = Σ(q=1..p) TecHBase[q] * par[q]   (acumulado semanal práctica)
///   docentes[p]  = fila15[p] / 18
///   técnicos[p]  = fila19[p] / 40
/// </summary>
public static class ConsolidadorProyeccionEstudiantes
{
    private const decimal HorasDocSemana = 18m;
    private const decimal HorasTecSemana = 40m;
    private const decimal PctMgs = 0.60m;
    private const decimal PctPhd = 0.40m; // de TC Mgs = 24 % del total

    private static readonly decimal[] DocHBase = [18, 21, 20, 21, 21, 20, 19, 16];
    private static readonly decimal[] TecHBase = [10, 11, 12, 13, 15, 16.5m, 21, 25];

    public static ProyeccionConsolidadaDto Calcular(
        ProyeccionEstudiantesDto proyeccion,
        int paralelosPeriodo1,
        int paralelosPeriodo2,
        decimal tasaRetencionMeta,
        decimal tasaGraduacionMeta)
    {
        var sem = proyeccion.SemanasPorSemestre;
        var detalles = proyeccion.Detalles ?? [];
        var totalPeriodos = detalles.Select(d => d.NumeroPeriodo).DefaultIfEmpty(0).Max();
        var totalCiclos = detalles.Select(d => d.NumeroCiclo).DefaultIfEmpty(0).Max();

        var anios = totalPeriodos > 0
            ? Enumerable.Range(0, (totalPeriodos + 1) / 2)
                .Select(i => proyeccion.AnioBase + i)
                .ToList()
            : [];

        var acumDocH = new decimal[totalPeriodos + 1];
        var acumTecH = new decimal[totalPeriodos + 1];

        for (var p = 1; p <= totalPeriodos; p++)
        {
            var par = Par(p, paralelosPeriodo1, paralelosPeriodo2);
            var i = p - 1;
            acumDocH[p] = acumDocH[p - 1] + Get(DocHBase, i) * par;
            acumTecH[p] = acumTecH[p - 1] + Get(TecHBase, i) * par;
        }

        var tablaPeriodos = new List<FilaConsumoPeriodicDto>(totalPeriodos);
        decimal totalHDoc = 0, totalHTec = 0;

        for (var p = 1; p <= totalPeriodos; p++)
        {
            var i = p - 1;
            var hDoc = R(Get(DocHBase, i) * sem);
            var hTec = R(Get(TecHBase, i) * sem);
            totalHDoc += hDoc;
            totalHTec += hTec;

            tablaPeriodos.Add(new FilaConsumoPeriodicDto
            {
                Periodo = p,
                Anio = AnioDePeriodo(proyeccion.AnioBase, p),
                Semestre = p % 2 == 1 ? "ABR" : "SEP",
                Docentes = R(acumDocH[p] / HorasDocSemana),
                Tecnicos = R(acumTecH[p] / HorasTecSemana, 3),
                HorasDocencia = hDoc,
                HorasPractica = hTec
            });
        }

        var filasMatricula = new List<FilaMatriculaAnualDto>(totalCiclos + 1);

        for (var ciclo = 1; ciclo <= totalCiclos; ciclo++)
        {
            var vals = anios
                .Select(anio => detalles
                    .Where(d => d.NumeroCiclo == ciclo && AnioDePeriodo(proyeccion.AnioBase, d.NumeroPeriodo) == anio)
                    .Sum(d => d.TotalEstudiantes))
                .ToList();

            filasMatricula.Add(new FilaMatriculaAnualDto
            {
                Ciclo = $"Ciclo {ciclo}",
                Anio1 = R(GetD(vals, 0)),
                Anio2 = R(GetD(vals, 1)),
                Anio3 = R(GetD(vals, 2)),
                Anio4 = R(GetD(vals, 3)),
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

        var docAnio = anios
            .Select(anio => tablaPeriodos.Where(t => t.Anio == anio).Sum(t => t.Docentes))
            .ToList();

        var tecAnio = anios
            .Select(anio => tablaPeriodos.Where(t => t.Anio == anio).Sum(t => t.Tecnicos))
            .ToList();

        var parcPct = 1m - PctMgs - PctMgs * PctPhd; // 16 %

        var filasDocentes = new List<FilaDocenteAnualDto>
        {
            FilaDoc("TC PhD (24 %)",     docAnio, tecAnio, d => R(d * PctMgs * PctPhd)),
            FilaDoc("TC Mgs (60 %)",     docAnio, tecAnio, d => R(d * PctMgs)),
            FilaDoc("T. Parcial (16 %)", docAnio, tecAnio, d => R(d * parcPct)),
            FilaDoc("Técnico",           docAnio, tecAnio, _ => 0m, t => R(t, 3)),
            FilaDoc("TOTAL Docentes",    docAnio, tecAnio, d => R(d))
        };

        var p1Total = detalles.Where(d => d.NumeroPeriodo == 1).Sum(d => d.TotalEstudiantes);
        var pFinalTot = detalles.Where(d => d.NumeroPeriodo == totalPeriodos).Sum(d => d.TotalEstudiantes);
        var ingresados = detalles.Where(d => d.NumeroCiclo == 1 && d.NumeroPeriodo % 2 == 1)
                                 .Sum(d => d.TotalEstudiantes);

        var indicadores = new IndicadoresProyeccionDto
        {
            TotalIngresados = R(ingresados),
            TituladosEsperados = R(ingresados * tasaGraduacionMeta / 100m),
            MatriculaPeriodo1 = R(p1Total),
            MatriculaPeriodoFinal = R(pFinalTot),
            CrecimientoMatricula = p1Total > 0 ? R(pFinalTot / p1Total) : 0m,
            TasaRetencionMeta = tasaRetencionMeta,
            TasaGraduacionMeta = tasaGraduacionMeta
        };

        return new ProyeccionConsolidadaDto
        {
            LabelAnio1 = anios.Count > 0 ? anios[0].ToString() : "—",
            LabelAnio2 = anios.Count > 1 ? anios[1].ToString() : "—",
            LabelAnio3 = anios.Count > 2 ? anios[2].ToString() : "—",
            LabelAnio4 = anios.Count > 3 ? anios[3].ToString() : "—",
            MatriculaPorAnio = filasMatricula,
            DocentesPorAnio = filasDocentes,
            TotalHorasDocencia = R(totalHDoc),
            TotalHorasPractica = R(totalHTec),
            TotalHorasCombinado = R(totalHDoc + totalHTec),
            Indicadores = indicadores,
            TablaPeriodos = tablaPeriodos
        };
    }

    private static int Par(int p, int par1, int par2) => p % 2 == 1 ? par1 : par2;

    private static int AnioDePeriodo(int anioBase, int numeroPeriodoSecuencial)
        => anioBase + (numeroPeriodoSecuencial - 1) / 2;

    private static decimal Get(decimal[] arr, int i) => i < arr.Length ? arr[i] : arr[^1];
    private static decimal GetD(IList<decimal> list, int i) => i < list.Count ? list[i] : 0m;
    private static decimal R(decimal v, int dec = 2) => decimal.Round(v, dec);

    private static FilaDocenteAnualDto FilaDoc(
        string tipo,
        IList<decimal> docAnio,
        IList<decimal> tecAnio,
        Func<decimal, decimal> docFn,
        Func<decimal, decimal>? tecFn = null)
    {
        bool esTec = tecFn is not null;

        decimal Val(int idx) => esTec
            ? tecFn!(GetD(tecAnio, idx))
            : docFn(GetD(docAnio, idx));

        decimal total = esTec
            ? tecFn!(tecAnio.Sum())
            : docFn(docAnio.Sum());

        return new FilaDocenteAnualDto
        {
            Tipo = tipo,
            Anio1 = Val(0),
            Anio2 = Val(1),
            Anio3 = Val(2),
            Anio4 = Val(3),
            Total = total
        };
    }
}