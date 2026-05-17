using SistemaAranceles.Application.DTOs.SueldosPlantaCentral;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.SueldosPlantaCentral;

public sealed class ObtenerDatosInstitucionalesVigentesQuery(
    IRepositorioDatosInstitucionales repositorio,
    IRepositorioUsuario repositorioUsuario)
{
    public async Task<DatosInstitucionalesDto?> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var datos = await repositorio.ObtenerVigenteAsync(cancellationToken);
        if (datos is null)
        {
            return null;
        }

        var usuario = await repositorioUsuario.ObtenerPorIdAsync(datos.ActualizadoPorUsuarioId, cancellationToken);
        return MapeoDatosInstitucionales.AMapeo(datos, usuario?.NombreCompleto);
    }
}

public sealed class ListarHistoricoDatosInstitucionalesQuery(IRepositorioDatosInstitucionales repositorio)
{
    public async Task<IReadOnlyList<DatosInstitucionalesDto>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        var lista = await repositorio.ListarHistoricoAsync(cancellationToken);
        return lista.Select(d => MapeoDatosInstitucionales.AMapeo(d, null)).ToList();
    }
}
