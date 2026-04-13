namespace SistemaAranceles.Application.DTOs.Inflacion;

public enum TipoSerieInflacionBce
{
    Annual = 1,
    Accumulated = 2
}

public sealed class BceImportSolicitudDto
{
    public string Url { get; init; } = "https://contenido.bce.fin.ec/documentos/informacioneconomica/indicadores/real/Inflacion.html";
    public int? AnioDesde { get; init; }
    public int? AnioHasta { get; init; }
    public TipoSerieInflacionBce TipoSerie { get; init; } = TipoSerieInflacionBce.Annual;
}
