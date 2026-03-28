namespace SistemaAranceles.Domain.Entities;

public sealed class Carrera : EntidadDominioBase
{
    private Carrera()
    {
    }

    public Carrera(string codigo, string nombre, string facultadNombre, int totalCiclos)
    {
        CambiarCodigo(codigo);
        CambiarNombre(nombre);
        CambiarFacultad(facultadNombre);
        CambiarTotalCiclos(totalCiclos);
    }

    public string Codigo { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public string FacultadNombre { get; private set; } = string.Empty;
    public int TotalCiclos { get; private set; }

    public void CambiarCodigo(string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            throw new ArgumentException("El codigo de carrera es obligatorio.", nameof(codigo));
        }

        Codigo = codigo.Trim().ToUpperInvariant();
    }

    public void CambiarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException("El nombre de carrera es obligatorio.", nameof(nombre));
        }

        Nombre = nombre.Trim();
    }

    public void CambiarFacultad(string facultadNombre)
    {
        if (string.IsNullOrWhiteSpace(facultadNombre))
        {
            throw new ArgumentException("La facultad es obligatoria.", nameof(facultadNombre));
        }

        FacultadNombre = facultadNombre.Trim();
    }

    public void CambiarTotalCiclos(int totalCiclos)
    {
        if (totalCiclos <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCiclos), "El total de ciclos debe ser mayor a cero.");
        }

        TotalCiclos = totalCiclos;
    }
}
