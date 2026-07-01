using SistemaAranceles.Domain.Enums;

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
    public decimal CantidadFijaAdicional { get; init; }
    public bool AplicaInflacion { get; init; }
    public bool EstaActivo { get; init; } = true;

    public string RatioDisplay => RatioConsumo.ToString("0.######");
    public string UnidadDisplay => UnidadRatio switch
    {
        UnidadRatioMaterialExtensiones.PorEstudianteMesText => "Por estudiante / mes",
        UnidadRatioMaterialExtensiones.FijoPeriodoText => "Fijo por período",
        UnidadRatioMaterialExtensiones.PorDocenteText => "Por docente",
        _ => "Por estudiante"
    };
    public string CantidadFijaAdicionalDisplay => CantidadFijaAdicional.ToString("0.####");
    public string PrecioReferenciaDisplay => $"$ {PrecioUnitarioReferencia:N2}";
    public string InflacionDisplay => AplicaInflacion ? "Sí" : "No";
    // Solo el nombre: la categoría entre paréntesis ensuciaba la columna "Item vinculado".
    public string ItemVinculadoDisplay => string.IsNullOrWhiteSpace(ItemMaterialInsumoNombre)
        ? "(sin item)"
        : ItemMaterialInsumoNombre;
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
    public decimal CantidadFijaAdicional { get; init; }
    public bool AplicaInflacion { get; init; } = true;
}
