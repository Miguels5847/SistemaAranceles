using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Infrastructure.Persistence.Entidades;

/// <summary>
/// Persistencia del activo fijo (KAN-24). Hereda soft-delete y auditoria de EntidadBase.
/// </summary>
public sealed class ActivoFijo : EntidadBase
{
    public int CarreraId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public CategoriaActivoFijo Categoria { get; set; }
    public decimal Cantidad { get; set; }
    public string UnidadMedida { get; set; } = "UNI";
    public decimal ValorUnitario { get; set; }
    public int VidaUtilAnios { get; set; }
    public decimal PorcentajeResidual { get; set; } = 0.05m;
    public DateTimeOffset FechaAdquisicion { get; set; } = DateTimeOffset.UtcNow;

    public Carrera? Carrera { get; set; }
}
