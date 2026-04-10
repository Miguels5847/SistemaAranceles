namespace SistemaAranceles.Application.DTOs.Auditoria;

public sealed class AuditoriaLogItemDto
{
    public int Id { get; init; }

    public DateTime EventoEnUtc { get; init; }

    public string ModuloNombre { get; init; } = string.Empty;

    public string EntidadNombre { get; init; } = string.Empty;

    public string EntidadId { get; init; } = string.Empty;

    public string AccionNombre { get; init; } = string.Empty;

    public string ResumenTexto { get; init; } = string.Empty;

    public int? EjecutadoPorUsuarioId { get; init; }

    public string EjecutadoPorUsuarioNombre { get; init; } = "Sistema";
}
