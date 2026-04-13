using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Infrastructure.Servicios;

public sealed partial class ServicioImportacionBCE : IServicioImportacionBCE
{
    public async Task<ImportacionInflacionLecturaResultadoDto> ObtenerInflacionAnualAsync(
        BceImportSolicitudDto solicitud,
        CancellationToken cancellationToken = default)
    {
        var filas = new List<ImportacionInflacionFilaDto>();
        var errores = new List<ImportacionInflacionErrorDto>();
        var tipoSerie = solicitud.TipoSerie;

        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCE: inicio. url={solicitud.Url}, anio_desde={solicitud.AnioDesde}, anio_hasta={solicitud.AnioHasta}, serie={tipoSerie}");

        using var httpClient = new HttpClient();
        var html = await httpClient.GetStringAsync(solicitud.Url, cancellationToken);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var tablas = doc.DocumentNode.SelectNodes("//table");
        if (tablas is null || tablas.Count == 0)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCE: sin tablas detectadas en html.");
            return new ImportacionInflacionLecturaResultadoDto
            {
                Filas = [],
                Errores =
                [
                    new ImportacionInflacionErrorDto
                    {
                        Fila = 0,
                        Mensaje = "No se encontraron tablas en la página BCE.",
                        Valor = solicitud.Url
                    }
                ]
            };
        }
        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCE: tablas_detectadas={tablas.Count}");

        var anioValor = new Dictionary<int, decimal>();
        var filasEscaneadas = 0;

        foreach (var tabla in tablas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ExtraerTablaSerieHistorica(tabla, tipoSerie, anioValor, ref filasEscaneadas);
        }

        if (anioValor.Count == 0)
        {
            ExtraerDesdeTextoCompleto(doc.DocumentNode.InnerText, tipoSerie, anioValor, ref filasEscaneadas);
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCE: filas_escaneadas={filasEscaneadas}, anios_extraidos={anioValor.Count}");

        var anioMin = solicitud.AnioDesde ?? anioValor.Keys.DefaultIfEmpty().Min();
        var anioMax = solicitud.AnioHasta ?? anioValor.Keys.DefaultIfEmpty().Max();

        if (anioValor.Count == 0)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCE: no se pudo mapear diciembre acumulada.");
            errores.Add(new ImportacionInflacionErrorDto
            {
                Fila = 0,
                Mensaje = $"No se pudo extraer datos BCE para serie {tipoSerie}. Verifique URL o estructura de la tabla.",
                Valor = solicitud.Url
            });
        }

        foreach (var (anio, valor) in anioValor.OrderBy(x => x.Key))
        {
            if (anio < anioMin || anio > anioMax)
                continue;

            filas.Add(new ImportacionInflacionFilaDto
            {
                Fila = anio,
                Anio = anio,
                PorcentajeInflacion = valor,
                FuenteNombre = "BCE",
                TipoFuente = "Dato historico"
            });
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCE: fin. filas_resultado={filas.Count}, errores={errores.Count}");

        return new ImportacionInflacionLecturaResultadoDto
        {
            Filas = filas,
            Errores = errores
        };
    }

    private static void ExtraerTablaSerieHistorica(HtmlNode tabla, TipoSerieInflacionBce tipoSerie, IDictionary<int, decimal> destino, ref int filasEscaneadas)
    {
        var filas = tabla.SelectNodes(".//tr");
        if (filas is null || filas.Count == 0)
            return;

        var headers = filas
            .Select(r => r.SelectNodes(".//th|.//td")?.Select(c => Limpiar(c.InnerText)).ToList() ?? [])
            .FirstOrDefault(c => c.Count > 1) ?? [];

        var idxAnio = BuscarIndice(headers, "año", "anio");
        var idxMes = BuscarIndice(headers, "mes");
        var idxAnual = BuscarIndice(headers, "anual");
        var idxAcum = BuscarIndice(headers, "acumulada", "acumulado");
        var idxPeriodo = BuscarIndice(headers, "periodo", "período");

        foreach (var fila in filas)
        {
            var celdas = fila.SelectNodes(".//th|.//td")?.Select(c => Limpiar(c.InnerText)).ToList() ?? [];
            if (celdas.Count < 2)
                continue;

            filasEscaneadas++;

            if (TryExtraerDesdeEstructuraClasica(celdas, idxAnio, idxMes, idxAnual, idxAcum, tipoSerie, out var anioClasico, out var valorClasico))
            {
                destino[anioClasico] = valorClasico;
                continue;
            }

            if (TryExtraerDesdePeriodo(celdas, idxPeriodo, idxAnual, tipoSerie, out var anioPeriodo, out var valorPeriodo))
            {
                destino[anioPeriodo] = valorPeriodo;
            }
        }
    }

    private static void ExtraerDesdeTextoCompleto(string texto, TipoSerieInflacionBce tipoSerie, IDictionary<int, decimal> destino, ref int filasEscaneadas)
    {
        // Fallback para el caso en que el BCE renderice la tabla como texto plano en lugar de nodos tr/td.
        var matches = SerieHistoricaRegex().Matches(texto);
        if (matches.Count == 0)
        {
            ExtraerDesdeTextoPeriodoAnual(texto, destino, ref filasEscaneadas);
            return;
        }

        foreach (Match match in matches)
        {
            filasEscaneadas++;

            if (!int.TryParse(match.Groups["anio"].Value, out var anio))
                continue;

            var mes = match.Groups["mes"].Value.Trim();
            if (!EsDiciembre(mes))
                continue;

            var valorTexto = tipoSerie == TipoSerieInflacionBce.Accumulated
                ? match.Groups["acum"].Value
                : match.Groups["anual"].Value;

            if (TryParseDecimal(valorTexto, out var valor))
                destino[anio] = valor;
        }
    }

    private static void ExtraerDesdeTextoPeriodoAnual(string texto, IDictionary<int, decimal> destino, ref int filasEscaneadas)
    {
        var matches = PeriodoAnualRegex().Matches(texto);
        foreach (Match match in matches)
        {
            filasEscaneadas++;

            if (!TryParseYearMonth(match.Groups["periodo"].Value, out var anio, out var mes))
                continue;

            if (mes != 12)
                continue;

            if (TryParseDecimal(match.Groups["anual"].Value, out var valor))
                destino[anio] = valor;
        }
    }

    private static bool TryExtraerDesdeEstructuraClasica(
        IReadOnlyList<string> celdas,
        int idxAnio,
        int idxMes,
        int idxAnual,
        int idxAcum,
        TipoSerieInflacionBce tipoSerie,
        out int anio,
        out decimal valor)
    {
        anio = default;
        valor = default;

        if (idxAnio < 0 || idxMes < 0)
            return false;
        if (idxAnio >= celdas.Count || idxMes >= celdas.Count)
            return false;

        if (!int.TryParse(celdas[idxAnio], out anio))
            return false;

        if (!EsDiciembre(celdas[idxMes]))
            return false;

        var idxValor = tipoSerie == TipoSerieInflacionBce.Accumulated ? idxAcum : idxAnual;
        if (idxValor < 0 || idxValor >= celdas.Count)
            return false;

        return TryParseDecimal(celdas[idxValor], out valor);
    }

    private static bool TryExtraerDesdePeriodo(
        IReadOnlyList<string> celdas,
        int idxPeriodo,
        int idxAnual,
        TipoSerieInflacionBce tipoSerie,
        out int anio,
        out decimal valor)
    {
        anio = default;
        valor = default;

        if (tipoSerie != TipoSerieInflacionBce.Annual)
            return false;
        if (idxPeriodo < 0 || idxAnual < 0)
            return false;
        if (idxPeriodo >= celdas.Count || idxAnual >= celdas.Count)
            return false;

        if (!TryParseYearMonth(celdas[idxPeriodo], out anio, out var mes))
            return false;
        if (mes != 12)
            return false;

        return TryParseDecimal(celdas[idxAnual], out valor);
    }

    private static bool TryParseYearMonth(string periodo, out int anio, out int mes)
    {
        anio = default;
        mes = default;

        var p = periodo.Trim();

        if (DateTime.TryParse(p, out var fecha))
        {
            anio = fecha.Year;
            mes = fecha.Month;
            return true;
        }

        var m = PeriodoFechaRegex().Match(p);
        if (!m.Success)
            return false;

        if (!int.TryParse(m.Groups["anio"].Value, out anio))
            return false;
        if (!int.TryParse(m.Groups["mes"].Value, out mes))
            return false;

        return mes is >= 1 and <= 12;
    }

    private static int BuscarIndice(IReadOnlyList<string> headers, params string[] tokens)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            var h = headers[i].ToLowerInvariant();
            if (tokens.Any(t => h.Contains(t.ToLowerInvariant())))
                return i;
        }

        return -1;
    }

    private static bool EsDiciembre(string mes)
        => mes.StartsWith("dic") || mes.Equals("diciembre", StringComparison.OrdinalIgnoreCase);

    private static string Limpiar(string? valor)
        => HtmlEntity.DeEntitize(valor ?? string.Empty).Replace("\u00A0", " ").Trim();

    private static bool TryParseDecimal(string valor, out decimal numero)
    {
        var limpio = valor.Replace("%", string.Empty).Trim();
        return decimal.TryParse(limpio.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out numero)
               || decimal.TryParse(limpio, NumberStyles.Any, new CultureInfo("es-EC"), out numero);
    }

    [GeneratedRegex("^20\\d{2}$")]
    private static partial Regex YearRegex();

    [GeneratedRegex(@"(?<periodo>20\d{2}[-/](?<mes>0[1-9]|1[0-2])[-/]\d{2})\s*\|\s*(?<anual>[-+]?\d+(?:[\.,]\d+)?)", RegexOptions.Multiline)]
    private static partial Regex PeriodoAnualRegex();

    [GeneratedRegex(@"(?<anio>20\d{2})[-/](?<mes>0[1-9]|1[0-2])")]
    private static partial Regex PeriodoFechaRegex();

    [GeneratedRegex(@"(?<anio>20\d{2})\s*\|\s*(?<mes>[A-Za-zÁÉÍÓÚáéíóúÑñ]+)\s*\|\s*(?<mensual>[-+]?\d+(?:[\.,]\d+)?)\s*\|\s*(?<anual>[-+]?\d+(?:[\.,]\d+)?)\s*\|\s*(?<acum>[-+]?\d+(?:[\.,]\d+)?)", RegexOptions.Multiline)]
    private static partial Regex SerieHistoricaRegex();
}
