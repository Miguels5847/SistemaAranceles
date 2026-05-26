using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Application.Interfaces.Persistencia;
using DatosInstitucionalesDominio = SistemaAranceles.Domain.Entities.DatosInstitucionales;

namespace SistemaAranceles.Application.UseCases.DemandaIngresos;

public sealed class ListarConfiguracionesArancelCarreraQuery(
    IRepositorioConfiguracionArancelCarrera repositorio,
    IRepositorioDatosInstitucionales repositorioDatos)
{
    public async Task<IReadOnlyList<ConfiguracionArancelCarreraDto>> EjecutarAsync(
        int? carreraId = null,
        CancellationToken ct = default)
    {
        var lista = await repositorio.ListarAsync(carreraId, ct);
        var datos = await repositorioDatos.ObtenerVigenteAsync(ct);
        var porcentajeInstitucional = datos?.PorcentajeMatriculaDefault
            ?? DatosInstitucionalesDominio.PorcentajeMatriculaDefaultPorDefecto;

        foreach (var dto in lista)
            dto.PorcentajeMatriculaInstitucional = porcentajeInstitucional;

        return lista;
    }
}
