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
            throw new ArgumentOutOfRangeException(nameof(anio), "El anio esta fuera de rango permitido.");
        }

        Anio = anio;
    }

    public void CambiarPorcentajeInflacion(decimal porcentajeInflacion)
    {
        if (porcentajeInflacion < -100 || porcentajeInflacion > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(porcentajeInflacion), "La inflacion debe estar en un rango valido.");
        }

        PorcentajeInflacion = decimal.Round(porcentajeInflacion, 4);
    }

    public void CambiarFuente(string fuenteNombre, string tipoFuente)
    {
        if (string.IsNullOrWhiteSpace(fuenteNombre))
        {
            throw new ArgumentException("La fuente es obligatoria.", nameof(fuenteNombre));
        }

        if (string.IsNullOrWhiteSpace(tipoFuente))
        {
            throw new ArgumentException("El tipo de fuente es obligatorio.", nameof(tipoFuente));
        }

        FuenteNombre = fuenteNombre.Trim();
        TipoFuente = tipoFuente.Trim();
    }
}
