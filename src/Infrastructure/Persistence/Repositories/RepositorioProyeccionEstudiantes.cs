using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Application.Interfaces.Persistencia;
using ProyeccionPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.ProyeccionEstudiantes;
using DetallePersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.DetalleProyeccionEstudiantes;
using PeriodoPersistencia = SistemaAranceles.Infrastructure.Persistence.Entidades.PeriodoAcademico;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioProyeccionEstudiantes(ContextoAplicacion contexto) : IRepositorioProyeccionEstudiantes
{
    public async Task<IReadOnlyList<ResumenProyeccionEstudiantesDto>> ListarResumenAsync(
        int? carreraId = null,
        int? escenarioProyeccionId = null,
        CancellationToken cancellationToken = default)
    {
        var query = from p in contexto.ProyeccionesEstudiantes.AsNoTracking()
                    where p.EstaActivo
                    join car in contexto.Carreras.AsNoTracking() on p.CarreraId equals car.Id
                    join esc in contexto.EscenariosProyeccion.AsNoTracking() on p.EscenarioProyeccionId equals esc.Id
                    select new { p, car, esc };

        if (carreraId.HasValue)
            query = query.Where(x => x.p.CarreraId == carreraId.Value);

        if (escenarioProyeccionId.HasValue)
            query = query.Where(x => x.p.EscenarioProyeccionId == escenarioProyeccionId.Value);

        return await query
            .OrderByDescending(x => x.p.CreadoEn)
            .Select(x => new ResumenProyeccionEstudiantesDto
            {
                Id = x.p.Id,
                CarreraId = x.p.CarreraId,
                CarreraNombre = x.car.Nombre,
                CarreraCodigo = x.car.Codigo,
                EscenarioProyeccionId = x.p.EscenarioProyeccionId,
                EscenarioNombre = x.esc.Nombre,
                AnioBase = x.p.AnioBase,
                SemanasPorSemestre = x.p.SemanasPorSemestre,
                CreadoEn = x.p.CreadoEn,
                ActualizadoEn = x.p.ActualizadoEn
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ProyeccionEstudiantesDto?> ObtenerDtoPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var cabecera = await (from p in contexto.ProyeccionesEstudiantes.AsNoTracking()
                              where p.Id == id && p.EstaActivo
                              join car in contexto.Carreras.AsNoTracking() on p.CarreraId equals car.Id
                              join esc in contexto.EscenariosProyeccion.AsNoTracking() on p.EscenarioProyeccionId equals esc.Id
                              select new ProyeccionEstudiantesDto
                              {
                                  Id = p.Id,
                                  CarreraId = p.CarreraId,
                                  CarreraNombre = car.Nombre,
                                  CarreraCodigo = car.Codigo,
                                  EscenarioProyeccionId = p.EscenarioProyeccionId,
                                  EscenarioNombre = esc.Nombre,
                                  AnioBase = p.AnioBase,
                                  SemanasPorSemestre = p.SemanasPorSemestre,
                                  CreadoEn = p.CreadoEn,
                                  ActualizadoEn = p.ActualizadoEn
                              })
                             .FirstOrDefaultAsync(cancellationToken);

        if (cabecera is null)
            return null;

        var detalles = await (from d in contexto.DetallesProyeccionEstudiantes.AsNoTracking()
                              where d.ProyeccionEstudiantesId == id && d.EstaActivo
                              join per in contexto.PeriodosAcademicos.AsNoTracking() on d.PeriodoAcademicoId equals per.Id
                              orderby per.Anio, per.NumeroPeriodo, d.NumeroCiclo
                              select new DetalleProyeccionEstudiantesDto
                              {
                                  Id = d.Id,
                                  PeriodoAcademicoId = d.PeriodoAcademicoId,
                                  Anio = per.Anio,
                                  NumeroPeriodo = per.NumeroPeriodo,
                                  EtiquetaPeriodo = per.EtiquetaPeriodo,
                                  NumeroCiclo = d.NumeroCiclo,
                                  CantidadParalelos = d.CantidadParalelos,
                                  TotalEstudiantes = d.TotalEstudiantes
                              })
                             .ToListAsync(cancellationToken);

        return new ProyeccionEstudiantesDto
        {
            Id = cabecera.Id,
            CarreraId = cabecera.CarreraId,
            CarreraNombre = cabecera.CarreraNombre,
            CarreraCodigo = cabecera.CarreraCodigo,
            EscenarioProyeccionId = cabecera.EscenarioProyeccionId,
            EscenarioNombre = cabecera.EscenarioNombre,
            AnioBase = cabecera.AnioBase,
            SemanasPorSemestre = cabecera.SemanasPorSemestre,
            CreadoEn = cabecera.CreadoEn,
            ActualizadoEn = cabecera.ActualizadoEn,
            Detalles = detalles
        };
    }

    public async Task<int?> ObtenerIdPorCarreraYEscenarioAsync(
        int carreraId,
        int escenarioProyeccionId,
        CancellationToken cancellationToken = default)
    {
        var id = await contexto.ProyeccionesEstudiantes
            .AsNoTracking()
            .Where(x => x.CarreraId == carreraId && x.EscenarioProyeccionId == escenarioProyeccionId && x.EstaActivo)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return id;
    }

    public async Task<int> GuardarAsync(
        int carreraId,
        int escenarioProyeccionId,
        int anioBase,
        int semanasPorSemestre,
        IReadOnlyList<CeldaProyeccionEstudiantesDto> celdas,
        int? usuarioId,
        CancellationToken cancellationToken = default)
    {
        var periodoIds = await ResolverPeriodosAcademicosAsync(anioBase, celdas, usuarioId, cancellationToken);

        var ahora = DateTime.UtcNow;

        var existente = await contexto.ProyeccionesEstudiantes
            .FirstOrDefaultAsync(x => x.CarreraId == carreraId && x.EscenarioProyeccionId == escenarioProyeccionId, cancellationToken);

        int proyeccionId;

        if (existente is not null)
        {
            existente.AnioBase = anioBase;
            existente.SemanasPorSemestre = semanasPorSemestre;
            existente.ActualizadoEn = ahora;
            existente.ActualizadoPorUsuarioId = usuarioId;
            existente.EstaActivo = true;
            existente.EliminadoEn = null;
            existente.EliminadoPorUsuarioId = null;
            proyeccionId = existente.Id;

            await contexto.DetallesProyeccionEstudiantes
                .Where(d => d.ProyeccionEstudiantesId == proyeccionId)
                .ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            var nueva = new ProyeccionPersistencia
            {
                CarreraId = carreraId,
                EscenarioProyeccionId = escenarioProyeccionId,
                AnioBase = anioBase,
                SemanasPorSemestre = semanasPorSemestre,
                CreadoEn = ahora,
                CreadoPorUsuarioId = usuarioId,
                EstaActivo = true
            };
            await contexto.ProyeccionesEstudiantes.AddAsync(nueva, cancellationToken);
            await contexto.SaveChangesAsync(cancellationToken);
            proyeccionId = nueva.Id;
        }

        var detalles = celdas.Select(celda => new DetallePersistencia
        {
            ProyeccionEstudiantesId = proyeccionId,
            PeriodoAcademicoId = periodoIds[celda.NumeroPeriodo],
            NumeroCiclo = celda.NumeroCiclo,
            CantidadParalelos = celda.CantidadParalelos,
            TotalEstudiantes = celda.TotalEstudiantes,
            CreadoEn = ahora,
            CreadoPorUsuarioId = usuarioId,
            EstaActivo = true
        });

        await contexto.DetallesProyeccionEstudiantes.AddRangeAsync(detalles, cancellationToken);

        return proyeccionId;
    }

    public async Task EliminarPorIdAsync(int id, int? usuarioId, CancellationToken cancellationToken = default)
    {
        var existente = await contexto.ProyeccionesEstudiantes
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, cancellationToken)
            ?? throw new KeyNotFoundException($"No se encontró la proyección con Id {id}.");

        existente.EstaActivo = false;
        existente.EliminadoEn = DateTime.UtcNow;
        existente.EliminadoPorUsuarioId = usuarioId;
    }

    private async Task<Dictionary<int, int>> ResolverPeriodosAcademicosAsync(
        int anioBase,
        IReadOnlyList<CeldaProyeccionEstudiantesDto> celdas,
        int? usuarioId,
        CancellationToken cancellationToken)
    {
        var totalPeriodos = celdas.Select(c => c.NumeroPeriodo).Max();
        var necesarios = new List<(int NumPeriodo, int Anio, int NumeroPeriodoBD, string Etiqueta)>();

        for (var p = 1; p <= totalPeriodos; p++)
        {
            var anio = anioBase + (p - 1) / 2;
            var numPer = ((p - 1) % 2) + 1;
            var etiqueta = numPer == 1 ? $"Abr {anio}" : $"Sep {anio}";
            necesarios.Add((p, anio, numPer, etiqueta));
        }

        var anios = necesarios.Select(x => x.Anio).Distinct().ToList();
        var existentes = await contexto.PeriodosAcademicos
            .Where(per => anios.Contains(per.Anio))
            .ToListAsync(cancellationToken);

        var resultado = new Dictionary<int, int>();
        var ahora = DateTime.UtcNow;

        foreach (var (numPeriodo, anio, numPer, etiqueta) in necesarios)
        {
            var existente = existentes.FirstOrDefault(e => e.Anio == anio && e.NumeroPeriodo == numPer);
            if (existente is not null)
            {
                resultado[numPeriodo] = existente.Id;
            }
            else
            {
                var nuevo = new PeriodoPersistencia
                {
                    Anio = anio,
                    NumeroPeriodo = numPer,
                    EtiquetaPeriodo = etiqueta,
                    CreadoEn = ahora,
                    CreadoPorUsuarioId = usuarioId,
                    EstaActivo = true
                };
                await contexto.PeriodosAcademicos.AddAsync(nuevo, cancellationToken);
                await contexto.SaveChangesAsync(cancellationToken);
                resultado[numPeriodo] = nuevo.Id;
                existentes.Add(nuevo);
            }
        }

        return resultado;
    }
}
