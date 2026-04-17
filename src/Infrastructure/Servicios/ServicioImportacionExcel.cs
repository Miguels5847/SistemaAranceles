using System.Globalization;
using System.Diagnostics;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Servicios;

namespace SistemaAranceles.Infrastructure.Servicios;

public sealed class ServicioImportacionExcel : IServicioImportacionExcel
{
    private const string FormatoEsperadoMensaje = "El Excel debe tener una sola hoja con las siguientes columnas: Columna A: AÑO (entero), Columna B: INFLACIÓN ANUAL (%) (decimal), Columna C: FUENTE (texto opcional).";

    public async Task<ImportacionInflacionLecturaResultadoDto> LeerExcelAsync(string rutaArchivo, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var filas = new List<ImportacionInflacionFilaDto>();
            var errores = new List<ImportacionInflacionErrorDto>();

            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportExcel: inicio lectura excel. archivo={rutaArchivo}");

            using var workbook = new XLWorkbook(rutaArchivo);
            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportExcel: hojas_detectadas={workbook.Worksheets.Count}");

            if (workbook.Worksheets.Count != 1)
            {
                throw new InvalidOperationException(FormatoEsperadoMensaje);
            }

            var worksheet = workbook.Worksheets.First();
            var ultimaFila = worksheet.LastRowUsed()?.RowNumber() ?? 0;
            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportExcel: ultima_fila={ultimaFila}");

            for (var fila = 1; fila <= ultimaFila; fila++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var anioTexto = worksheet.Cell(fila, 1).GetString().Trim();
                var inflacionTexto = worksheet.Cell(fila, 2).GetString().Trim();
                var fuenteTexto = worksheet.Cell(fila, 3).GetString().Trim();

                if (string.IsNullOrWhiteSpace(anioTexto)
                    && string.IsNullOrWhiteSpace(inflacionTexto)
                    && string.IsNullOrWhiteSpace(fuenteTexto))
                {
                    continue;
                }

                if (fila == 1 && !int.TryParse(anioTexto, out _))
                    continue;

                if (!int.TryParse(anioTexto, out var anio))
                {
                    errores.Add(new ImportacionInflacionErrorDto { Fila = fila, Mensaje = "Año inválido.", Valor = anioTexto });
                    continue;
                }

                if (!TryParseDecimalFlexible(inflacionTexto, out var porcentaje))
                {
                    errores.Add(new ImportacionInflacionErrorDto { Fila = fila, Mensaje = "Porcentaje inválido.", Valor = inflacionTexto });
                    continue;
                }

                var fuente = string.IsNullOrWhiteSpace(fuenteTexto) ? "Importado" : fuenteTexto;
                if (!EsFuenteValida(fuente))
                {
                    errores.Add(new ImportacionInflacionErrorDto { Fila = fila, Mensaje = "Fuente no válida (BCE, Manual, Importado).", Valor = fuenteTexto });
                    continue;
                }

                if (fuente.Length > 100)
                {
                    errores.Add(new ImportacionInflacionErrorDto { Fila = fila, Mensaje = "Fuente demasiado larga (máx. 100).", Valor = fuenteTexto });
                    continue;
                }

                filas.Add(new ImportacionInflacionFilaDto
                {
                    Fila = fila,
                    Anio = anio,
                    PorcentajeInflacion = porcentaje,
                    FuenteNombre = fuente,
                    TipoFuente = "Dato historico"
                });
            }

            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportExcel: fin lectura excel. filas_validas={filas.Count}, filas_error={errores.Count}");

            return new ImportacionInflacionLecturaResultadoDto
            {
                Filas = filas,
                Errores = errores
            };
        }, cancellationToken);
    }

    public async Task<ImportacionInflacionLecturaResultadoDto> LeerCsvAsync(string rutaArchivo, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var filas = new List<ImportacionInflacionFilaDto>();
            var errores = new List<ImportacionInflacionErrorDto>();

            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportExcel: inicio lectura csv. archivo={rutaArchivo}");

            using var reader = new StreamReader(rutaArchivo);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                BadDataFound = null,
                MissingFieldFound = null,
                HeaderValidated = null
            };

            using var csv = new CsvReader(reader, config);
            var rowIndex = 0;

            while (csv.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                rowIndex++;

                var anioTexto = csv.GetField(0)?.Trim() ?? string.Empty;
                var inflacionTexto = csv.GetField(1)?.Trim() ?? string.Empty;
                var fuenteTexto = (csv.TryGetField(2, out string? fuenteLeida) ? fuenteLeida : string.Empty)?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(anioTexto)
                    && string.IsNullOrWhiteSpace(inflacionTexto)
                    && string.IsNullOrWhiteSpace(fuenteTexto))
                {
                    continue;
                }

                if (rowIndex == 1 && !int.TryParse(anioTexto, out _))
                    continue;

                if (!int.TryParse(anioTexto, out var anio))
                {
                    errores.Add(new ImportacionInflacionErrorDto { Fila = rowIndex, Mensaje = "Año inválido.", Valor = anioTexto });
                    continue;
                }

                if (!TryParseDecimalFlexible(inflacionTexto, out var porcentaje))
                {
                    errores.Add(new ImportacionInflacionErrorDto { Fila = rowIndex, Mensaje = "Porcentaje inválido.", Valor = inflacionTexto });
                    continue;
                }

                var fuente = string.IsNullOrWhiteSpace(fuenteTexto) ? "Importado" : fuenteTexto;
                if (!EsFuenteValida(fuente))
                {
                    errores.Add(new ImportacionInflacionErrorDto { Fila = rowIndex, Mensaje = "Fuente no válida (BCE, Manual, Importado).", Valor = fuenteTexto });
                    continue;
                }

                if (fuente.Length > 100)
                {
                    errores.Add(new ImportacionInflacionErrorDto { Fila = rowIndex, Mensaje = "Fuente demasiado larga (máx. 100).", Valor = fuenteTexto });
                    continue;
                }

                filas.Add(new ImportacionInflacionFilaDto
                {
                    Fila = rowIndex,
                    Anio = anio,
                    PorcentajeInflacion = porcentaje,
                    FuenteNombre = fuente,
                    TipoFuente = "Dato historico"
                });
            }

            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportExcel: fin lectura csv. filas_validas={filas.Count}, filas_error={errores.Count}");

            return new ImportacionInflacionLecturaResultadoDto
            {
                Filas = filas,
                Errores = errores
            };
        }, cancellationToken);
    }

    private static bool EsFuenteValida(string fuente)
        => fuente.Equals("BCE", StringComparison.OrdinalIgnoreCase)
           || fuente.Equals("Manual", StringComparison.OrdinalIgnoreCase)
           || fuente.Equals("Importado", StringComparison.OrdinalIgnoreCase)
           || fuente.Equals("Estimacion", StringComparison.OrdinalIgnoreCase);

    private static bool TryParseDecimalFlexible(string valor, out decimal resultado)
    {
        var limpio = valor.Replace("%", string.Empty).Trim();
        return decimal.TryParse(limpio.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out resultado)
               || decimal.TryParse(limpio, NumberStyles.Any, new CultureInfo("es-EC"), out resultado);
    }
}
