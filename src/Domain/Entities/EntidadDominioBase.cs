namespace SistemaAranceles.Domain.Entities;

public abstract class EntidadDominioBase
{
    public int Id { get; protected set; }

    public void RehidratarId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "El identificador debe ser mayor a cero.");
        }

        Id = id;
    }
}
