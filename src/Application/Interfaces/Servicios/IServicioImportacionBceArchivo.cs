using SistemaAranceles.Application.DTOs.Inflacion;

namespace SistemaAranceles.Application.Interfaces.Servicios;

public interface IServicioImportacionBceArchivo
{
    Task<ImportacionInflacionLecturaResultadoDto> LeerArchivoAsync(
        ImportacionBceArchivoSolicitudDto solicitud,
        CancellationToken cancellationToken = default);
}