using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class InflacionProyectada : EntidadDominioBase
{
    private InflacionProyectada()
    {
    }

    public InflacionProyectada(int escenarioProyeccionId, int anio, decimal porcentajeInflacion, string metodoProyeccion, bool esAjusteManual)
    {
        EscenarioProyeccionId = GuardiaDominio.EnteroPositivo(escenarioProyeccionId, "Escenario de proyección");
        CambiarAnio(anio);
        CambiarPorcentajeInflacion(porcentajeInflacion);
        CambiarMetodoProyeccion(metodoProyeccion);
        EsAjusteManual = esAjusteManual;
    }

    public int EscenarioProyeccionId { get; private set; }
    public int Anio { get; private set; }
    public decimal PorcentajeInflacion { get; private set; }
    public string MetodoProyeccion { get; private set; } = string.Empty;
    public bool EsAjusteManual { get; private set; }

    public void CambiarAnio(int anio)
    {
        if (anio < 2000 || anio > 2100)
        {
            throw new DominioException("Año de inflación proyectada fuera del rango permitido.");
        }

        Anio = anio;
    }

    public void CambiarPorcentajeInflacion(decimal porcentajeInflacion)
    {
        if (porcentajeInflacion < -100 || porcentajeInflacion > 100)
        {
            throw new DominioException("Inflación proyectada fuera del rango válido (-100 a 100).");
        }

        PorcentajeInflacion = decimal.Round(porcentajeInflacion, 4);
    }

    public void CambiarMetodoProyeccion(string metodoProyeccion)
    {
        MetodoProyeccion = GuardiaDominio.Requerido(metodoProyeccion, "Método de proyección", 80);
    }

    public void DefinirAjusteManual(bool esAjusteManual)
    {
        EsAjusteManual = esAjusteManual;
    }
}
