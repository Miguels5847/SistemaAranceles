using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Rubro de servicios básicos o mantenimiento institucional (KAN-28).
/// Refleja las hojas "8 Mantenimiento" A7:B12 (ServicioBasico) y A14:B20 (Mantenimiento).
/// El costo se registra a nivel universidad; se prorrata por alumno al proyectar.
/// </summary>
public sealed class ServicioMantenimiento : EntidadDominioBase
{
    private ServicioMantenimiento() { }

    public ServicioMantenimiento(
        int carreraId,
        TipoRubroMantenimiento tipoRubro,
        string nombreRubro,
        decimal costoAnualUniversidad)
    {
        CambiarCarrera(carreraId);
        CambiarTipoRubro(tipoRubro);
        CambiarNombreRubro(nombreRubro);
        CambiarCosto(costoAnualUniversidad);
    }

    public int CarreraId { get; private set; }
    public TipoRubroMantenimiento TipoRubro { get; private set; }
    public string NombreRubro { get; private set; } = string.Empty;
    public decimal CostoAnualUniversidad { get; private set; }

    public void CambiarCarrera(int carreraId)
        => CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");

    public void CambiarTipoRubro(TipoRubroMantenimiento tipo)
    {
        if (!Enum.IsDefined(tipo))
            throw new DominioException("Tipo de rubro de mantenimiento no válido.");
        TipoRubro = tipo;
    }

    public void CambiarNombreRubro(string nombre)
        => NombreRubro = GuardiaDominio.Requerido(nombre, "Nombre del rubro", 150);

    public void CambiarCosto(decimal costo)
        => CostoAnualUniversidad = GuardiaDominio.DecimalNoNegativo(costo, "Costo anual", 2);
}
