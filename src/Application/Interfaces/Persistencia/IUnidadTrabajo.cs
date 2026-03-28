namespace SistemaAranceles.Application.Interfaces.Persistencia;

public interface IUnidadTrabajo
{
    Task<int> GuardarCambiosAsync(CancellationToken cancellationToken = default);
}
