using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Application.DTOs.RecursosFisicosDepreciacion;

public sealed class InversionFuturaDto
{
    public int Id { get; init; }
    public int ActivoFijoId { get; init; }
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public decimal CantidadProyectada { get; init; }
}

public sealed class GuardarInversionFuturaDto
{
    public int ActivoFijoId { get; init; }
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public decimal CantidadProyectada { get; init; }
}

public sealed class PeriodoInversionDto
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
}

public sealed class CeldaInversionDto
{
    public int ActivoFijoId { get; init; }
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public decimal Cantidad { get; init; }
    public decimal ValorUnitario { get; init; }
    public decimal FactorInflacion { get; init; } = 1m;
    public decimal Monto { get; init; }
    public bool EsEditable { get; init; }
    public bool EsPeriodoInicial { get; init; }
    public string OrigenCantidad { get; init; } = string.Empty;
    public string CantidadDisplay => Cantidad.ToString("0.####");
    public string MontoDisplay => Monto.ToString("C2");
}

public sealed class FilaMatrizInversionDto
{
    public int ActivoFijoId { get; init; }
    public string Descripcion { get; init; } = string.Empty;
    public CategoriaActivoFijo Categoria { get; init; }
    public string CategoriaNombre { get; init; } = string.Empty;
    public TipoCalculoCantidad TipoCalculoCantidad { get; init; }
    public string TipoCalculoNombre { get; init; } = string.Empty;
    public decimal ValorUnitario { get; init; }
    public IReadOnlyList<CeldaInversionDto> Celdas { get; init; } = [];
}

public sealed class TotalPeriodoInversionDto
{
    public int Anio { get; init; }
    public int Semestre { get; init; }
    public int NumeroPeriodo { get; init; }
    public string Etiqueta { get; init; } = string.Empty;
    public decimal Total { get; init; }
    public string TotalDisplay => Total.ToString("C2");
}

public sealed class MatrizInversionesDto
{
    public int CarreraId { get; init; }
    public int EscenarioProyeccionId { get; init; }
    public IReadOnlyList<PeriodoInversionDto> Periodos { get; init; } = [];
    public IReadOnlyList<FilaMatrizInversionDto> Filas { get; init; } = [];
    public IReadOnlyList<TotalPeriodoInversionDto> TotalesPorPeriodo { get; init; } = [];
}
