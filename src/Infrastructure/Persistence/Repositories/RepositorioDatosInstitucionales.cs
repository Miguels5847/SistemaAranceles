using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DominioDatosInstitucionales = SistemaAranceles.Domain.Entities.DatosInstitucionales;
using InfraDatosInstitucionales = SistemaAranceles.Infrastructure.Persistence.Entidades.DatosInstitucionales;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioDatosInstitucionales(ContextoAplicacion contexto) : IRepositorioDatosInstitucionales
{
    public async Task<DominioDatosInstitucionales?> ObtenerPorPeriodoAsync(string periodo, CancellationToken cancellationToken = default)
    {
        var e = await contexto.DatosInstitucionales
            .FirstOrDefaultAsync(x => x.Periodo == periodo, cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<DominioDatosInstitucionales?> ObtenerVigenteAsync(CancellationToken cancellationToken = default)
    {
        var e = await contexto.DatosInstitucionales
            .OrderByDescending(x => x.FechaActualizacion)
            .FirstOrDefaultAsync(cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<DominioDatosInstitucionales?> ObtenerAnteriorAsync(string periodoActual, CancellationToken cancellationToken = default)
    {
        var e = await contexto.DatosInstitucionales
            .Where(x => x.Periodo != periodoActual)
            .OrderByDescending(x => x.FechaActualizacion)
            .FirstOrDefaultAsync(cancellationToken);
        return e is null ? null : MapearADominio(e);
    }

    public async Task<IReadOnlyList<DominioDatosInstitucionales>> ListarHistoricoAsync(CancellationToken cancellationToken = default)
    {
        var lista = await contexto.DatosInstitucionales
            .OrderByDescending(x => x.FechaActualizacion)
            .ToListAsync(cancellationToken);
        return lista.Select(MapearADominio).ToList();
    }

    public void Agregar(DominioDatosInstitucionales datos)
        => contexto.DatosInstitucionales.Add(MapearAInfra(datos));

    public void Actualizar(DominioDatosInstitucionales datos)
        => contexto.DatosInstitucionales.Update(MapearAInfra(datos));

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
            e.FuenteNotas);

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
        FechaActualizacion = d.FechaActualizacion,
        ActualizadoPorUsuarioId = d.ActualizadoPorUsuarioId,
        FuenteNotas = d.FuenteNotas,
    };
}
