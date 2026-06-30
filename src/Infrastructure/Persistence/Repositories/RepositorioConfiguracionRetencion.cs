using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using System.Diagnostics;
using ConfiguracionRetencionDominio = SistemaAranceles.Domain.Entities.ConfiguracionRetencion;
using ConfiguracionRetencionPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.ConfiguracionRetencion;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioConfiguracionRetencion(ContextoAplicacion contextoAplicacion) : IRepositorioConfiguracionRetencion
{
    private static readonly SemaphoreSlim _gateEsquema = new(1, 1);
    private static bool _esquemaListo;

    // DDL self-healing (una vez por proceso): agrega las columnas de meta y, para filas previas,
    // las rellena desde la tasa por ciclo ya guardada (meta = tasa^pasos), manteniendo coherencia.
    private async Task AsegurarEsquemaAsync(CancellationToken cancellationToken)
    {
        if (_esquemaListo) return;
        await _gateEsquema.WaitAsync(cancellationToken);
        try
        {
            if (_esquemaListo) return;
            await contextoAplicacion.Database.ExecuteSqlRawAsync("""
            ALTER TABLE public.configuracion_retencion
                ADD COLUMN IF NOT EXISTS meta_retencion_porcentaje NUMERIC(9,4) NOT NULL DEFAULT 0,
                ADD COLUMN IF NOT EXISTS meta_graduacion_porcentaje NUMERIC(9,4) NOT NULL DEFAULT 0;

            UPDATE public.configuracion_retencion
               SET meta_retencion_porcentaje =
                     round(power(tasa_retencion_porcentaje / 100.0, greatest(1, total_ciclos / 2 - 1)) * 100, 4)
             WHERE meta_retencion_porcentaje = 0 AND tasa_retencion_porcentaje > 0;

            UPDATE public.configuracion_retencion
               SET meta_graduacion_porcentaje =
                     round(power(tasa_graduacion_porcentaje / 100.0, greatest(1, total_ciclos - total_ciclos / 2 - 1)) * 100, 4)
             WHERE meta_graduacion_porcentaje = 0 AND tasa_graduacion_porcentaje > 0;
            """, cancellationToken);
            _esquemaListo = true;
        }
        finally
        {
            _gateEsquema.Release();
        }
    }

    public async Task<IReadOnlyList<ConfiguracionRetencionDto>> ListarDtoAsync(CancellationToken cancellationToken = default)
    {
        await AsegurarEsquemaAsync(cancellationToken);
        var lista = await (from c in contextoAplicacion.ConfiguracionesRetencion.AsNoTracking()
                           where c.EstaActivo
                           join car in contextoAplicacion.Carreras.AsNoTracking() on c.CarreraId equals car.Id
                           join esc in contextoAplicacion.EscenariosProyeccion.AsNoTracking() on c.EscenarioProyeccionId equals esc.Id
                           orderby car.Nombre, esc.Nombre
                           select new ConfiguracionRetencionDto
                           {
                               Id = c.Id,
                               CarreraId = c.CarreraId,
                               CarreraNombre = car.Nombre,
                               CarreraCodigo = car.Codigo,
                               EscenarioProyeccionId = c.EscenarioProyeccionId,
                               EscenarioNombre = esc.Nombre,
                               TotalCiclos = c.TotalCiclos,
                               TasaRetencionPorcentaje = c.TasaRetencionPorcentaje,
                               TasaGraduacionPorcentaje = c.TasaGraduacionPorcentaje,
                               EstudiantesPeriodo1 = c.EstudiantesPeriodo1,
                               EstudiantesPeriodo2 = c.EstudiantesPeriodo2,
                               ParalelosPeriodo1 = c.ParalelosPeriodo1,
                               ParalelosPeriodo2 = c.ParalelosPeriodo2,
                               TieneCriterioReferencia = c.MetaRetencionPorcentaje > 0m || c.MetaGraduacionPorcentaje > 0m,
                               MetaRetencionPorcentaje = c.MetaRetencionPorcentaje,
                               MetaGraduacionPorcentaje = c.MetaGraduacionPorcentaje
                           }).ToListAsync(cancellationToken);

        return lista;
    }

    public async Task<ConfiguracionRetencionDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await AsegurarEsquemaAsync(cancellationToken);
        return await (from c in contextoAplicacion.ConfiguracionesRetencion.AsNoTracking()
                      where c.Id == id && c.EstaActivo
                      join car in contextoAplicacion.Carreras.AsNoTracking() on c.CarreraId equals car.Id
                      join esc in contextoAplicacion.EscenariosProyeccion.AsNoTracking() on c.EscenarioProyeccionId equals esc.Id
                      select new ConfiguracionRetencionDto
                      {
                          Id = c.Id,
                          CarreraId = c.CarreraId,
                          CarreraNombre = car.Nombre,
                          CarreraCodigo = car.Codigo,
                          EscenarioProyeccionId = c.EscenarioProyeccionId,
                          EscenarioNombre = esc.Nombre,
                          TotalCiclos = c.TotalCiclos,
                          TasaRetencionPorcentaje = c.TasaRetencionPorcentaje,
                          TasaGraduacionPorcentaje = c.TasaGraduacionPorcentaje,
                          EstudiantesPeriodo1 = c.EstudiantesPeriodo1,
                          EstudiantesPeriodo2 = c.EstudiantesPeriodo2,
                          ParalelosPeriodo1 = c.ParalelosPeriodo1,
                          ParalelosPeriodo2 = c.ParalelosPeriodo2,
                          TieneCriterioReferencia = c.MetaRetencionPorcentaje > 0m || c.MetaGraduacionPorcentaje > 0m,
                          MetaRetencionPorcentaje = c.MetaRetencionPorcentaje,
                          MetaGraduacionPorcentaje = c.MetaGraduacionPorcentaje
                      }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ConfiguracionRetencionDominio?> ObtenerDominioPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entidad = await contextoAplicacion.ConfiguracionesRetencion
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, cancellationToken);

        return entidad is null ? null : MapearADominio(entidad);
    }

    public async Task<ConfiguracionRetencionDominio?> ObtenerActivoPorCarreraYEscenarioNombreAsync(int carreraId, string escenarioNombre, CancellationToken cancellationToken = default)
    {
        var entidad = await (from c in contextoAplicacion.ConfiguracionesRetencion.AsNoTracking()
                             join esc in contextoAplicacion.EscenariosProyeccion.AsNoTracking() on c.EscenarioProyeccionId equals esc.Id
                             where c.CarreraId == carreraId && c.EstaActivo && esc.Nombre == escenarioNombre
                             select c)
                        .FirstOrDefaultAsync(cancellationToken);

        return entidad is null ? null : MapearADominio(entidad);
    }

    public async Task<bool> ExisteCombinacionAsync(int carreraId, int escenarioProyeccionId, int? excluirId = null, CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.ConfiguracionesRetencion
            .AnyAsync(x => x.CarreraId == carreraId
                        && x.EscenarioProyeccionId == escenarioProyeccionId
                        && x.EstaActivo
                        && (!excluirId.HasValue || x.Id != excluirId.Value),
                cancellationToken);
    }

    public async Task<int?> ObtenerIdCualquierEstadoPorCombinacionAsync(int carreraId, int escenarioProyeccionId, CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.ConfiguracionesRetencion
            .AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EscenarioProyeccionId == escenarioProyeccionId)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AgregarAsync(ConfiguracionRetencionDominio configuracion, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        await AsegurarEsquemaAsync(cancellationToken);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetDB: agregar carrera={configuracion.CarreraId}, escenario={configuracion.EscenarioProyeccionId}, usuario={creadoPorUsuarioId}");

        var entidad = new ConfiguracionRetencionPersistencia
        {
            CarreraId = configuracion.CarreraId,
            EscenarioProyeccionId = configuracion.EscenarioProyeccionId,
            TotalCiclos = configuracion.TotalCiclos,
            TasaRetencionPorcentaje = configuracion.TasaRetencionPorcentaje,
            TasaGraduacionPorcentaje = configuracion.TasaGraduacionPorcentaje,
            MetaRetencionPorcentaje = configuracion.MetaRetencionPorcentaje,
            MetaGraduacionPorcentaje = configuracion.MetaGraduacionPorcentaje,
            EstudiantesPeriodo1 = configuracion.EstudiantesPeriodo1,
            EstudiantesPeriodo2 = configuracion.EstudiantesPeriodo2,
            ParalelosPeriodo1 = configuracion.ParalelosPeriodo1,
            ParalelosPeriodo2 = configuracion.ParalelosPeriodo2,
            CreadoEn = DateTime.UtcNow,
            CreadoPorUsuarioId = creadoPorUsuarioId,
            EstaActivo = true
        };

        await contextoAplicacion.ConfiguracionesRetencion.AddAsync(entidad, cancellationToken);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetDB: agregado pendiente id-temp={entidad.Id}");
    }

    public async Task ActualizarAsync(ConfiguracionRetencionDominio configuracion, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        await AsegurarEsquemaAsync(cancellationToken);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetDB: actualizar id={configuracion.Id}, usuario={actualizadoPorUsuarioId}");

        var existente = await contextoAplicacion.ConfiguracionesRetencion
            .FirstOrDefaultAsync(x => x.Id == configuracion.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la configuración de retención con Id {configuracion.Id}.");

        existente.CarreraId = configuracion.CarreraId;
        existente.EscenarioProyeccionId = configuracion.EscenarioProyeccionId;
        existente.TotalCiclos = configuracion.TotalCiclos;
        existente.TasaRetencionPorcentaje = configuracion.TasaRetencionPorcentaje;
        existente.TasaGraduacionPorcentaje = configuracion.TasaGraduacionPorcentaje;
        existente.MetaRetencionPorcentaje = configuracion.MetaRetencionPorcentaje;
        existente.MetaGraduacionPorcentaje = configuracion.MetaGraduacionPorcentaje;
        existente.EstudiantesPeriodo1 = configuracion.EstudiantesPeriodo1;
        existente.EstudiantesPeriodo2 = configuracion.EstudiantesPeriodo2;
        existente.ParalelosPeriodo1 = configuracion.ParalelosPeriodo1;
        existente.ParalelosPeriodo2 = configuracion.ParalelosPeriodo2;
        existente.ActualizadoEn = DateTime.UtcNow;
        existente.ActualizadoPorUsuarioId = actualizadoPorUsuarioId;

        // Reactivación: si la fila estaba borrada lógicamente (revivir combinación), vuelve a
        // quedar activa. Para una edición normal (ya activa) esto es un no-op.
        existente.EstaActivo = true;
        existente.EliminadoEn = null;
        existente.EliminadoPorUsuarioId = null;
    }

    public async Task EliminarPorIdAsync(int id, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetDB: eliminar id={id}, usuario={eliminadoPorUsuarioId}");

        var existente = await contextoAplicacion.ConfiguracionesRetencion
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la configuración de retención con Id {id}.");

        existente.EstaActivo = false;
        existente.EliminadoEn = DateTime.UtcNow;
        existente.EliminadoPorUsuarioId = eliminadoPorUsuarioId;
    }

    private static ConfiguracionRetencionDominio MapearADominio(ConfiguracionRetencionPersistencia entidad)
    {
        var dominio = new ConfiguracionRetencionDominio(
            entidad.CarreraId,
            entidad.EscenarioProyeccionId,
            entidad.TotalCiclos,
            entidad.TasaRetencionPorcentaje,
            entidad.TasaGraduacionPorcentaje);

        dominio.ActualizarBaseEstudiantes(
            entidad.EstudiantesPeriodo1,
            entidad.EstudiantesPeriodo2,
            entidad.ParalelosPeriodo1,
            entidad.ParalelosPeriodo2);

        dominio.CargarMetas(entidad.MetaRetencionPorcentaje, entidad.MetaGraduacionPorcentaje);
        dominio.RehidratarId(entidad.Id);
        return dominio;
    }
}
