namespace SistemaAranceles.Application.DTOs.Estudiantes;

public sealed class ProyeccionConsolidadaDto
{
    public string LabelAnio1 { get; init; } = string.Empty;
    public string LabelAnio2 { get; init; } = string.Empty;
    public string LabelAnio3 { get; init; } = string.Empty;
    public string LabelAnio4 { get; init; } = string.Empty;

    public IReadOnlyList<FilaMatriculaAnualDto> MatriculaPorAnio  { get; init; } = [];
    public IReadOnlyList<FilaDocenteAnualDto>   DocentesPorAnio   { get; init; } = [];
    public decimal TotalHorasDocencia  { get; init; }
    public decimal TotalHorasPractica  { get; init; }
    public decimal TotalHorasCombinado { get; init; }
    public IndicadoresProyeccionDto         Indicadores   { get; init; } = new();
    public IReadOnlyList<FilaConsumoPeriodicDto> TablaPeriodos { get; init; } = [];
}

public sealed class FilaMatriculaAnualDto
{
    public string  Ciclo { get; init; } = string.Empty;
    public decimal Anio1 { get; init; }
    public decimal Anio2 { get; init; }
    public decimal Anio3 { get; init; }
    public decimal Anio4 { get; init; }
    public decimal Total { get; init; }
}

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
    public int     Periodo        { get; init; }
    public int     Anio           { get; init; }
    public string  Semestre       { get; init; } = string.Empty;
    public decimal Docentes       { get; init; }
    public decimal Tecnicos       { get; init; }
    public decimal HorasDocencia  { get; init; }
    public decimal HorasPractica  { get; init; }
}
