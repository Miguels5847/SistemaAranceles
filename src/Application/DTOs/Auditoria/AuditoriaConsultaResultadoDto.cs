namespace SistemaAranceles.Application.DTOs.Auditoria;

public sealed class AuditoriaConsultaResultadoDto
{
    public IReadOnlyList<AuditoriaLogItemDto> Items { get; init; } = [];

    public int TotalRegistros { get; init; }

    public int NumeroPagina { get; init; }

    public int TamanoPagina { get; init; }

    public int TotalPaginas => TamanoPagina <= 0
        ? 0
        : (int)Math.Ceiling((double)TotalRegistros / TamanoPagina);
}
