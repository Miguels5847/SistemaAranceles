using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.TasaRetencion;
using SistemaAranceles.Application.Interfaces.Persistencia;
using System.Diagnostics;
using SimulacionDominio = SistemaAranceles.Domain.Entities.SimulacionRetencion;
using SimulacionPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.SimulacionRetencion;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioSimulacionRetencion(ContextoAplicacion contextoAplicacion) : IRepositorioSimulacionRetencion
{
    public async Task<IReadOnlyList<ResumenSimulacionRetencionDto>> ListarResumenAsync(
        int? carreraId = null,
        int? escenarioProyeccionId = null,
        int? cohorteAnio = null,
        CancellationToken cancellationToken = default)
    {
        var query = from s in contextoAplicacion.SimulacionesRetencion.AsNoTracking()
                    where s.EstaActivo
                    join c in contextoAplicacion.ConfiguracionesRetencion.AsNoTracking() on s.ConfiguracionRetencionId equals c.Id
                    join car in contextoAplicacion.Carreras.AsNoTracking() on c.CarreraId equals car.Id
                    join esc in contextoAplicacion.EscenariosProyeccion.AsNoTracking() on c.EscenarioProyeccionId equals esc.Id
                    select new { s, c, car, esc };

        if (carreraId.HasValue)
            query = query.Where(x => x.c.CarreraId == carreraId.Value);

        if (escenarioProyeccionId.HasValue)
            query = query.Where(x => x.c.EscenarioProyeccionId == escenarioProyeccionId.Value);

        if (cohorteAnio.HasValue)
            query = query.Where(x => x.s.CohorteAnio == cohorteAnio.Value);

        var lista = await query
            .OrderByDescending(x => x.s.FechaSimulacion)
            .Select(x => new ResumenSimulacionRetencionDto
            {
                Id = x.s.Id,
                ConfiguracionRetencionId = x.s.ConfiguracionRetencionId,
                CarreraId = x.c.CarreraId,
                CarreraNombre = x.car.Nombre,
                CarreraCodigo = x.car.Codigo,
                EscenarioProyeccionId = x.c.EscenarioProyeccionId,
                EscenarioNombre = x.esc.Nombre,
                TasaRetencionConfigurada = x.c.TasaRetencionPorcentaje,
                TasaGraduacionConfigurada = x.c.TasaGraduacionPorcentaje,
                CohorteAnio = x.s.CohorteAnio,
                FechaSimulacion = x.s.FechaSimulacion,
                RetencionPorcentajeFinal = x.s.RetencionPorcentajeFinal,
                GraduacionPorcentajeFinal = x.s.GraduacionPorcentajeFinal,
                EstudiantesTotalesInicio = x.s.EstudiantesTotalesInicio,
                EstudiantesRetenidos = x.s.EstudiantesRetenidos,
                EstudiantesGraduados = x.s.EstudiantesGraduados
            })
            .ToListAsync(cancellationToken);

        return lista;
    }

    public async Task<SimulacionRetencionDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var baseDto = await (from s in contextoAplicacion.SimulacionesRetencion.AsNoTracking()
                             where s.Id == id && s.EstaActivo
                             join c in contextoAplicacion.ConfiguracionesRetencion.AsNoTracking() on s.ConfiguracionRetencionId equals c.Id
                             join car in contextoAplicacion.Carreras.AsNoTracking() on c.CarreraId equals car.Id
                             join esc in contextoAplicacion.EscenariosProyeccion.AsNoTracking() on c.EscenarioProyeccionId equals esc.Id
                             select new SimulacionRetencionDto
                             {
                                 Id = s.Id,
                                 ConfiguracionRetencionId = s.ConfiguracionRetencionId,
                                 CarreraId = c.CarreraId,
                                 CarreraNombre = car.Nombre,
                                 CarreraCodigo = car.Codigo,
                                 EscenarioProyeccionId = c.EscenarioProyeccionId,
                                 EscenarioNombre = esc.Nombre,
                                 TasaRetencionConfigurada = c.TasaRetencionPorcentaje,
                                 TasaGraduacionConfigurada = c.TasaGraduacionPorcentaje,
                                 CohorteAnio = s.CohorteAnio,
                                 FechaSimulacion = s.FechaSimulacion,
                                 RetencionPorcentajeFinal = s.RetencionPorcentajeFinal,
                                 GraduacionPorcentajeFinal = s.GraduacionPorcentajeFinal,
                                 EstudiantesTotalesInicio = s.EstudiantesTotalesInicio,
                                 EstudiantesRetenidos = s.EstudiantesRetenidos,
                                 EstudiantesGraduados = s.EstudiantesGraduados
                             })
                            .FirstOrDefaultAsync(cancellationToken);

        if (baseDto is null)
            return null;

        var detalles = await contextoAplicacion.DetallesSimulacionRetencion
            .AsNoTracking()
            .Where(x => x.SimulacionRetencionId == id && x.EstaActivo)
            .OrderBy(x => x.Ciclo)
            .Select(x => new DetalleSimulacionRetencionDto
            {
                Id = x.Id,
                Ciclo = x.Ciclo,
                EstudiantesInicio = x.EstudiantesInicio,
                EstudiantesRetenidos = x.EstudiantesRetenidos,
                EstudiantesReprobados = x.EstudiantesReprobados,
                EstudiantesGraduados = x.EstudiantesGraduados,
                AnioAcademico = x.AnioAcademico,
                TasaRetencionCicloPorcentaje = x.EstudiantesInicio <= 0m ? 0m : decimal.Round((x.EstudiantesRetenidos / x.EstudiantesInicio) * 100m, 4),
                TasaGraduacionCicloPorcentaje = x.EstudiantesInicio <= 0m ? 0m : decimal.Round((x.EstudiantesGraduados / x.EstudiantesInicio) * 100m, 4)
            })
            .ToListAsync(cancellationToken);

        return new SimulacionRetencionDto
        {
            Id = baseDto.Id,
            ConfiguracionRetencionId = baseDto.ConfiguracionRetencionId,
            CarreraId = baseDto.CarreraId,
            CarreraNombre = baseDto.CarreraNombre,
            CarreraCodigo = baseDto.CarreraCodigo,
            EscenarioProyeccionId = baseDto.EscenarioProyeccionId,
            EscenarioNombre = baseDto.EscenarioNombre,
            TasaRetencionConfigurada = baseDto.TasaRetencionConfigurada,
            TasaGraduacionConfigurada = baseDto.TasaGraduacionConfigurada,
            CohorteAnio = baseDto.CohorteAnio,
            FechaSimulacion = baseDto.FechaSimulacion,
            RetencionPorcentajeFinal = baseDto.RetencionPorcentajeFinal,
            GraduacionPorcentajeFinal = baseDto.GraduacionPorcentajeFinal,
            EstudiantesTotalesInicio = baseDto.EstudiantesTotalesInicio,
            EstudiantesRetenidos = baseDto.EstudiantesRetenidos,
            EstudiantesGraduados = baseDto.EstudiantesGraduados,
            Detalles = detalles
        };
    }

    public async Task<SimulacionDominio?> ObtenerDominioPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entidad = await contextoAplicacion.SimulacionesRetencion
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, cancellationToken);

        return entidad is null ? null : MapearADominio(entidad);
    }

    public async Task<SimulacionDominio?> ObtenerDominioPorConfiguracionYCohorteAsync(int configuracionRetencionId, int cohorteAnio, CancellationToken cancellationToken = default)
    {
        var entidad = await contextoAplicacion.SimulacionesRetencion
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ConfiguracionRetencionId == configuracionRetencionId
                                   && x.CohorteAnio == cohorteAnio,
                cancellationToken);

        return entidad is null ? null : MapearADominio(entidad);
    }

    public async Task<bool> ExisteActivaPorConfiguracionAsync(int configuracionRetencionId, CancellationToken cancellationToken = default)
    {
        return await contextoAplicacion.SimulacionesRetencion
            .AnyAsync(x => x.ConfiguracionRetencionId == configuracionRetencionId && x.EstaActivo, cancellationToken);
    }

    public async Task AgregarAsync(SimulacionDominio simulacion, int? creadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(simulacion);
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] SimRetDB: agregar config={simulacion.ConfiguracionRetencionId}, cohorte={simulacion.CohorteAnio}, usuario={creadoPorUsuarioId}");

        var entidad = new SimulacionPersistencia
        {
            ConfiguracionRetencionId = simulacion.ConfiguracionRetencionId,
            EjecutadoEn = simulacion.FechaSimulacion,
            CohorteAnio = simulacion.CohorteAnio,
            FechaSimulacion = simulacion.FechaSimulacion,
            RetencionPorcentajeFinal = simulacion.RetencionPorcentajeFinal,
            GraduacionPorcentajeFinal = simulacion.GraduacionPorcentajeFinal,
            EstudiantesTotalesInicio = simulacion.EstudiantesTotalesInicio,
            EstudiantesRetenidos = simulacion.EstudiantesRetenidos,
            EstudiantesGraduados = simulacion.EstudiantesGraduados,
            CostoMatriculaPromedio = simulacion.CostoMatriculaPromedio,
            CreadoEn = DateTime.UtcNow,
            CreadoPorUsuarioId = creadoPorUsuarioId,
            EstaActivo = true
        };

        await contextoAplicacion.SimulacionesRetencion.AddAsync(entidad, cancellationToken);
    }

    public async Task ActualizarAsync(SimulacionDominio simulacion, int? actualizadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(simulacion);
        var existente = await contextoAplicacion.SimulacionesRetencion
            .FirstOrDefaultAsync(x => x.Id == simulacion.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la simulación con Id {simulacion.Id}.");

        existente.EstaActivo = true;
        existente.EliminadoEn = null;
        existente.EliminadoPorUsuarioId = null;
        existente.CohorteAnio = simulacion.CohorteAnio;
        existente.EjecutadoEn = simulacion.FechaSimulacion;
        existente.FechaSimulacion = simulacion.FechaSimulacion;
        existente.RetencionPorcentajeFinal = simulacion.RetencionPorcentajeFinal;
        existente.GraduacionPorcentajeFinal = simulacion.GraduacionPorcentajeFinal;
        existente.EstudiantesTotalesInicio = simulacion.EstudiantesTotalesInicio;
        existente.EstudiantesRetenidos = simulacion.EstudiantesRetenidos;
        existente.EstudiantesGraduados = simulacion.EstudiantesGraduados;
        existente.CostoMatriculaPromedio = simulacion.CostoMatriculaPromedio;
        existente.ActualizadoEn = DateTime.UtcNow;
        existente.ActualizadoPorUsuarioId = actualizadoPorUsuarioId;
    }

    public async Task EliminarPorIdAsync(int id, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        var existente = await contextoAplicacion.SimulacionesRetencion
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la simulación con Id {id}.");

        existente.EstaActivo = false;
        existente.EliminadoEn = DateTime.UtcNow;
        existente.EliminadoPorUsuarioId = eliminadoPorUsuarioId;
    }

    public async Task<int> LimpiarPorConfiguracionAsync(int configuracionRetencionId, int? eliminadoPorUsuarioId = null, CancellationToken cancellationToken = default)
    {
        var simulaciones = await contextoAplicacion.SimulacionesRetencion
            .Where(x => x.ConfiguracionRetencionId == configuracionRetencionId && x.EstaActivo)
            .ToListAsync(cancellationToken);

        foreach (var simulacion in simulaciones)
        {
            simulacion.EstaActivo = false;
            simulacion.EliminadoEn = DateTime.UtcNow;
            simulacion.EliminadoPorUsuarioId = eliminadoPorUsuarioId;
        }

        return simulaciones.Count;
    }

    private static SimulacionDominio MapearADominio(SimulacionPersistencia entidad)
    {
        var dominio = new SimulacionDominio(entidad.ConfiguracionRetencionId, entidad.CohorteAnio);
        dominio.EstablecerFechaSimulacion(entidad.FechaSimulacion);
        dominio.ActualizarIndicadoresFinales(
            entidad.EstudiantesTotalesInicio,
            entidad.EstudiantesRetenidos,
            entidad.EstudiantesGraduados,
            entidad.CostoMatriculaPromedio);
        dominio.RehidratarId(entidad.Id);
        return dominio;
    }
}
