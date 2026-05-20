namespace SistemaAranceles.Infrastructure.Persistence.Entidades;

/// <summary>
/// Activo diferido (permiso legal) sujeto a amortización (KAN-31).
/// </summary>
public sealed class ActivoDiferido : EntidadBase
{
    public int CarreraId { get; set; }
    public string NombreRubro { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public decimal TasaAmortizacionAnual { get; set; } = 0.20m;

    public Carrera? Carrera { get; set; }
}
