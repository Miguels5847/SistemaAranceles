namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class ActualizarCargoFacultadDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public string TipoCargo { get; init; } = string.Empty;
    public decimal SueldoBaseMensual { get; init; }
    public bool EsCargoDocente { get; init; }
    public decimal CantidadDefault { get; init; } = 1m;
}