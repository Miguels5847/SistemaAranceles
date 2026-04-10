namespace SistemaAranceles.Application.DTOs.Auditoria;

public sealed class AuditoriaFiltroDto
{
    public int? UsuarioId { get; init; }

    public DateTime? FechaDesdeUtc { get; init; }

    public DateTime? FechaHastaUtc { get; init; }

    public string? ModuloNombre { get; init; }

    public string? AccionNombre { get; init; }

    public int NumeroPagina { get; init; } = 1;

    public int TamanoPagina { get; init; } = 25;
}
