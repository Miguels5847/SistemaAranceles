using SistemaAranceles.Application.DTOs.Inflacion;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Application.UseCases.Inflacion;

/// <summary>
/// Entrega inflación anual para módulos consumidores en modo solo lectura.
/// No realiza ni habilita mutaciones de datos.
/// </summary>
public sealed class ObtenerInflacionProyectadaParaDependientesUseCase(IRepositorioInflacionAnual repositorioInflacionAnual)
{
    public async Task<IReadOnlyList<InflacionDependienteLecturaDto>> EjecutarAsync(
        int anioDesde,
        int anioHasta,
        CancellationToken cancellationToken = default)
    {
        if (anioDesde <= 0 || anioHasta <= 0)
            throw new ArgumentException("El rango de años debe ser mayor a cero.");

        if (anioDesde > anioHasta)
            throw new ArgumentException("El año inicial no puede ser mayor al año final.");

        var registros = await repositorioInflacionAnual.ListarPorRangoAsync(anioDesde, anioHasta, cancellationToken);

        return registros
            .OrderBy(x => x.Anio)
            .Select(x => new InflacionDependienteLecturaDto
            {
                Anio = x.Anio,
                PorcentajeInflacion = x.PorcentajeInflacion,
                EsProyectada = x.TipoFuente.Equals("estimacion", StringComparison.OrdinalIgnoreCase),
                SoloLectura = true
            })
            .ToList();
    }
}
