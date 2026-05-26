namespace SistemaAranceles.Infrastructure.Persistence.Entidades;

/// <summary>
/// Ratio de consumo de materiales por estudiante (KAN-34, Épica 9).
/// </summary>
public sealed class RatioMaterialDemanda
{
    public int Id { get; set; }
    public int? CarreraId { get; set; }
    public string Categoria { get; set; } = string.Empty;
    public string Concepto { get; set; } = string.Empty;
    public int? ItemMaterialInsumoId { get; set; }
    public decimal RatioConsumo { get; set; }
    public string UnidadRatio { get; set; } = "por_estudiante";
    public int MesesOperativos { get; set; } = 6;
    public bool AplicaInflacion { get; set; } = true;

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public int? CreadoPorUsuarioId { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public bool EstaActivo { get; set; } = true;
    public DateTime? EliminadoEn { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public Carrera? Carrera { get; set; }
    public ItemMaterialInsumo? ItemMaterialInsumo { get; set; }
}
