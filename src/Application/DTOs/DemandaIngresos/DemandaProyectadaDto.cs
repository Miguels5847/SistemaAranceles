namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class DemandaCicloFilaDto
{
    public int NumeroCiclo { get; init; }
    public string CicloDisplay => $"Ciclo {NumeroCiclo}";
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total => Periodos.Sum();
    public string TotalDisplay => Total.ToString("N0");
}

public sealed class DemandaDocenteFilaDto
{
    public string Tipo { get; init; } = string.Empty;
    public IReadOnlyList<decimal> Periodos { get; init; } = [];
    public decimal Total { get; init; }
    public string TotalDisplay => Total.ToString("N0");
}

public sealed class DemandaProyectadaDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = string.Empty;
    public IReadOnlyList<string> EtiquetasPeriodos { get; init; } = [];
    public IReadOnlyList<int> AniosPeriodos { get; init; } = [];
    public IReadOnlyList<int> NumerosPeriodos { get; init; } = [];
    public IReadOnlyList<DemandaCicloFilaDto> Filas { get; init; } = [];
    public IReadOnlyList<decimal> TotalesPorPeriodo { get; init; } = [];
    public IReadOnlyList<DemandaDocenteFilaDto> DocentesPorPeriodo { get; init; } = [];
    public decimal TotalGeneral => TotalesPorPeriodo.Sum();
    public string TotalGeneralDisplay => TotalGeneral.ToString("N0");
    public string? MensajeAdvertencia { get; init; }
    public string? MensajeAdvertenciaDocentes { get; init; }
    public bool TieneDatos => Filas.Count > 0 && EtiquetasPeriodos.Count > 0;
    public bool TieneDocentes => DocentesPorPeriodo.Count > 0 && EtiquetasPeriodos.Count > 0;
}
