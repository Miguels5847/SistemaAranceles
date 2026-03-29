using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class UnidadTrabajo(ContextoAplicacion contextoAplicacion) : IUnidadTrabajo
{
    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
    {
        return contextoAplicacion.SaveChangesAsync(cancellationToken);
    }
}
