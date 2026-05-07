namespace SistemaAranceles.Application.DTOs.CargosFacultad;

public sealed class FilaSueldoPeriodoDto
{
    public int CargoId { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public bool EsCargoDocente { get; init; }
    public decimal NumeroPersonas { get; init; }
    public decimal Peso { get; init; }
    public decimal SueldoMensual { get; init; }
    public decimal DecimoTerceroSemestral { get; init; }
    public decimal DecimoCuartoSemestral { get; init; }
    public decimal VacacionesSemestral { get; init; }
    public decimal FondoReservaMensual { get; init; }
    public decimal AportePatronalMensual { get; init; }
    public decimal TotalSemestre { get; init; }
}
