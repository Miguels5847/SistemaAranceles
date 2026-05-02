namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class CargoFacultadCalculadoDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public string TipoCargo { get; init; } = string.Empty;
    public decimal SueldoBaseMensual { get; init; }
    public bool EsCargoDocente { get; init; }
    public decimal PesoProporcional { get; init; }
    public decimal DecimoTerceroSemestral { get; init; }
    public decimal DecimoCuartoSemestral { get; init; }
    public decimal VacacionesSemestral { get; init; }
    public decimal FondoReservaMensual { get; init; }
    public decimal AportePatronalMensual { get; init; }
    public decimal CostoBaseSemestral { get; init; }
    public decimal CostoSemestralPonderado { get; init; }
}