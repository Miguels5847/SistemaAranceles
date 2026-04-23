namespace SistemaAranceles.Application.DTOs.Inflacion;

public sealed class ProyeccionInflacionResultadoDto
{
    public int RegistrosCreados { get; init; }
    public int RegistrosActualizados { get; init; }
    public int RegistrosOmitidos { get; init; }
}
