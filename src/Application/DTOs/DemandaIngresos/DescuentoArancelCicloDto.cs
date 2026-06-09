namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class DescuentoArancelCicloDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = "Global";
    public int CicloDesde { get; init; }
    public int CicloHasta { get; init; }
    public decimal PorcentajeDescuento { get; init; }
    public bool EstaActivo { get; init; }

    public string RangoDisplay => CicloDesde == CicloHasta ? $"Ciclo {CicloDesde}" : $"Ciclos {CicloDesde}-{CicloHasta}";
    public string PorcentajeDisplay => $"{PorcentajeDescuento:0.##}%";
}

public sealed class GuardarDescuentoArancelCicloDto
{
    public int? Id { get; init; }
    public int CarreraId { get; init; }
    public int? EscenarioProyeccionId { get; init; }
    public int CicloDesde { get; init; }
    public int CicloHasta { get; init; }
    public decimal PorcentajeDescuento { get; init; }
}

public sealed class MatrizDescuentosArancelDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public int? EscenarioProyeccionId { get; init; }
    public string EscenarioNombre { get; init; } = "Global";
    public IReadOnlyList<DescuentoArancelCicloDto> Descuentos { get; init; } = [];
    public string? MensajeAdvertencia { get; init; }
}
