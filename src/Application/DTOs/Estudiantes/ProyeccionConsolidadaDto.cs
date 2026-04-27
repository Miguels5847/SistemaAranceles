namespace SistemaAranceles.Application.DTOs.Estudiantes;

public sealed class ProyeccionConsolidadaDto
{
    public string LabelAnio1 { get; init; } = string.Empty;
    public string LabelAnio2 { get; init; } = string.Empty;
    public string LabelAnio3 { get; init; } = string.Empty;
    public string LabelAnio4 { get; init; } = string.Empty;

    // ── Vista nueva: por período (como el Excel) ─────────────────────────
    public int[]                                     ParalelosPorPeriodo { get; init; } = [];
    public IReadOnlyList<FilaMatriculaPeriodoDto>    MatriculaPorPeriodo { get; init; } = [];

    // ── Vista legada: por año (se mantiene por compatibilidad) ───────────
    public IReadOnlyList<FilaMatriculaAnualDto>      MatriculaPorAnio    { get; init; } = [];

    // ── Tabla 2: docentes/técnicos por PERÍODO (columnas dinámicas) ──────
    public IReadOnlyList<FilaDocentePeriodoDto>      DocentesPorPeriodo  { get; init; } = [];

    // ── Tabla 2 legada (por año, se mantiene por compatibilidad) ─────────
    public IReadOnlyList<FilaDocenteAnualDto>        DocentesPorAnio     { get; init; } = [];

    public decimal TotalHorasDocencia  { get; init; }
    public decimal TotalHorasPractica  { get; init; }
    public decimal TotalHorasCombinado { get; init; }
    public IndicadoresProyeccionDto              Indicadores   { get; init; } = new();
    public IReadOnlyList<FilaConsumoPeriodicDto> TablaPeriodos { get; init; } = [];

    // ── Inputs editables para recalcular (fila 16, fila 20, J24, J30) ───
    public decimal[] HorasDocenciaSemestral { get; init; } = [];
    public decimal[] HorasPracticaSemestral { get; init; } = [];
    public decimal   HorasDocenteSemana     { get; init; } = 18m;
    public decimal   HorasTecnicoSemana     { get; init; } = 40m;
}

/// <summary>
/// Fila de la tabla 1 — matrícula por período (columnas dinámicas).
/// Periodos[0] = período 1, Periodos[1] = período 2, … Periodos[n-1] = período n.
/// </summary>
public sealed class FilaMatriculaPeriodoDto
{
    public string    Ciclo    { get; init; } = string.Empty;
    public decimal[] Periodos { get; init; } = [];
    public decimal   Total    { get; init; }
}

public sealed class FilaMatriculaAnualDto
{
    public string    Ciclo    { get; init; } = string.Empty;
    public decimal[] Periodos { get; init; } = [];
    public decimal   Anio1    { get; init; }
    public decimal   Anio2    { get; init; }
    public decimal   Anio3    { get; init; }
    public decimal   Anio4    { get; init; }
    public decimal   Total    { get; init; }
}

public sealed class FilaParalelosDto
{
    public int[] CantidadParalelos { get; init; } = [];
}

/// <summary>
/// Tabla 2 — Docentes requeridos por PERÍODO (columnas dinámicas, igual que Matrícula).
/// Periodos[0] = período 1 (p.ej. 2023 ABR), Periodos[1] = período 2 (2023 SEP), …
/// Los valores son enteros (Math.Ceiling) porque no existen fracciones de docente.
/// </summary>
public sealed class FilaDocentePeriodoDto
{
    public string  Tipo     { get; init; } = string.Empty;
    /// <summary>Valores enteros (Ceiling) por período — array 0-based.</summary>
    public int[]   Periodos { get; init; } = [];
    public int     Total    { get; init; }
}

/// <summary>Legado — por año (4 columnas fijas). Se mantiene por compatibilidad.</summary>
public sealed class FilaDocenteAnualDto
{
    public string  Tipo  { get; init; } = string.Empty;
    public decimal Anio1 { get; init; }
    public decimal Anio2 { get; init; }
    public decimal Anio3 { get; init; }
    public decimal Anio4 { get; init; }
    public decimal Total { get; init; }
}

public sealed class IndicadoresProyeccionDto
{
    public decimal TotalIngresados       { get; init; }
    public decimal TituladosEsperados    { get; init; }
    public decimal MatriculaPeriodo1     { get; init; }
    public decimal MatriculaPeriodoFinal { get; init; }
    public decimal CrecimientoMatricula  { get; init; }
    public decimal TasaRetencionMeta     { get; init; }
    public decimal TasaGraduacionMeta    { get; init; }
}

public sealed class FilaConsumoPeriodicDto
{
    public int     Periodo       { get; init; }
    public int     Anio          { get; init; }
    public string  Semestre      { get; init; } = string.Empty;
    public decimal Docentes      { get; init; }
    public decimal Tecnicos      { get; init; }
    public decimal HorasDocencia { get; init; }
    public decimal HorasPractica { get; init; }
}
