namespace SistemaAranceles.Application.DTOs.DemandaIngresos;

public sealed class RatioMaterialDemandaDto
{
    public int Id { get; init; }
    public int? CarreraId { get; init; }
    public string CarreraNombre { get; init; } = "Global";
    public string Categoria { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public int? ItemMaterialInsumoId { get; init; }
    public string? ItemMaterialInsumoNombre { get; init; }
    public string? ItemMaterialInsumoCategoria { get; init; }
    public decimal PrecioUnitarioReferencia { get; init; }
    public decimal RatioConsumo { get; init; }
    public string UnidadRatio { get; init; } = "por_estudiante";
    public int MesesOperativos { get; init; }
    public bool AplicaInflacion { get; init; }
    public bool EstaActivo { get; init; } = true;

    public string RatioDisplay => RatioConsumo.ToString("0.######");
    public string UnidadDisplay => UnidadRatio == "por_estudiante_mes" ? "por estudiante / mes" : "por estudiante";
    public string PrecioReferenciaDisplay => $"$ {PrecioUnitarioReferencia:N2}";
    public string InflacionDisplay => AplicaInflacion ? "Sí" : "No";
    public string ItemVinculadoDisplay => string.IsNullOrWhiteSpace(ItemMaterialInsumoNombre)
        ? "(sin item)"
        : string.IsNullOrWhiteSpace(ItemMaterialInsumoCategoria)
            ? ItemMaterialInsumoNombre
            : $"{ItemMaterialInsumoNombre} ({ItemMaterialInsumoCategoria})";
}

public sealed class GuardarRatioMaterialDemandaDto
{
    public int? Id { get; init; }
    public int? CarreraId { get; init; }
    public string Categoria { get; init; } = string.Empty;
    public string Concepto { get; init; } = string.Empty;
    public int? ItemMaterialInsumoId { get; init; }
    public decimal RatioConsumo { get; init; }
    public string UnidadRatio { get; init; } = "por_estudiante";
    public int MesesOperativos { get; init; } = 6;
    public bool AplicaInflacion { get; init; } = true;
}
