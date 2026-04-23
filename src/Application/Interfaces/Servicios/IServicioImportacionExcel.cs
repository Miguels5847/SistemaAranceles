using SistemaAranceles.Application.DTOs.Inflacion;

namespace SistemaAranceles.Application.Interfaces.Servicios;

public interface IServicioImportacionExcel
{
    Task<ImportacionInflacionLecturaResultadoDto> LeerExcelAsync(string rutaArchivo, CancellationToken cancellationToken = default);
    Task<ImportacionInflacionLecturaResultadoDto> LeerCsvAsync(string rutaArchivo, CancellationToken cancellationToken = default);
}
