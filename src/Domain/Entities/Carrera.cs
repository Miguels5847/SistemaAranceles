using SistemaAranceles.Domain.Common;

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
        Codigo = GuardiaDominio.Requerido(codigo, "Codigo de carrera", 40).ToUpperInvariant();
    }

    public void CambiarNombre(string nombre)
    {
        Nombre = GuardiaDominio.Requerido(nombre, "Nombre de carrera", 180);
    }

    public void CambiarFacultad(string facultadNombre)
    {
        FacultadNombre = GuardiaDominio.Requerido(facultadNombre, "Facultad", 180);
    }

    public void CambiarTotalCiclos(int totalCiclos)
    {
        TotalCiclos = GuardiaDominio.EnteroPositivo(totalCiclos, "Total de ciclos");
    }
}
