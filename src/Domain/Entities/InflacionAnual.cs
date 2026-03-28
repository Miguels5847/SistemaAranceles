using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class InflacionAnual : EntidadDominioBase
{
    private InflacionAnual()
    {
    }

    public InflacionAnual(int anio, decimal porcentajeInflacion, string fuenteNombre, string tipoFuente)
    {
        CambiarAnio(anio);
        CambiarPorcentajeInflacion(porcentajeInflacion);
        CambiarFuente(fuenteNombre, tipoFuente);
    }

    public int Anio { get; private set; }
    public decimal PorcentajeInflacion { get; private set; }
    public string FuenteNombre { get; private set; } = string.Empty;
    public string TipoFuente { get; private set; } = string.Empty;

    public void CambiarAnio(int anio)
    {
        if (anio < 2000 || anio > 2100)
        {
            throw new DominioException("Anio de inflacion fuera del rango permitido.");
        }

        Anio = anio;
    }

    public void CambiarPorcentajeInflacion(decimal porcentajeInflacion)
    {
        if (porcentajeInflacion < -100 || porcentajeInflacion > 100)
        {
            throw new DominioException("Inflacion fuera del rango valido (-100 a 100).");
        }

        PorcentajeInflacion = decimal.Round(porcentajeInflacion, 4);
    }

    public void CambiarFuente(string fuenteNombre, string tipoFuente)
    {
        FuenteNombre = GuardiaDominio.Requerido(fuenteNombre, "Fuente de inflacion", 100);
        TipoFuente = GuardiaDominio.Requerido(tipoFuente, "Tipo de fuente", 60);
    }
}
