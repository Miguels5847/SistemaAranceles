namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IUnidadTrabajo
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
    Task IniciarTransaccionAsync(CancellationToken cancellationToken = default);
    Task ConfirmarTransaccionAsync(CancellationToken cancellationToken = default);
    Task RevertirTransaccionAsync();
}
