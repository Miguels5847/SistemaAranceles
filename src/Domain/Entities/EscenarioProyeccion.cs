using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class EscenarioProyeccion : EntidadDominioBase
{
    private EscenarioProyeccion()
    {
    }

    public EscenarioProyeccion(int carreraId, string nombre, string? descripcion, bool esPredeterminado)
    {
        CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");
        CambiarNombre(nombre);
        CambiarDescripcion(descripcion);
        EsPredeterminado = esPredeterminado;
    }

    public int CarreraId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public string? Descripcion { get; private set; }
    public bool EsPredeterminado { get; private set; }

    public void CambiarNombre(string nombre)
    {
        Nombre = GuardiaDominio.Requerido(nombre, "Nombre de escenario", 120);
    }

    public void CambiarDescripcion(string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcion))
        {
            Descripcion = null;
            return;
        }

        Descripcion = GuardiaDominio.Requerido(descripcion, "Descripcion de escenario", 500);
    }

    public void DefinirPredeterminado(bool esPredeterminado)
    {
        EsPredeterminado = esPredeterminado;
    }
}
