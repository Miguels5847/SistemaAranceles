namespace SistemaAranceles.Application.DTOs.Inflacion;

public sealed class ImportacionBceArchivoSolicitudDto
{
    public string RutaArchivo { get; init; } = string.Empty;
    public TipoSerieInflacionBce TipoSerie { get; init; } = TipoSerieInflacionBce.Annual;
}