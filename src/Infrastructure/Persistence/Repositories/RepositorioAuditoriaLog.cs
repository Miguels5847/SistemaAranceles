using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Data;
using System.Text;
using System.Globalization;
using SistemaAranceles.Application.DTOs.Auditoria;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioAuditoriaLog(ContextoAplicacion contextoAplicacion) : IRepositorioAuditoriaLog
{
    public async Task AgregarAsync(
        string moduloNombre,
        string entidadNombre,
        string entidadId,
        string accionNombre,
        string resumenTexto,
        int? ejecutadoPorUsuarioId = null,
        string? valoresAnterioresJson = null,
        string? valoresNuevosJson = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditoriaLog
        {
            EventoEn = DateTime.UtcNow,
            ModuloNombre = moduloNombre,
            EntidadNombre = entidadNombre,
            EntidadId = entidadId,
            AccionNombre = accionNombre,
            ResumenTexto = resumenTexto,
            EjecutadoPorUsuarioId = ejecutadoPorUsuarioId,
            ValoresAnterioresJson = valoresAnterioresJson,
            ValoresNuevosJson = valoresNuevosJson
        };

        await contextoAplicacion.AuditoriasLog.AddAsync(log, cancellationToken);
        await contextoAplicacion.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuditoriaConsultaResultadoDto> ConsultarAsync(
        AuditoriaFiltroDto filtros,
        CancellationToken cancellationToken = default)
    {
        var swTotal = Stopwatch.StartNew();
        var numeroPagina = filtros.NumeroPagina <= 0 ? 1 : filtros.NumeroPagina;
        var tamanoPagina = filtros.TamanoPagina <= 0 ? 25 : Math.Min(filtros.TamanoPagina, 100);

        Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: inicio ConsultarAsync. UsuarioId={filtros.UsuarioId?.ToString() ?? "null"}, FechaDesdeUtc={filtros.FechaDesdeUtc:O}, FechaHastaUtc={filtros.FechaHastaUtc:O}, Modulo='{filtros.ModuloNombre}', Accion='{filtros.AccionNombre}', Pagina={numeroPagina}, Tamano={tamanoPagina}.");
        var sqlBuilder = new StringBuilder(@"
SELECT
        al.id,
        al.evento_en,
        al.modulo_nombre,
        al.entidad_nombre,
        al.entidad_id,
        al.accion_nombre,
        al.resumen_texto,
        al.ejecutado_por_usuario_id
FROM public.auditoria_log al
WHERE 1=1");

        var modulo = string.IsNullOrWhiteSpace(filtros.ModuloNombre) ? null : filtros.ModuloNombre.Trim();
        var accion = string.IsNullOrWhiteSpace(filtros.AccionNombre) ? null : filtros.AccionNombre.Trim();
        var fechaDesde = filtros.FechaDesdeUtc.HasValue ? DateTime.SpecifyKind(filtros.FechaDesdeUtc.Value, DateTimeKind.Utc) : (DateTime?)null;
        var fechaHasta = filtros.FechaHastaUtc.HasValue ? DateTime.SpecifyKind(filtros.FechaHastaUtc.Value, DateTimeKind.Utc) : (DateTime?)null;

        if (filtros.UsuarioId.HasValue)
            sqlBuilder.Append($" AND al.ejecutado_por_usuario_id = {filtros.UsuarioId.Value}");
        if (fechaDesde.HasValue)
            sqlBuilder.Append($" AND al.evento_en >= TIMESTAMP '{fechaDesde.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture)}'");
        if (fechaHasta.HasValue)
            sqlBuilder.Append($" AND al.evento_en <= TIMESTAMP '{fechaHasta.Value.ToString("yyyy-MM-dd HH:mm:ss.ffffff", CultureInfo.InvariantCulture)}'");
        if (!string.IsNullOrWhiteSpace(modulo))
            sqlBuilder.Append($" AND al.modulo_nombre = '{EscapeSqlLiteral(modulo)}'");
        if (!string.IsNullOrWhiteSpace(accion))
            sqlBuilder.Append($" AND al.accion_nombre = '{EscapeSqlLiteral(accion)}'");

        var offset = (numeroPagina - 1) * tamanoPagina;
        sqlBuilder.Append($" ORDER BY al.id DESC LIMIT {tamanoPagina + 1} OFFSET {offset};");
        var sql = sqlBuilder.ToString();

        var items = new List<AuditoriaLogItemDto>(tamanoPagina + 1);
        var swPage = Stopwatch.StartNew();
        var connection = contextoAplicacion.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: estado conexión inicial={connection.State}. shouldClose={shouldClose}.");

        if (shouldClose)
        {
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: abriendo conexión...");
            await connection.OpenAsync(cancellationToken);
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: conexión abierta. estado={connection.State}.");
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 30;
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: comando creado. timeout_s={command.CommandTimeout}.");

            Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: ejecutando ExecuteReaderAsync...");

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: ExecuteReaderAsync completado. iniciando lectura de filas...");
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new AuditoriaLogItemDto
                {
                    Id = reader.GetInt32(0),
                    EventoEnUtc = DateTime.SpecifyKind(reader.GetDateTime(1), DateTimeKind.Utc),
                    ModuloNombre = reader.GetString(2),
                    EntidadNombre = reader.GetString(3),
                    EntidadId = reader.GetString(4),
                    AccionNombre = reader.GetString(5),
                    ResumenTexto = reader.GetString(6),
                    EjecutadoPorUsuarioId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    EjecutadoPorUsuarioNombre = reader.IsDBNull(7) ? "Sistema" : $"Usuario #{reader.GetInt32(7)}"
                });
            }
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: lectura completada. filas={items.Count}.");
        }
        finally
        {
            if (shouldClose)
            {
                Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: cerrando conexión...");
                await connection.CloseAsync();
                Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: conexión cerrada. estado={connection.State}.");
            }
        }

        swPage.Stop();
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: página base obtenida. items_ext={items.Count}, page_ms={swPage.ElapsedMilliseconds}.");

        var haySiguientePagina = items.Count > tamanoPagina;
        if (haySiguientePagina)
            items = items.Take(tamanoPagina).ToList();

        if (items.Count == 0)
        {
            swTotal.Stop();
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: fin ConsultarAsync sin resultados. total_ms={swTotal.ElapsedMilliseconds}.");
            return new AuditoriaConsultaResultadoDto
            {
                Items = [],
                TotalRegistros = 0,
                NumeroPagina = numeroPagina,
                TamanoPagina = tamanoPagina
            };
        }

        // Paginación sin CountAsync global: usa un total mínimo consistente.
        var totalRegistros = haySiguientePagina
            ? (numeroPagina * tamanoPagina) + 1
            : ((numeroPagina - 1) * tamanoPagina) + items.Count;

        swTotal.Stop();
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] RepositorioAuditoriaLog: fin ConsultarAsync. total_ms={swTotal.ElapsedMilliseconds}, items={items.Count}, total_paginas={(tamanoPagina <= 0 ? 0 : (int)Math.Ceiling((double)totalRegistros / tamanoPagina))}.");

        return new AuditoriaConsultaResultadoDto
        {
            Items = items,
            TotalRegistros = totalRegistros,
            NumeroPagina = numeroPagina,
            TamanoPagina = tamanoPagina
        };
    }

    private static string EscapeSqlLiteral(string value)
        => value.Replace("'", "''");
}
