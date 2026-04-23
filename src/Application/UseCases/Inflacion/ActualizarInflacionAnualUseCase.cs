using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using System.Diagnostics;

namespace SistemaAranceles.Application.UseCases.Inflacion;

public sealed class ActualizarInflacionAnualUseCase(
    IRepositorioInflacionAnual repositorioInflacionAnual,
    IUnidadTrabajo unidadTrabajo,
    IAuditoriaServicio auditoriaServicio)
{
    private const string FuenteBce = "BCE";
    private const string FuenteDatoHistorico = "Dato historico";
    private const string FuenteEstimacion = "Estimacion";
    private const string FuenteAjusteManual = "Ajuste manual";

    public async Task EjecutarAsync(
        ActualizarInflacionAnualDto dto,
        int? ejecutadoPorUsuarioId = null,
        CancellationToken cancellationToken = default)
    {
        var usuarioId = NormalizarUsuarioId(ejecutadoPorUsuarioId);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionManualSave: inicio actualizar id={dto.Id}, anio={dto.Anio}, valor={dto.PorcentajeInflacion}, usuario={usuarioId}");

        ValidarEntrada(dto.Anio, dto.PorcentajeInflacion);

        var entidad = await repositorioInflacionAnual.ObtenerPorIdAsync(dto.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el registro de inflación con Id {dto.Id}.");

        var anioDuplicado = await repositorioInflacionAnual.ExisteAnioAsync(dto.Anio, dto.Id, cancellationToken);
        if (anioDuplicado)
            throw new InvalidOperationException($"Ya existe otro registro para el año {dto.Anio}.");

        var eraProyectado = entidad.TipoFuente.Equals(FuenteEstimacion, StringComparison.OrdinalIgnoreCase);

        entidad.CambiarAnio(dto.Anio);
        entidad.CambiarPorcentajeInflacion(dto.PorcentajeInflacion);
        var tipoFuente = eraProyectado
            ? "Ajuste manual"
            : NormalizarTipoFuente(dto.TipoFuente, dto.FuenteNombre);
        entidad.CambiarFuente(NormalizarFuente(dto.FuenteNombre), tipoFuente);

        try
        {
            await repositorioInflacionAnual.ActualizarAsync(entidad, usuarioId, cancellationToken);
            await unidadTrabajo.GuardarCambiosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            var detalle = ObtenerDetalleDb(ex);
            Trace.TraceError($"[{DateTime.UtcNow:O}] InflacionDBError: actualizar falló. detalle={detalle}");
            throw new InvalidOperationException($"No se pudo actualizar el registro de inflación. Detalle: {detalle}");
        }

        try
        {
            await auditoriaServicio.RegistrarAsync(
                moduloNombre: "Inflacion",
                entidadNombre: "InflacionAnual",
                entidadId: dto.Id.ToString(),
                accionNombre: "ACTUALIZAR",
                resumenTexto: $"Inflación anual actualizada para {dto.Anio}: {dto.PorcentajeInflacion:N2}%.",
                ejecutadoPorUsuarioId: usuarioId,
                cancellationToken: cancellationToken);
        }
        catch
        {
            // No bloquea operación principal si falla auditoría.
        }

        Trace.TraceInformation($"[{DateTime.UtcNow:O}] InflacionManualSave: fin actualizar ok id={dto.Id}");
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

    private static string NormalizarTipoFuente(string? tipoFuente, string? fuenteNombre)
    {
        var fuente = fuenteNombre?.Trim() ?? string.Empty;
        string valorNormalizado;
        if (string.IsNullOrWhiteSpace(tipoFuente))
        {
            valorNormalizado = fuente.Equals(FuenteBce, StringComparison.OrdinalIgnoreCase)
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
