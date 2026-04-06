using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class RepositorioAuditoriaLog(ContextoAplicacion contextoAplicacion) : IRepositorioAuditoriaLog
{
    public async Task AgregarAsync(
        string moduloNombre,
        string entidadNombre,
        string entidadId,
        string accionNombre,
        string resumenTexto,
        int? ejecutadoPorUsuarioId = null,
        string? valoresAnterioresJson = null,
        string? valoresNuevosJson = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditoriaLog
        {
            EventoEn = DateTime.UtcNow,
            ModuloNombre = moduloNombre,
            EntidadNombre = entidadNombre,
            EntidadId = entidadId,
            AccionNombre = accionNombre,
            ResumenTexto = resumenTexto,
            EjecutadoPorUsuarioId = ejecutadoPorUsuarioId,
            ValoresAnterioresJson = valoresAnterioresJson,
            ValoresNuevosJson = valoresNuevosJson
        };

        await contextoAplicacion.AuditoriasLog.AddAsync(log, cancellationToken);
        await contextoAplicacion.SaveChangesAsync(cancellationToken);
    }
}
