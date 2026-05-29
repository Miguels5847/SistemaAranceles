using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Domain.Enums;
using InfraRatio = SistemaAranceles.Infrastructure.Persistence.Entidades.RatioMaterialDemanda;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioRatioMaterialDemanda(ContextoAplicacion contexto)
    : IRepositorioRatioMaterialDemanda
{
    public async Task<IReadOnlyList<RatioMaterialDemandaDto>> ListarPorCarreraAsync(
        int carreraId,
        bool incluirGlobales = true,
        CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        var consulta = from r in contexto.RatiosMaterialDemanda.AsNoTracking()
                       join c in contexto.Carreras.AsNoTracking() on r.CarreraId equals c.Id into cj
                       from c in cj.DefaultIfEmpty()
                       join it in contexto.ItemsMaterialInsumo.AsNoTracking() on r.ItemMaterialInsumoId equals it.Id into ij
                       from it in ij.DefaultIfEmpty()
                       where r.EstaActivo
                          && (r.CarreraId == carreraId || (incluirGlobales && r.CarreraId == null))
                       select new RatioMaterialDemandaDto
                       {
                           Id = r.Id,
                           CarreraId = r.CarreraId,
                           CarreraNombre = c != null ? c.Nombre : "Global",
                           Categoria = r.Categoria,
                           Concepto = r.Concepto,
                           ItemMaterialInsumoId = r.ItemMaterialInsumoId,
                           ItemMaterialInsumoNombre = it != null ? it.NombreItem : null,
                           ItemMaterialInsumoCategoria = it != null ? it.CategoriaNombre : null,
                           PrecioUnitarioReferencia = it != null ? it.PrecioUnitario : 0m,
                           RatioConsumo = r.RatioConsumo,
                           UnidadRatio = r.UnidadRatio,
                           MesesOperativos = r.MesesOperativos,
                           CantidadFijaAdicional = r.CantidadFijaAdicional,
                           AplicaInflacion = r.AplicaInflacion,
                           EstaActivo = r.EstaActivo
                       };

        return await consulta
            .OrderBy(x => x.Categoria)
            .ThenBy(x => x.Concepto)
            .ToListAsync(ct);
    }

    public async Task<int> GuardarAsync(
        GuardarRatioMaterialDemandaDto dto,
        int? usuarioId,
        CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        if (dto.Id is int id && id > 0)
        {
            var existente = await contexto.RatiosMaterialDemanda
                .FirstOrDefaultAsync(x => x.Id == id, ct);
            if (existente is null)
                throw new InvalidOperationException($"No existe ratio id={id}.");

            existente.CarreraId = dto.CarreraId is > 0 ? dto.CarreraId : null;
            existente.Categoria = dto.Categoria;
            existente.Concepto = dto.Concepto;
            existente.ItemMaterialInsumoId = dto.ItemMaterialInsumoId is > 0 ? dto.ItemMaterialInsumoId : null;
            existente.RatioConsumo = dto.RatioConsumo;
            existente.UnidadRatio = dto.UnidadRatio;
            existente.MesesOperativos = NormalizarMesesOperativos(dto);
            existente.CantidadFijaAdicional = NormalizarCantidadFijaAdicional(dto);
            existente.AplicaInflacion = dto.AplicaInflacion;
            existente.ActualizadoEn = DateTime.UtcNow;
            existente.ActualizadoPorUsuarioId = usuarioId;
            existente.EstaActivo = true;
        }
        else
        {
            var nuevo = new InfraRatio
            {
                CarreraId = dto.CarreraId is > 0 ? dto.CarreraId : null,
                Categoria = dto.Categoria.Trim(),
                Concepto = dto.Concepto.Trim(),
                ItemMaterialInsumoId = dto.ItemMaterialInsumoId is > 0 ? dto.ItemMaterialInsumoId : null,
                RatioConsumo = dto.RatioConsumo,
                UnidadRatio = dto.UnidadRatio,
                MesesOperativos = NormalizarMesesOperativos(dto),
                CantidadFijaAdicional = NormalizarCantidadFijaAdicional(dto),
                AplicaInflacion = dto.AplicaInflacion,
                CreadoEn = DateTime.UtcNow,
                CreadoPorUsuarioId = usuarioId,
                EstaActivo = true
            };
            await contexto.RatiosMaterialDemanda.AddAsync(nuevo, ct);
            await contexto.SaveChangesAsync(ct);
            return nuevo.Id;
        }

        await contexto.SaveChangesAsync(ct);
        return dto.Id!.Value;
    }

    private static int NormalizarMesesOperativos(GuardarRatioMaterialDemandaDto dto)
        => dto.UnidadRatio is UnidadRatioMaterialExtensiones.FijoPeriodoText
            or UnidadRatioMaterialExtensiones.PorDocenteText
            ? 1
            : dto.MesesOperativos;

    private static decimal NormalizarCantidadFijaAdicional(GuardarRatioMaterialDemandaDto dto)
        => dto.UnidadRatio == UnidadRatioMaterialExtensiones.PorDocenteText
            ? dto.CantidadFijaAdicional
            : 0m;

    public async Task EliminarAsync(int id, int? usuarioId, CancellationToken ct = default)
    {
        await AsegurarTablaAsync(ct);

        var existente = await contexto.RatiosMaterialDemanda
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (existente is null) return;

        existente.EstaActivo = false;
        existente.EliminadoEn = DateTime.UtcNow;
        existente.EliminadoPorUsuarioId = usuarioId;
        await contexto.SaveChangesAsync(ct);
    }

    private Task AsegurarTablaAsync(CancellationToken ct)
        => contexto.Database.ExecuteSqlRawAsync("""
            CREATE TABLE IF NOT EXISTS public.ratio_material_demanda (
                id                         SERIAL PRIMARY KEY,
                carrera_id                 INTEGER NULL REFERENCES public.carrera(id) ON DELETE CASCADE,
                categoria                  VARCHAR(60)  NOT NULL,
                concepto                   VARCHAR(140) NOT NULL,
                item_material_insumo_id    INTEGER NULL REFERENCES public.item_material_insumo(id) ON DELETE SET NULL,
                ratio_consumo              NUMERIC(12,6) NOT NULL DEFAULT 0,
                unidad_ratio               VARCHAR(40)  NOT NULL DEFAULT 'por_estudiante',
                meses_operativos           INTEGER      NOT NULL DEFAULT 6,
                cantidad_fija_adicional    NUMERIC(18,4) NOT NULL DEFAULT 0,
                aplica_inflacion           BOOLEAN      NOT NULL DEFAULT TRUE,
                creado_en                  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
                creado_por_usuario_id      INTEGER NULL,
                actualizado_en             TIMESTAMPTZ NULL,
                actualizado_por_usuario_id INTEGER NULL,
                esta_activo                BOOLEAN      NOT NULL DEFAULT TRUE,
                eliminado_en               TIMESTAMPTZ NULL,
                eliminado_por_usuario_id   INTEGER NULL
            );
            """, ct);
}
