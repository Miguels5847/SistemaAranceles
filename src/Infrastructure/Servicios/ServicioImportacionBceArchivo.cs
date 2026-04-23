using System.Globalization;
using System.Diagnostics;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using HtmlAgilityPack;
using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Infrastructure.Servicios;

public sealed partial class ServicioImportacionBceArchivo : IServicioImportacionBceArchivo
{
    public async Task<ImportacionInflacionLecturaResultadoDto> LeerArchivoAsync(
        ImportacionBceArchivoSolicitudDto solicitud,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(solicitud.RutaArchivo) || !File.Exists(solicitud.RutaArchivo))
            throw new FileNotFoundException("No se encontró el archivo BCE a importar.", solicitud.RutaArchivo);

        return await Task.Run(() =>
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCEArchivo: inicio. archivo={solicitud.RutaArchivo}, serie={solicitud.TipoSerie}");

            var filas = new List<ImportacionInflacionFilaDto>();
            var errores = new List<ImportacionInflacionErrorDto>();
            var extension = Path.GetExtension(solicitud.RutaArchivo).ToLowerInvariant();

            if (extension == ".xlsx")
            {
                ExtraerDesdeExcel(solicitud, filas, errores, cancellationToken);
            }
            else
            {
                ExtraerDesdeHtmlOTexto(solicitud, filas, errores, cancellationToken);
            }

            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCEArchivo: fin. filas_validas={filas.Count}, filas_error={errores.Count}");

            return new ImportacionInflacionLecturaResultadoDto
            {
                Filas = filas,
                Errores = errores
            };
        }, cancellationToken);
    }

    private static void ExtraerDesdeExcel(
        ImportacionBceArchivoSolicitudDto solicitud,
        ICollection<ImportacionInflacionFilaDto> filas,
        ICollection<ImportacionInflacionErrorDto> errores,
        CancellationToken cancellationToken)
    {
        using var workbook = new XLWorkbook(solicitud.RutaArchivo);

        if (workbook.Worksheets.Count == 0)
        {
            errores.Add(new ImportacionInflacionErrorDto
            {
                Fila = 0,
                Mensaje = "El archivo BCE no contiene hojas legibles.",
                Valor = solicitud.RutaArchivo
            });
            return;
        }

        var anioValor = new Dictionary<int, decimal>();
        var filasEscaneadas = 0;

        foreach (var hoja in workbook.Worksheets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? 0;
            var idxAnio = -1;
            var idxMes = -1;
            var idxAnual = -1;
            var idxAcum = -1;
            var idxPeriodo = -1;

            for (var fila = 1; fila <= ultimaFila; fila++)
            {
                var celdas = new List<string>();
                for (var columna = 1; columna <= Math.Min(8, hoja.LastColumnUsed()?.ColumnNumber() ?? 0); columna++)
                {
                    celdas.Add(Limpiar(hoja.Cell(fila, columna).GetString()));
                }

                if (celdas.All(string.IsNullOrWhiteSpace))
                    continue;

                if (idxAnio < 0 && idxMes < 0 && idxAnual < 0 && idxAcum < 0 && idxPeriodo < 0)
                {
                    if (TryDetectarEncabezados(celdas, out idxAnio, out idxMes, out idxAnual, out idxAcum, out idxPeriodo))
                        continue;
                }

                if (IntentarExtraerFila(celdas, solicitud.TipoSerie, idxAnio, idxMes, idxAnual, idxAcum, idxPeriodo, out var anio, out var valor))
                {
                    anioValor[anio] = valor;
                    filasEscaneadas++;
                }
            }
        }

        if (anioValor.Count == 0)
        {
            errores.Add(new ImportacionInflacionErrorDto
            {
                Fila = 0,
                Mensaje = $"No se encontraron datos BCE válidos en el archivo Excel para la serie {solicitud.TipoSerie}.",
                Valor = solicitud.RutaArchivo
            });
            return;
        }

        foreach (var (anio, valor) in anioValor.OrderBy(x => x.Key))
        {
            filas.Add(new ImportacionInflacionFilaDto
            {
                Fila = anio,
                Anio = anio,
                PorcentajeInflacion = valor,
                FuenteNombre = "BCE",
                TipoFuente = "Dato historico"
            });
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCEArchivo: excel escaneado. filas_escaneadas={filasEscaneadas}, anios_extraidos={anioValor.Count}");
    }

    private static void ExtraerDesdeHtmlOTexto(
        ImportacionBceArchivoSolicitudDto solicitud,
        ICollection<ImportacionInflacionFilaDto> filas,
        ICollection<ImportacionInflacionErrorDto> errores,
        CancellationToken cancellationToken)
    {
        var texto = File.ReadAllText(solicitud.RutaArchivo);
        var doc = new HtmlDocument();
        doc.LoadHtml(texto);

        var anioValor = new Dictionary<int, decimal>();
        var filasEscaneadas = 0;

        var tablas = doc.DocumentNode.SelectNodes("//table");
        if (tablas is not null && tablas.Count > 0)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCEArchivo: tablas_detectadas={tablas.Count}");

            foreach (var tabla in tablas)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExtraerDesdeTabla(tabla, solicitud.TipoSerie, anioValor, ref filasEscaneadas);
            }
        }

        if (anioValor.Count == 0)
        {
            ExtraerDesdeTextoCompleto(doc.DocumentNode.InnerText, solicitud.TipoSerie, anioValor, ref filasEscaneadas);
        }

        if (anioValor.Count == 0)
        {
            errores.Add(new ImportacionInflacionErrorDto
            {
                Fila = 0,
                Mensaje = $"No se pudo extraer datos BCE desde el archivo para la serie {solicitud.TipoSerie}. Verifique el formato descargado.",
                Valor = solicitud.RutaArchivo
            });
            return;
        }

        foreach (var (anio, valor) in anioValor.OrderBy(x => x.Key))
        {
            filas.Add(new ImportacionInflacionFilaDto
            {
                Fila = anio,
                Anio = anio,
                PorcentajeInflacion = valor,
                FuenteNombre = "BCE",
                TipoFuente = "Dato historico"
            });
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCEArchivo: html/texto escaneado. filas_escaneadas={filasEscaneadas}, anios_extraidos={anioValor.Count}");
    }

    private static void ExtraerDesdeTabla(HtmlNode tabla, TipoSerieInflacionBce tipoSerie, IDictionary<int, decimal> destino, ref int filasEscaneadas)
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

            if (IntentarExtraerFila(celdas, tipoSerie, idxAnio, idxMes, idxAnual, idxAcum, idxPeriodo, out var anio, out var valor))
            {
                destino[anio] = valor;
                filasEscaneadas++;
            }
        }
    }

    private static bool TryDetectarEncabezados(
        IReadOnlyList<string> celdas,
        out int idxAnio,
        out int idxMes,
        out int idxAnual,
        out int idxAcum,
        out int idxPeriodo)
    {
        idxAnio = BuscarIndice(celdas, "año", "anio");
        idxMes = BuscarIndice(celdas, "mes");
        idxAnual = BuscarIndice(celdas, "anual");
        idxAcum = BuscarIndice(celdas, "acumulada", "acumulado");
        idxPeriodo = BuscarIndice(celdas, "periodo", "período");

        return idxAnio >= 0 || idxPeriodo >= 0 || idxAnual >= 0 || idxAcum >= 0;
    }

    private static void ExtraerDesdeTextoCompleto(string texto, TipoSerieInflacionBce tipoSerie, IDictionary<int, decimal> destino, ref int filasEscaneadas)
    {
        var matches = SerieHistoricaRegex().Matches(texto);
        if (matches.Count > 0)
        {
            foreach (Match match in matches)
            {
                filasEscaneadas++;

                if (!int.TryParse(match.Groups["anio"].Value, out var anio))
                    continue;

                if (!EsDiciembre(match.Groups["mes"].Value))
                    continue;

                var valorTexto = tipoSerie == TipoSerieInflacionBce.Accumulated
                    ? match.Groups["acum"].Value
                    : match.Groups["anual"].Value;

                if (TryParseDecimal(valorTexto, out var valor))
                    destino[anio] = valor;
            }

            return;
        }

        var matchesPeriodo = PeriodoAnualRegex().Matches(texto);
        foreach (Match match in matchesPeriodo)
        {
            filasEscaneadas++;

            if (!TryParsePeriodo(match.Groups["periodo"].Value, out var anio, out var mes) || mes != 12)
                continue;

            if (tipoSerie != TipoSerieInflacionBce.Annual)
                continue;

            if (TryParseDecimal(match.Groups["anual"].Value, out var valor))
                destino[anio] = valor;
        }
    }

    private static bool IntentarExtraerFila(
        IReadOnlyList<string> celdas,
        TipoSerieInflacionBce tipoSerie,
        int idxAnio,
        int idxMes,
        int idxAnual,
        int idxAcum,
        int idxPeriodo,
        out int anio,
        out decimal valor)
    {
        anio = default;
        valor = default;

        if (idxAnio >= 0 && idxMes >= 0 && idxAnio < celdas.Count && idxMes < celdas.Count)
        {
            if (int.TryParse(celdas[idxAnio], out anio) && EsDiciembre(celdas[idxMes]))
            {
                var idxValor = tipoSerie == TipoSerieInflacionBce.Accumulated ? idxAcum : idxAnual;
                if (idxValor >= 0 && idxValor < celdas.Count && TryParseDecimal(celdas[idxValor], out valor))
                    return true;
            }
        }

        if (idxPeriodo >= 0 && idxAnual >= 0 && idxPeriodo < celdas.Count && idxAnual < celdas.Count)
        {
            if (tipoSerie == TipoSerieInflacionBce.Annual
                && TryParsePeriodo(celdas[idxPeriodo], out anio, out var mes)
                && mes == 12
                && TryParseDecimal(celdas[idxAnual], out valor))
            {
                return true;
            }
        }

        if (celdas.Count >= 2 && int.TryParse(celdas[0], out anio) && TryParseDecimal(celdas[1], out valor))
        {
            return true;
        }

        return false;
    }

    private static bool TryParsePeriodo(string valor, out int anio, out int mes)
    {
        anio = default;
        mes = default;

        var limpio = valor.Trim();
        if (DateTime.TryParse(limpio, out var fecha))
        {
            anio = fecha.Year;
            mes = fecha.Month;
            return true;
        }

        var match = PeriodoFechaRegex().Match(limpio);
        if (!match.Success)
            return false;

        return int.TryParse(match.Groups["anio"].Value, out anio)
               && int.TryParse(match.Groups["mes"].Value, out mes)
               && mes is >= 1 and <= 12;
    }

    private static bool EsDiciembre(string mes)
        => mes.StartsWith("dic", StringComparison.OrdinalIgnoreCase)
           || mes.Equals("diciembre", StringComparison.OrdinalIgnoreCase);

    private static string Limpiar(string? valor)
        => HtmlEntity.DeEntitize(valor ?? string.Empty).Replace("\u00A0", " ").Trim();

    private static bool TryParseDecimal(string valor, out decimal numero)
    {
        var limpio = valor.Replace("%", string.Empty).Trim();
        return decimal.TryParse(limpio.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out numero)
               || decimal.TryParse(limpio, NumberStyles.Any, new CultureInfo("es-EC"), out numero);
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

    [GeneratedRegex(@"(?<anio>20\d{2})\s*\|\s*(?<mes>[A-Za-zÁÉÍÓÚáéíóúÑñ]+)\s*\|\s*(?<mensual>[-+]?\d+(?:[\.,]\d+)?)\s*\|\s*(?<anual>[-+]?\d+(?:[\.,]\d+)?)\s*\|\s*(?<acum>[-+]?\d+(?:[\.,]\d+)?)", RegexOptions.Multiline)]
    private static partial Regex SerieHistoricaRegex();

    [GeneratedRegex(@"(?<periodo>20\d{2}[-/](?<mes>0[1-9]|1[0-2])[-/]\d{2})\s*\|\s*(?<anual>[-+]?\d+(?:[\.,]\d+)?)", RegexOptions.Multiline)]
    private static partial Regex PeriodoAnualRegex();

    [GeneratedRegex(@"(?<anio>20\d{2})[-/](?<mes>0[1-9]|1[0-2])")]
    private static partial Regex PeriodoFechaRegex();
}