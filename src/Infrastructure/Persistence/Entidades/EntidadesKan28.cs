using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Infrastructure.Persistence.Entidades;

/// <summary>
/// Rubro de servicios básicos o mantenimiento institucional (KAN-28).
/// </summary>
public sealed class ServicioMantenimiento : EntidadBase
{
    public int CarreraId { get; set; }
    public TipoRubroMantenimiento TipoRubro { get; set; }
    public string NombreRubro { get; set; } = string.Empty;
    public decimal CostoAnualUniversidad { get; set; }

    public Carrera? Carrera { get; set; }
}
