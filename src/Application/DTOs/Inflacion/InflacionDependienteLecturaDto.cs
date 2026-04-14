namespace SistemaAranceles.Application.DTOs.Inflacion;

/// <summary>
/// DTO de consumo para módulos dependientes.
/// Este contrato es solo lectura y no debe utilizarse para operaciones de escritura.
/// </summary>
public sealed class InflacionDependienteLecturaDto
{
    public int Anio { get; init; }
    public decimal PorcentajeInflacion { get; init; }
    public bool EsProyectada { get; init; }
    public bool SoloLectura { get; init; } = true;
}
