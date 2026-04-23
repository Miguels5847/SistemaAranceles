using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using System.Diagnostics;
using CriterioDominio = SistemaAranceles.Domain.Entities.CriterioReferenciaRetencion;
using CriterioPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.CriterioReferenciaRetencion;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioCriterioReferenciaRetencion(ContextoAplicacion contextoAplicacion) : IRepositorioCriterioReferenciaRetencion
{
    public async Task<CriterioDominio?> ObtenerPorConfiguracionAsync(int configuracionRetencionId, CancellationToken cancellationToken = default)
    {
        var entidad = await contextoAplicacion.CriteriosReferenciaRetencion
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ConfiguracionRetencionId == configuracionRetencionId, cancellationToken);

        return entidad is null ? null : MapearADominio(entidad);
    }

    public async Task<CriterioDominio?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entidad = await contextoAplicacion.CriteriosReferenciaRetencion
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entidad is null ? null : MapearADominio(entidad);
    }

    public async Task<bool> ExistePorConfiguracionAsync(int configuracionRetencionId, CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.CriteriosReferenciaRetencion
            .AnyAsync(x => x.ConfiguracionRetencionId == configuracionRetencionId, cancellationToken);
    }

    public async Task AgregarAsync(CriterioDominio criterio, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criterio);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] CritRefDB: agregar configuracion={criterio.ConfiguracionRetencionId}, usuario={creadoPorUsuarioId}");

        var entidad = new CriterioPersistencia
        {
            ConfiguracionRetencionId = criterio.ConfiguracionRetencionId,
            MetaRetencionPorcentaje = criterio.MetaRetencionPorcentaje,
            MetaGraduacionPorcentaje = criterio.MetaGraduacionPorcentaje,
            CreadoEn = DateTime.UtcNow,
            CreadoPorUsuarioId = creadoPorUsuarioId,
            EstaActivo = true
        };

        await contextoAplicacion.CriteriosReferenciaRetencion.AddAsync(entidad, cancellationToken);
    }

    public async Task ActualizarAsync(CriterioDominio criterio, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(criterio);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] CritRefDB: actualizar id={criterio.Id}, usuario={actualizadoPorUsuarioId}");

        var existente = await contextoAplicacion.CriteriosReferenciaRetencion
            .FirstOrDefaultAsync(x => x.Id == criterio.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró el criterio de referencia con Id {criterio.Id}.");

        existente.MetaRetencionPorcentaje = criterio.MetaRetencionPorcentaje;
        existente.MetaGraduacionPorcentaje = criterio.MetaGraduacionPorcentaje;
        existente.ActualizadoEn = DateTime.UtcNow;
        existente.ActualizadoPorUsuarioId = actualizadoPorUsuarioId;
    }

    private static CriterioDominio MapearADominio(CriterioPersistencia entidad)
    {
        var dominio = new CriterioDominio(
            entidad.ConfiguracionRetencionId,
            entidad.MetaRetencionPorcentaje,
            entidad.MetaGraduacionPorcentaje);
        dominio.RehidratarId(entidad.Id);
        return dominio;
    }
}
