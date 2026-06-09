namespace SistemaAranceles.Infrastructure.Persistence.Entidades;

/// <summary>
/// Descuento comercial del arancel por rango de ciclos (KAN-44). Tabla descuento_arancel_ciclo.
/// </summary>
public sealed class DescuentoArancelCiclo
{
    public int Id { get; set; }
    public int CarreraId { get; set; }
    public int? EscenarioProyeccionId { get; set; }
    public int CicloDesde { get; set; }
    public int CicloHasta { get; set; }
    public decimal PorcentajeDescuento { get; set; }

    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public int? CreadoPorUsuarioId { get; set; }
    public DateTime? ActualizadoEn { get; set; }
    public int? ActualizadoPorUsuarioId { get; set; }
    public bool EstaActivo { get; set; } = true;
    public DateTime? EliminadoEn { get; set; }
    public int? EliminadoPorUsuarioId { get; set; }

    public Carrera? Carrera { get; set; }
    public EscenarioProyeccion? EscenarioProyeccion { get; set; }
}
