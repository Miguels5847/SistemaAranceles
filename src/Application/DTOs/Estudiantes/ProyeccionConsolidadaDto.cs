namespace SistemaAranceles.Application.DTOs.Estudiantes;

public sealed class ProyeccionConsolidadaDto
{
    public string LabelAnio1 { get; init; } = string.Empty;
    public string LabelAnio2 { get; init; } = string.Empty;
    public string LabelAnio3 { get; init; } = string.Empty;
    public string LabelAnio4 { get; init; } = string.Empty;

    public int[]                                     ParalelosPorPeriodo { get; init; } = [];
    public IReadOnlyList<FilaMatriculaPeriodoDto>    MatriculaPorPeriodo { get; init; } = [];
    public IReadOnlyList<FilaMatriculaAnualDto>      MatriculaPorAnio    { get; init; } = [];

    /// <summary>Tabla 2 — Docentes/Técnicos por PERÍODO (columnas dinámicas, valores enteros).</summary>
    public IReadOnlyList<FilaDocentePeriodoDto>      DocentesPorPeriodo  { get; init; } = [];

    /// <summary>Legado — por año (4 columnas fijas).</summary>
    public IReadOnlyList<FilaDocenteAnualDto>        DocentesPorAnio     { get; init; } = [];

    /// <summary>Tabla de horas: filas 15, 16, 17, 19, 20, 21 del Excel.</summary>
    public IReadOnlyList<FilaHorasDto>               TablaHoras          { get; init; } = [];

    public decimal TotalHorasDocencia  { get; init; }
    public decimal TotalHorasPractica  { get; init; }
    public decimal TotalHorasCombinado { get; init; }

    public IndicadoresProyeccionDto              Indicadores   { get; init; } = new();
    public IReadOnlyList<FilaConsumoPeriodicDto> TablaPeriodos { get; init; } = [];

    // Inputs editables (J24, J30, fila 16, fila 20)
    public decimal[] HorasDocenciaSemestral { get; init; } = [];
    public decimal[] HorasPracticaSemestral { get; init; } = [];
    public decimal   HorasDocenteSemana     { get; init; } = 18m;
    public decimal   HorasTecnicoSemana     { get; init; } = 40m;
}

/// <summary>
/// Fila de la tabla de horas (filas 15–17 y 19–21 del Excel).
/// Valores[p] = valor de la celda en el período p (0-based).
/// </summary>
public sealed class FilaHorasDto
{
    /// <summary>Etiqueta de la fila, p.ej. "Nº Horas Clase Docencia Asistida x semana Acumuladas"</summary>
    public string    Etiqueta  { get; init; } = string.Empty;
    /// <summary>true = celda amarilla (input manual), false = fórmula</summary>
    public bool      EsInput   { get; init; }
    public decimal[] Valores   { get; init; } = [];
}

/// <summary>
/// Tabla 1 — matrícula por período (columnas dinámicas).
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

/// <summary>
/// Tabla 2 — Docentes requeridos por PERÍODO (columnas dinámicas, enteros).
/// Periodos[p] = número de personas requeridas en el período p (0-based).
/// Total = valor del último período (máximo acumulado).
/// INVARIANTE: PhD + Mgs + Parcial == TOTAL en cada período.
/// INVARIANTE: ningún valor es negativo.
/// </summary>
public sealed class FilaDocentePeriodoDto
{
    public string     Tipo           { get; init; } = string.Empty;
    public int[]      Periodos       { get; init; } = [];
    /// <summary>Valor del período final (no suma, sino último acumulado).</summary>
    public int        Total          { get; init; }
    /// <summary>Horas asignadas por período. Solo poblada en filas "Horas asignadas Medio Tiempo" y "Horas asignadas Tiempo Parcial". Null en filas de personas.</summary>
    public decimal[]? HorasAsignadas { get; init; }
}

/// <summary>Legado — por año (4 columnas fijas).</summary>
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

/// <summary>
/// Fila de la tabla "4. Consumo por período".
/// Docentes y Tecnicos son siempre enteros (Math.Ceiling aplicado en el consolidador).
/// </summary>
public sealed class FilaConsumoPeriodicDto
{
    public int    Periodo       { get; init; }
    public int    Anio          { get; init; }
    public string Semestre      { get; init; } = string.Empty;
    public int    Docentes      { get; init; }   // personas enteras, nunca fraccionario
    public int    Tecnicos      { get; init; }   // personas enteras, nunca fraccionario
    // CU-ES-04: editable. set público para soportar edición desde DataGrid (override de horas).
    public decimal HorasDocencia { get; set; }
    public decimal HorasPractica { get; set; }
}
