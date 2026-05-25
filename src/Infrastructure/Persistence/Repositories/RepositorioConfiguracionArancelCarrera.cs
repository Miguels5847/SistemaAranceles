using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DominioConfiguracion = SistemaAranceles.Domain.Entities.ConfiguracionArancelCarrera;
using InfraConfiguracion = SistemaAranceles.Infrastructure.Persistence.Entidades.ConfiguracionArancelCarrera;
using DominioModoArancel = SistemaAranceles.Domain.Enums.ModoCalculoArancel;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioConfiguracionArancelCarrera(ContextoAplicacion contexto)
    : IRepositorioConfiguracionArancelCarrera
{
    public async Task<IReadOnlyList<ConfiguracionArancelCarreraDto>> ListarAsync(
        int? carreraId = null,
        CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        var consulta = from c in contexto.ConfiguracionesArancelCarrera.AsNoTracking()
                       join car in contexto.Carreras.AsNoTracking() on c.CarreraId equals car.Id
                       join es in contexto.EscenariosProyeccion.AsNoTracking()
                            on c.EscenarioProyeccionId equals es.Id into esJoin
                       from es in esJoin.DefaultIfEmpty()
                       where c.EstaActivo
                       select new ConfiguracionArancelCarreraDto
                       {
                           Id = c.Id,
                           CarreraId = c.CarreraId,
                           CarreraNombre = car.Nombre,
                           CarreraCodigo = car.Codigo,
                           EscenarioProyeccionId = c.EscenarioProyeccionId,
                           EscenarioNombre = es != null ? es.Nombre : "Global",
                           ModoCalculoArancel = c.ModoCalculoArancel,
                           ArancelManual = c.ArancelManual,
                           PorcentajeMatricula = c.PorcentajeMatricula,
                           UsaPorcentajeMatriculaInstitucional = c.UsaPorcentajeMatriculaInstitucional,
                           EstaActivo = c.EstaActivo,
                           CreadoEn = c.CreadoEn,
                           ActualizadoEn = c.ActualizadoEn
                       };

        if (carreraId is > 0)
            consulta = consulta.Where(x => x.CarreraId == carreraId.Value);

        return await consulta
            .OrderBy(x => x.CarreraNombre)
            .ThenBy(x => x.EscenarioNombre)
            .ToListAsync(ct);
    }

    public async Task<ConfiguracionArancelCarreraDto?> ObtenerPorCarreraEscenarioAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        var consulta = from c in contexto.ConfiguracionesArancelCarrera.AsNoTracking()
                       join car in contexto.Carreras.AsNoTracking() on c.CarreraId equals car.Id
                       join es in contexto.EscenariosProyeccion.AsNoTracking()
                            on c.EscenarioProyeccionId equals es.Id into esJoin
                       from es in esJoin.DefaultIfEmpty()
                       where c.EstaActivo
                          && c.CarreraId == carreraId
                          && c.EscenarioProyeccionId == escenarioProyeccionId
                       select new ConfiguracionArancelCarreraDto
                       {
                           Id = c.Id,
                           CarreraId = c.CarreraId,
                           CarreraNombre = car.Nombre,
                           CarreraCodigo = car.Codigo,
                           EscenarioProyeccionId = c.EscenarioProyeccionId,
                           EscenarioNombre = es != null ? es.Nombre : "Global",
                           ModoCalculoArancel = c.ModoCalculoArancel,
                           ArancelManual = c.ArancelManual,
                           PorcentajeMatricula = c.PorcentajeMatricula,
                           UsaPorcentajeMatriculaInstitucional = c.UsaPorcentajeMatriculaInstitucional,
                           EstaActivo = c.EstaActivo,
                           CreadoEn = c.CreadoEn,
                           ActualizadoEn = c.ActualizadoEn
                       };

        var resultado = await consulta.FirstOrDefaultAsync(ct);
        if (resultado is not null)
            return resultado;

        if (escenarioProyeccionId is not null)
        {
            return await ObtenerPorCarreraEscenarioAsync(carreraId, null, ct);
        }
        return null;
    }

    public async Task<DominioConfiguracion?> ObtenerDominioAsync(int id, CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        var entidad = await contexto.ConfiguracionesArancelCarrera
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.EstaActivo, ct);

        return entidad is null ? null : MapearADominio(entidad);
    }

    public async Task<int> GuardarAsync(
        GuardarConfiguracionArancelCarreraDto dto,
        int? usuarioId,
        CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        if (dto.Id is int id && id > 0)
        {
            var existente = await contexto.ConfiguracionesArancelCarrera
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existente is null)
                throw new InvalidOperationException($"No existe configuración arancel id={id}.");

            existente.EscenarioProyeccionId = dto.EscenarioProyeccionId;
            existente.ModoCalculoArancel = dto.ModoCalculoArancel;
            existente.ArancelManual = dto.ArancelManual;
            existente.PorcentajeMatricula = dto.UsaPorcentajeMatriculaInstitucional ? null : dto.PorcentajeMatricula;
            existente.UsaPorcentajeMatriculaInstitucional = dto.UsaPorcentajeMatriculaInstitucional;
            existente.ActualizadoEn = DateTime.UtcNow;
            existente.ActualizadoPorUsuarioId = usuarioId;
            existente.EstaActivo = true;
        }
        else
        {
            var duplicada = await contexto.ConfiguracionesArancelCarrera
                .AnyAsync(x => x.EstaActivo
                            && x.CarreraId == dto.CarreraId
                            && x.EscenarioProyeccionId == dto.EscenarioProyeccionId, ct);
            if (duplicada)
                throw new InvalidOperationException("Ya existe configuración para esa carrera/escenario.");

            var nueva = new InfraConfiguracion
            {
                CarreraId = dto.CarreraId,
                EscenarioProyeccionId = dto.EscenarioProyeccionId,
                ModoCalculoArancel = dto.ModoCalculoArancel,
                ArancelManual = dto.ArancelManual,
                PorcentajeMatricula = dto.UsaPorcentajeMatriculaInstitucional ? null : dto.PorcentajeMatricula,
                UsaPorcentajeMatriculaInstitucional = dto.UsaPorcentajeMatriculaInstitucional,
                CreadoEn = DateTime.UtcNow,
                CreadoPorUsuarioId = usuarioId,
                EstaActivo = true
            };
            await contexto.ConfiguracionesArancelCarrera.AddAsync(nueva, ct);
            await contexto.SaveChangesAsync(ct);
            return nueva.Id;
        }

        await contexto.SaveChangesAsync(ct);
        return dto.Id!.Value;
    }

    public async Task EliminarAsync(int id, int? usuarioId, CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        var existente = await contexto.ConfiguracionesArancelCarrera
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existente is null)
            return;

        existente.EstaActivo = false;
        existente.EliminadoEn = DateTime.UtcNow;
        existente.EliminadoPorUsuarioId = usuarioId;
        await contexto.SaveChangesAsync(ct);
    }

    private Task AsegurarTablaAsync(CancellationToken ct)
        => contexto.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS public.configuracion_arancel_carrera (
                id                                       SERIAL PRIMARY KEY,
                carrera_id                               INTEGER NOT NULL REFERENCES public.carrera(id) ON DELETE CASCADE,
                escenario_proyeccion_id                  INTEGER NULL REFERENCES public.escenario_proyeccion(id) ON DELETE SET NULL,
                modo_calculo_arancel                     VARCHAR(30) NOT NULL DEFAULT 'Manual',
                arancel_manual                           NUMERIC(18,2) NULL,
                porcentaje_matricula                     NUMERIC(7,4) NULL,
                usa_porcentaje_matricula_institucional   BOOLEAN NOT NULL DEFAULT TRUE,
                creado_en                                TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                creado_por_usuario_id                    INTEGER NULL,
                actualizado_en                           TIMESTAMPTZ NULL,
                actualizado_por_usuario_id               INTEGER NULL,
                esta_activo                              BOOLEAN NOT NULL DEFAULT TRUE,
                eliminado_en                             TIMESTAMPTZ NULL,
                eliminado_por_usuario_id                 INTEGER NULL
            );
            """, ct);

    private static DominioConfiguracion MapearADominio(InfraConfiguracion e)
    {
        var modo = Enum.TryParse<DominioModoArancel>(e.ModoCalculoArancel, ignoreCase: true, out var m)
            ? m
            : DominioModoArancel.Manual;

        var dominio = new DominioConfiguracion(
            e.CarreraId,
            e.EscenarioProyeccionId,
            modo,
            e.ArancelManual,
            e.PorcentajeMatricula,
            e.UsaPorcentajeMatriculaInstitucional);

        dominio.RehidratarId(e.Id);
        if (!e.EstaActivo)
            dominio.Desactivar();

        return dominio;
    }
}
