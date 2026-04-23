using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Inflacion;

public sealed class CrearInflacionAnualUseCase(
    IRepositorioInflacionAnual repositorioInflacionAnual,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    private const string FuenteBce = "BCE";
    private const string FuenteDatoHistorico = "Dato historico";
    private const string FuenteEstimacion = "Estimacion";
    private const string FuenteAjusteManual = "Ajuste manual";

    public async Task EjecutarAsync(
        CrearInflacionAnualDto dto,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionManualSave: inicio crear anio={dto.Anio}, valor={dto.PorcentajeInflacion}, usuario={usuarioId}");

        ValidarEntrada(dto.Anio, dto.PorcentajeInflacion);

        var yaExiste = await repositorioInflacionAnual.ExisteAnioAsync(dto.Anio, null, cancellationToken);
        if (yaExiste)
        {
            throw new InvalidOperationException($"Ya existe un registro de inflación para el año {dto.Anio}. Puede editar el valor existente.");
        }

        var fuente = NormalizarFuente(dto.FuenteNombre);
        var tipoFuente = NormalizarTipoFuente(dto.TipoFuente, fuente);
        var entidad = new InflacionAnual(dto.Anio, dto.PorcentajeInflacion, fuente, tipoFuente);

        try
        {
            await repositorioInflacionAnual.AgregarAsync(entidad, usuarioId, cancellationToken);
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var detalle = ObtenerDetalleDb(ex);
            Trace.TraceError($"[{DateTime.UtcNow:O}] InflacionDBError: crear falló. detalle={detalle}");
            throw new InvalidOperationException($"No se pudo guardar el registro de inflación. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Inflacion",
                entidadNombre: "InflacionAnual",
                entidadId: dto.Anio.ToString(),
                accionNombre: "CREAR",
                resumenTexto: $"Inflación anual registrada para {dto.Anio}: {dto.PorcentajeInflacion:N2}%.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // No bloquea operación principal si falla auditoría.
        }

        Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionManualSave: fin crear ok anio={dto.Anio}");
    }

    private static void ValidarEntrada(int anio, decimal porcentajeInflacion)
    {
        if (anio < 2000 || anio > 2100)
            throw new ArgumentException("El año debe estar entre 2000 y 2100.");

        if (porcentajeInflacion < -10m || porcentajeInflacion > 20m)
            throw new ArgumentException("La inflación anual debe estar en el rango de -10% a +20%.");
    }

    private static string NormalizarFuente(string? fuenteNombre)
    {
        if (string.IsNullOrWhiteSpace(fuenteNombre))
            return FuenteEstimacion;

        return fuenteNombre.Trim();
    }

    private static string NormalizarTipoFuente(string? tipoFuente, string fuenteNombre)
    {
        string valorNormalizado;
        if (string.IsNullOrWhiteSpace(tipoFuente))
        {
            valorNormalizado = fuenteNombre.Equals(FuenteBce, StringComparison.OrdinalIgnoreCase)
                ? FuenteDatoHistorico
                : FuenteEstimacion;
        }
        else
        {
            valorNormalizado = tipoFuente.Trim();
        }

        if (!valorNormalizado.Equals(FuenteDatoHistorico, StringComparison.OrdinalIgnoreCase)
            && !valorNormalizado.Equals(FuenteEstimacion, StringComparison.OrdinalIgnoreCase)
            && !valorNormalizado.Equals(FuenteAjusteManual, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El tipo de fuente debe ser 'Dato historico', 'Estimacion' o 'Ajuste manual'.");
        }

        return valorNormalizado;
    }

    private static int? NormalizarUsuarioId(int? usuarioId)
        => usuarioId.HasValue && usuarioId.Value > 0 ? usuarioId.Value : null;

    private static string ObtenerDetalleDb(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;
}
