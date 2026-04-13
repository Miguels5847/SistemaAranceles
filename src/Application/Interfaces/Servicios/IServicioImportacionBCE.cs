using SistemaAranceles.Application.DTOs.Inflacion;

namespace SistemaAranceles.Application.Interfaces.Servicios;

public interface IServicioImportacionBCE
{
    Task<ImportacionInflacionLecturaResultadoDto> ObtenerInflacionAnualAsync(BceImportSolicitudDto solicitud, CancellationToken cancellationToken = default);
}
