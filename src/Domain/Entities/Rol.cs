namespace SistemaAranceles.Domain.Entities;

public sealed class Rol : EntidadDominioBase
{
    private Rol()
    {
    }

    public Rol(string nombre, string descripcion)
    {
        Renombrar(nombre);
        Describir(descripcion);
    }

    public string Nombre { get; private set; } = string.Empty;
    public string Descripcion { get; private set; } = string.Empty;

    public void Renombrar(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre del rol es obligatorio.", nameof(nombre));
        }

        Nombre = nombre.Trim();
    }

    public void Describir(string descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new ArgumentException("La descripcion del rol es obligatoria.", nameof(descripcion));
        }

        Descripcion = descripcion.Trim();
    }
}
