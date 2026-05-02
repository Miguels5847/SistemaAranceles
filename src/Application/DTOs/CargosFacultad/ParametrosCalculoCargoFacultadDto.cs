namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class ParametrosCalculoCargoFacultadDto
{
    public decimal EstudiantesCarreraPeriodo { get; init; }
    public decimal EstudiantesUnidadAcademica { get; init; }
    public decimal FactorInflacion { get; init; } = 1m;
    public decimal ValorBaseDecimoCuartoSemestral { get; init; } = 200m;
    public decimal TasaFondoReserva { get; init; } = 0.0833m;
    public decimal TasaAportePatronal { get; init; } = 0.1115m;
}