using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DominioDatosInstitucionales = SistemaAranceles.Domain.Entities.DatosInstitucionales;
using InfraDatosInstitucionales = SistemaAranceles.Infrastructure.Persistence.Entidades.DatosInstitucionales;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioDatosInstitucionales(ContextoAplicacion contexto) : IRepositorioDatosInstitucionales
{
    public async Task<DominioDatosInstitucionales?> ObtenerPorPeriodoAsync(string periodo, CancellationToken cancellationToken = default)
    {
        await AsegurarParametrosInversionAsync(cancellationToken);

        var e = await contexto.DatosInstitucionales
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Periodo == periodo, cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<DominioDatosInstitucionales?> ObtenerVigenteAsync(CancellationToken cancellationToken = default)
    {
        await AsegurarParametrosInversionAsync(cancellationToken);

        var e = await contexto.DatosInstitucionales
            .AsNoTracking()
            .OrderByDescending(x => x.FechaActualizacion)
            .FirstOrDefaultAsync(cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<DominioDatosInstitucionales?> ObtenerAnteriorAsync(string periodoActual, CancellationToken cancellationToken = default)
    {
        await AsegurarParametrosInversionAsync(cancellationToken);

        var e = await contexto.DatosInstitucionales
            .AsNoTracking()
            .Where(x => x.Periodo != periodoActual)
            .OrderByDescending(x => x.FechaActualizacion)
            .FirstOrDefaultAsync(cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<IReadOnlyList<DominioDatosInstitucionales>> ListarHistoricoAsync(CancellationToken cancellationToken = default)
    {
        await AsegurarParametrosInversionAsync(cancellationToken);

        var lista = await contexto.DatosInstitucionales
            .AsNoTracking()
            .OrderByDescending(x => x.FechaActualizacion)
            .ToListAsync(cancellationToken);
        return lista.Select(MapearADominio).ToList();
    }

    private Task AsegurarParametrosInversionAsync(CancellationToken cancellationToken)
        => contexto.Database.ExecuteSqlRawAsync("""
            ALTER TABLE public.datos_institucionales
                ADD COLUMN IF NOT EXISTS meses_capital_trabajo INTEGER NOT NULL DEFAULT 2;

            ALTER TABLE public.datos_institucionales
                ADD COLUMN IF NOT EXISTS porcentaje_imprevistos_inversion NUMERIC(7,4) NOT NULL DEFAULT 5.0000;

            UPDATE public.datos_institucionales
               SET meses_capital_trabajo = 2
             WHERE meses_capital_trabajo IS NULL;

            UPDATE public.datos_institucionales
               SET porcentaje_imprevistos_inversion = 5.0000
             WHERE porcentaje_imprevistos_inversion IS NULL;

            ALTER TABLE public.datos_institucionales
                ALTER COLUMN meses_capital_trabajo SET DEFAULT 2,
                ALTER COLUMN meses_capital_trabajo SET NOT NULL,
                ALTER COLUMN porcentaje_imprevistos_inversion SET DEFAULT 5.0000,
                ALTER COLUMN porcentaje_imprevistos_inversion SET NOT NULL;
            """, cancellationToken);

    public void Agregar(DominioDatosInstitucionales datos)
        => contexto.DatosInstitucionales.Add(MapearAInfra(datos));

    public void Actualizar(DominioDatosInstitucionales datos)
    {
        var rastreado = contexto.ChangeTracker.Entries<InfraDatosInstitucionales>()
            .FirstOrDefault(e => e.Entity.Id == datos.Id);
        if (rastreado is not null)
        {
            rastreado.State = EntityState.Detached;
        }

        contexto.DatosInstitucionales.Update(MapearAInfra(datos));
    }

    private static DominioDatosInstitucionales MapearADominio(InfraDatosInstitucionales e)
    {
        var dominio = new DominioDatosInstitucionales(
            e.Periodo,
            e.NumeroEstudiantesUniversidad,
            e.NumeroDocentesUniversidad,
            e.NumeroPersonasPlantaCentral,
            e.SueldoBasico,
            e.Funcional,
            e.FondoReserva,
            e.BeneficioXiv,
            e.BeneficioXiii,
            e.AportePatronal,
            e.Varios,
            e.ActualizadoPorUsuarioId,
            e.FuenteNotas,
            e.MesesCapitalTrabajo,
            e.PorcentajeImprevistosInversion);

        dominio.RehidratarId(e.Id);
        dominio.RehidratarFechaActualizacion(e.FechaActualizacion);
        return dominio;
    }

    private static InfraDatosInstitucionales MapearAInfra(DominioDatosInstitucionales d) => new()
    {
        Id = d.Id,
        Periodo = d.Periodo,
        NumeroEstudiantesUniversidad = d.NumeroEstudiantesUniversidad,
        NumeroDocentesUniversidad = d.NumeroDocentesUniversidad,
        NumeroPersonasPlantaCentral = d.NumeroPersonasPlantaCentral,
        SueldoBasico = d.SueldoBasico,
        Funcional = d.Funcional,
        FondoReserva = d.FondoReserva,
        BeneficioXiv = d.BeneficioXiv,
        BeneficioXiii = d.BeneficioXiii,
        AportePatronal = d.AportePatronal,
        Varios = d.Varios,
        MesesCapitalTrabajo = d.MesesCapitalTrabajo,
        PorcentajeImprevistosInversion = d.PorcentajeImprevistosInversion,
        FechaActualizacion = d.FechaActualizacion,
        ActualizadoPorUsuarioId = d.ActualizadoPorUsuarioId,
        FuenteNotas = d.FuenteNotas,
    };
}
