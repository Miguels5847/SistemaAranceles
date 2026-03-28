using SistemaAranceles.Domain.Common;

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
        Nombre = GuardiaDominio.Requerido(nombre, "Nombre de rol", 100);
    }

    public void Describir(string descripcion)
    {
        Descripcion = GuardiaDominio.Requerido(descripcion, "Descripcion de rol", 300);
    }
}
