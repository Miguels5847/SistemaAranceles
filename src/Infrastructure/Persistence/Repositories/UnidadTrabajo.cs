using Microsoft.EntityFrameworkCore.Storage;
using SistemaAranceles.Application.Interfaces.Persistencia;

namespace SistemaAranceles.Infrastructure.Persistence.Repositories;

public sealed class UnidadTrabajo(ContextoAplicacion contextoAplicacion) : IUnidadTrabajo
{
    private IDbContextTransaction? _transaccion;

    public Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default)
        => contextoAplicacion.SaveChangesAsync(cancellationToken);

    public async Task IniciarTransaccionAsync(CancellationToken cancellationToken = default)
    {
        _transaccion = await contextoAplicacion.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task ConfirmarTransaccionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaccion is null)
            throw new InvalidOperationException("No hay transacción activa para confirmar.");
        await _transaccion.CommitAsync(cancellationToken);
        await _transaccion.DisposeAsync();
        _transaccion = null;
    }

    public async Task RevertirTransaccionAsync()
    {
        if (_transaccion is null) return;
        await _transaccion.RollbackAsync(CancellationToken.None);
        await _transaccion.DisposeAsync();
        _transaccion = null;
    }
}
