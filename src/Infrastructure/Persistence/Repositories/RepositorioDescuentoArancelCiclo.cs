using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using InfraDescuento = SistemaAranceles.Infrastructure.Persistence.Entidades.DescuentoArancelCiclo;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioDescuentoArancelCiclo(ContextoAplicacion contexto)
    : IRepositorioDescuentoArancelCiclo
{
    public async Task<IReadOnlyList<DescuentoArancelCicloDto>> ListarPorCarreraEscenarioAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);
        return await ConsultaActivos(carreraId, escenarioProyeccionId).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<DescuentoArancelCicloDto>> ListarEfectivosPorCarreraEscenarioAsync(
        int carreraId,
        int? escenarioProyeccionId,
        CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        if (escenarioProyeccionId is > 0)
        {
            var especificos = await ConsultaActivos(carreraId, escenarioProyeccionId).ToListAsync(ct);
            if (especificos.Count > 0)
                return especificos;
        }

        // Fallback a configuración global de la carrera (escenario nulo).
        return await ConsultaActivos(carreraId, null).ToListAsync(ct);
    }

    private IQueryable<DescuentoArancelCicloDto> ConsultaActivos(int carreraId, int? escenarioProyeccionId)
        => from d in contexto.DescuentosArancelCiclo.AsNoTracking()
           join car in contexto.Carreras.AsNoTracking() on d.CarreraId equals car.Id
           join es in contexto.EscenariosProyeccion.AsNoTracking()
                on d.EscenarioProyeccionId equals es.Id into esJoin
           from es in esJoin.DefaultIfEmpty()
           where d.EstaActivo
              && d.CarreraId == carreraId
              && d.EscenarioProyeccionId == escenarioProyeccionId
           orderby d.CicloDesde, d.CicloHasta
           select new DescuentoArancelCicloDto
           {
               Id = d.Id,
               CarreraId = d.CarreraId,
               CarreraNombre = car.Nombre,
               EscenarioProyeccionId = d.EscenarioProyeccionId,
               EscenarioNombre = es != null ? es.Nombre : "Global",
               CicloDesde = d.CicloDesde,
               CicloHasta = d.CicloHasta,
               PorcentajeDescuento = d.PorcentajeDescuento,
               EstaActivo = d.EstaActivo
           };

    public async Task<int> GuardarAsync(
        GuardarDescuentoArancelCicloDto dto,
        int? usuarioId,
        CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        if (dto.Id is > 0)
        {
            var existente = await contexto.DescuentosArancelCiclo
                .FirstOrDefaultAsync(x => x.Id == dto.Id.Value, ct);
            if (existente is not null)
            {
                existente.CicloDesde = dto.CicloDesde;
                existente.CicloHasta = dto.CicloHasta;
                existente.PorcentajeDescuento = dto.PorcentajeDescuento;
                existente.EstaActivo = true;
                existente.ActualizadoEn = DateTime.UtcNow;
                existente.ActualizadoPorUsuarioId = usuarioId;
                await contexto.SaveChangesAsync(ct);
                return existente.Id;
            }
        }

        var nuevo = new InfraDescuento
        {
            CarreraId = dto.CarreraId,
            EscenarioProyeccionId = dto.EscenarioProyeccionId,
            CicloDesde = dto.CicloDesde,
            CicloHasta = dto.CicloHasta,
            PorcentajeDescuento = dto.PorcentajeDescuento,
            CreadoEn = DateTime.UtcNow,
            CreadoPorUsuarioId = usuarioId,
            EstaActivo = true
        };
        await contexto.DescuentosArancelCiclo.AddAsync(nuevo, ct);
        await contexto.SaveChangesAsync(ct);
        return nuevo.Id;
    }

    public async Task DesactivarAsync(int id, int? usuarioId, CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        var existente = await contexto.DescuentosArancelCiclo.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existente is null)
            return;

        existente.EstaActivo = false;
        existente.EliminadoEn = DateTime.UtcNow;
        existente.EliminadoPorUsuarioId = usuarioId;
        await contexto.SaveChangesAsync(ct);
    }

    private Task AsegurarTablaAsync(CancellationToken ct)
        => contexto.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS public.descuento_arancel_ciclo (
                id                          SERIAL PRIMARY KEY,
                carrera_id                  INTEGER NOT NULL REFERENCES public.carrera(id) ON DELETE CASCADE,
                escenario_proyeccion_id     INTEGER NULL REFERENCES public.escenario_proyeccion(id) ON DELETE SET NULL,
                ciclo_desde                 INTEGER NOT NULL,
                ciclo_hasta                 INTEGER NOT NULL,
                porcentaje_descuento        NUMERIC(6,2) NOT NULL DEFAULT 0,
                creado_en                   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                creado_por_usuario_id       INTEGER NULL,
                actualizado_en              TIMESTAMPTZ NULL,
                actualizado_por_usuario_id  INTEGER NULL,
                esta_activo                 BOOLEAN NOT NULL DEFAULT TRUE,
                eliminado_en                TIMESTAMPTZ NULL,
                eliminado_por_usuario_id    INTEGER NULL,
                CONSTRAINT "CK_descuento_arancel_ciclo_rango" CHECK (ciclo_desde > 0 AND ciclo_hasta >= ciclo_desde),
                CONSTRAINT "CK_descuento_arancel_ciclo_porc" CHECK (porcentaje_descuento >= 0 AND porcentaje_descuento <= 100)
            );
            """, ct);
}
