using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using System.Diagnostics;
using ConfiguracionRetencionDominio = SistemaAranceles.Domain.Entities.ConfiguracionRetencion;
using ConfiguracionRetencionPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.ConfiguracionRetencion;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioConfiguracionRetencion(ContextoAplicacion contextoAplicacion) : IRepositorioConfiguracionRetencion
{
    public async Task<IReadOnlyList<ConfiguracionRetencionDto>> ListarDtoAsync(CancellationToken cancellationToken = default)
    {
        var lista = await (from c in contextoAplicacion.ConfiguracionesRetencion.AsNoTracking()
                           where c.EstaActivo
                           join car in contextoAplicacion.Carreras.AsNoTracking() on c.CarreraId equals car.Id
                           join esc in contextoAplicacion.EscenariosProyeccion.AsNoTracking() on c.EscenarioProyeccionId equals esc.Id
                           from cr in contextoAplicacion.CriteriosReferenciaRetencion.AsNoTracking()
                                       .Where(x => x.ConfiguracionRetencionId == c.Id).DefaultIfEmpty()
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
                               TieneCriterioReferencia = cr != null,
                               MetaRetencionPorcentaje = cr != null ? (decimal?)cr.MetaRetencionPorcentaje : null,
                               MetaGraduacionPorcentaje = cr != null ? (decimal?)cr.MetaGraduacionPorcentaje : null
                           }).ToListAsync(cancellationToken);

        return lista;
    }

    public async Task<ConfiguracionRetencionDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await (from c in contextoAplicacion.ConfiguracionesRetencion.AsNoTracking()
                      where c.Id == id && c.EstaActivo
                      join car in contextoAplicacion.Carreras.AsNoTracking() on c.CarreraId equals car.Id
                      join esc in contextoAplicacion.EscenariosProyeccion.AsNoTracking() on c.EscenarioProyeccionId equals esc.Id
                      from cr in contextoAplicacion.CriteriosReferenciaRetencion.AsNoTracking()
                                  .Where(x => x.ConfiguracionRetencionId == c.Id).DefaultIfEmpty()
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
                          TieneCriterioReferencia = cr != null,
                          MetaRetencionPorcentaje = cr != null ? (decimal?)cr.MetaRetencionPorcentaje : null,
                          MetaGraduacionPorcentaje = cr != null ? (decimal?)cr.MetaGraduacionPorcentaje : null
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

    public async Task AgregarAsync(ConfiguracionRetencionDominio configuracion, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetDB: agregar carrera={configuracion.CarreraId}, escenario={configuracion.EscenarioProyeccionId}, usuario={creadoPorUsuarioId}");

        var entidad = new ConfiguracionRetencionPersistencia
        {
            CarreraId = configuracion.CarreraId,
            EscenarioProyeccionId = configuracion.EscenarioProyeccionId,
            TotalCiclos = configuracion.TotalCiclos,
            TasaRetencionPorcentaje = configuracion.TasaRetencionPorcentaje,
            TasaGraduacionPorcentaje = configuracion.TasaGraduacionPorcentaje,
            EstudiantesPeriodo1 = configuracion.EstudiantesPeriodo1,
            EstudiantesPeriodo2 = configuracion.EstudiantesPeriodo2,
            ParalelosPeriodo1 = configuracion.ParalelosPeriodo1,
            ParalelosPeriodo2 = configuracion.ParalelosPeriodo2,
            CreadoEn = DateTime.UtcNow,
            CreadoPorUsuarioId = creadoPorUsuarioId,
            EstaActivo = true
        };

        await contextoAplicacion.ConfiguracionesRetencion.AddAsync(entidad, cancellationToken);
        configuracion.RehidratarId(entidad.Id);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetDB: agregado id={entidad.Id}");
    }

    public async Task ActualizarAsync(ConfiguracionRetencionDominio configuracion, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuracion);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] ConfRetDB: actualizar id={configuracion.Id}, usuario={actualizadoPorUsuarioId}");

        var existente = await contextoAplicacion.ConfiguracionesRetencion
            .FirstOrDefaultAsync(x => x.Id == configuracion.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la configuración de retención con Id {configuracion.Id}.");

        existente.CarreraId = configuracion.CarreraId;
        existente.EscenarioProyeccionId = configuracion.EscenarioProyeccionId;
        existente.TotalCiclos = configuracion.TotalCiclos;
        existente.TasaRetencionPorcentaje = configuracion.TasaRetencionPorcentaje;
        existente.TasaGraduacionPorcentaje = configuracion.TasaGraduacionPorcentaje;
        existente.EstudiantesPeriodo1 = configuracion.EstudiantesPeriodo1;
        existente.EstudiantesPeriodo2 = configuracion.EstudiantesPeriodo2;
        existente.ParalelosPeriodo1 = configuracion.ParalelosPeriodo1;
        existente.ParalelosPeriodo2 = configuracion.ParalelosPeriodo2;
        existente.ActualizadoEn = DateTime.UtcNow;
        existente.ActualizadoPorUsuarioId = actualizadoPorUsuarioId;
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

        dominio.RehidratarId(entidad.Id);
        return dominio;
    }
}
