using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Inflacion;

public sealed class ImportarInflacionUseCase(
    IServicioImportacionExcel servicioImportacionExcel,
    IRepositorioInflacionAnual repositorioInflacionAnual,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<ImportacionInflacionResultadoDto> EjecutarAsync(
        string rutaArchivo,
        int? ejecutadoPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        int? usuarioId = ejecutadoPorUsuarioId.HasValue && ejecutadoPorUsuarioId.Value > 0 ? ejecutadoPorUsuarioId.Value : null;
        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportExcel: inicio. archivo={rutaArchivo}, usuario={usuarioId}");

        if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo))
            throw new FileNotFoundException("No se encontró el archivo a importar.", rutaArchivo);

        var extension = Path.GetExtension(rutaArchivo).ToLowerInvariant();
        ImportacionInflacionLecturaResultadoDto lectura = extension switch
        {
            ".xlsx" => await servicioImportacionExcel.LeerExcelAsync(rutaArchivo, cancellationToken),
            ".csv" => await servicioImportacionExcel.LeerCsvAsync(rutaArchivo, cancellationToken),
            _ => throw new InvalidOperationException("Formato no soportado. Use .xlsx o .csv.")
        };

        var errores = new List<ImportacionInflacionErrorDto>(lectura.Errores);
        var omitidos = new List<ImportacionInflacionErrorDto>();
        var filasCorrectas = 0;
        var aniosProcesados = new HashSet<int>();

        foreach (var fila in lectura.Filas)
        {
            if (!aniosProcesados.Add(fila.Anio))
            {
                errores.Add(new ImportacionInflacionErrorDto
                {
                    Fila = fila.Fila,
                    Mensaje = $"Año duplicado dentro del archivo: {fila.Anio}.",
                    Valor = fila.Anio.ToString()
                });
                continue;
            }

            if (fila.PorcentajeInflacion < -10m || fila.PorcentajeInflacion > 20m)
            {
                errores.Add(new ImportacionInflacionErrorDto
                {
                    Fila = fila.Fila,
                    Mensaje = "Porcentaje fuera de rango permitido (-10 a 20).",
                    Valor = fila.PorcentajeInflacion.ToString("0.####")
                });
                continue;
            }

            var duplicadoBd = await repositorioInflacionAnual.ExisteAnioAsync(fila.Anio, null, cancellationToken);
            if (duplicadoBd)
            {
                omitidos.Add(new ImportacionInflacionErrorDto
                {
                    Fila = fila.Fila,
                    Mensaje = $"Omitido: ya existe un registro para el año {fila.Anio}.",
                    Valor = fila.Anio.ToString()
                });
                continue;
            }

            var entidad = new InflacionAnual(
                fila.Anio,
                fila.PorcentajeInflacion,
                fila.FuenteNombre,
                fila.TipoFuente);

            await repositorioInflacionAnual.AgregarAsync(entidad, ejecutadoPorUsuarioId, cancellationToken);
            filasCorrectas++;
        }

        try
        {
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var detalle = ex.InnerException?.Message ?? ex.Message;
            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionDBError: import excel guardado falló. detalle={detalle}");
            throw new InvalidOperationException($"Error al guardar importación de inflación. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Inflacion",
                entidadNombre: "ImportacionInflacion",
                entidadId: DateTime.UtcNow.Ticks.ToString(),
                accionNombre: "IMPORTAR",
                resumenTexto: $"Importación de inflación completada. Correctas: {filasCorrectas}, omitidas: {omitidos.Count}, errores: {errores.Count}.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // No bloquear flujo por auditoría.
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportExcel: fin. procesadas={lectura.Filas.Count + lectura.Errores.Count}, correctas={filasCorrectas}, omitidas={omitidos.Count}, errores={errores.Count}");

        return new ImportacionInflacionResultadoDto
        {
            TotalFilasProcesadas = lectura.Filas.Count + lectura.Errores.Count,
            FilasCorrectas = filasCorrectas,
            FilasOmitidas = omitidos.Count,
            FilasConError = errores.Count,
            DetalleErrores = errores
        };
    }
}
