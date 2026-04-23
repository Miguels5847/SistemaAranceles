namespace SistemaAranceles.Domain.Retencion.Politicas;

public sealed class ValoresSugeridosDto
{
    public decimal Retencion { get; init; }
    public decimal Graduacion { get; init; }
    public decimal EstP1 { get; init; }
    public decimal EstP2 { get; init; }
    public int ParP1 { get; init; }
    public int ParP2 { get; init; }
    public int Ciclos { get; init; }
}
