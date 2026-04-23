using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Inflacion;

public sealed class ImportarInflacionBceArchivoUseCase(
    IServicioImportacionBceArchivo servicioImportacionBceArchivo,
    IRepositorioInflacionAnual repositorioInflacionAnual,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    public async Task<ImportacionInflacionResultadoDto> EjecutarAsync(
        ImportacionBceArchivoSolicitudDto solicitud,
        int? ejecutadoPorUsuarioId,
        CancellationToken cancellationToken = default)
    {
        int? usuarioId = ejecutadoPorUsuarioId.HasValue && ejecutadoPorUsuarioId.Value > 0 ? ejecutadoPorUsuarioId.Value : null;
        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCEArchivo: inicio usecase. archivo={solicitud.RutaArchivo}, usuario={usuarioId}, serie={solicitud.TipoSerie}");

        var lectura = await servicioImportacionBceArchivo.LeerArchivoAsync(solicitud, cancellationToken);

        var errores = new List<ImportacionInflacionErrorDto>(lectura.Errores);
        var omitidos = new List<ImportacionInflacionErrorDto>();
        var filasCorrectas = 0;

        foreach (var fila in lectura.Filas)
        {
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

            var existente = await repositorioInflacionAnual.ObtenerPorAnioAsync(fila.Anio, cancellationToken);
            if (existente is not null)
            {
                omitidos.Add(new ImportacionInflacionErrorDto
                {
                    Fila = fila.Fila,
                    Mensaje = $"Omitido: año {fila.Anio} ya existe. No se sobrescribió.",
                    Valor = fila.Anio.ToString()
                });
                continue;
            }

            await repositorioInflacionAnual.AgregarAsync(
                new InflacionAnual(fila.Anio, fila.PorcentajeInflacion, "BCE", "Dato historico"),
                usuarioId,
                cancellationToken);
            filasCorrectas++;
        }

        try
        {
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var detalle = ex.InnerException?.Message ?? ex.Message;
            Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionDBError: import bce archivo guardado falló. detalle={detalle}");
            throw new InvalidOperationException($"Error al guardar importación BCE. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Inflacion",
                entidadNombre: "ImportacionBCEArchivo",
                entidadId: DateTime.UtcNow.Ticks.ToString(),
                accionNombre: "IMPORTAR_BCE_ARCHIVO",
                resumenTexto: $"Importación BCE por archivo completada. Correctas: {filasCorrectas}, omitidas: {omitidos.Count}, errores: {errores.Count}.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] InflacionImportBCEArchivo: fin usecase. procesadas={lectura.Filas.Count + lectura.Errores.Count}, correctas={filasCorrectas}, omitidas={omitidos.Count}, errores={errores.Count}");

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