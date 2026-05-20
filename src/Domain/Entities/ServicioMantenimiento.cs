using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Rubro de servicios básicos o mantenimiento institucional (KAN-28).
/// Refleja la hoja "8 Mantenimiento" y permite configuración por carrera con escenario opcional.
/// El costo anual se prorratea por alumno al proyectar.
/// </summary>
public sealed class ServicioMantenimiento : EntidadDominioBase
{
    private ServicioMantenimiento() { }

    public ServicioMantenimiento(
        int carreraId,
        TipoRubroMantenimiento tipoRubro,
        string nombreRubro,
        decimal costoAnualUniversidad,
        int? escenarioProyeccionId = null,
        string? sede = null)
    {
        CambiarCarrera(carreraId);
        CambiarEscenario(escenarioProyeccionId);
        CambiarSede(sede);
        CambiarTipoRubro(tipoRubro);
        CambiarNombreRubro(nombreRubro);
        CambiarCosto(costoAnualUniversidad);
    }

    public int CarreraId { get; private set; }
    public int? EscenarioProyeccionId { get; private set; }
    public string Sede { get; private set; } = "General";
    public TipoRubroMantenimiento TipoRubro { get; private set; }
    public string NombreRubro { get; private set; } = string.Empty;
    public decimal CostoAnualUniversidad { get; private set; }

    public void CambiarCarrera(int carreraId)
        => CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");

    public void CambiarEscenario(int? escenarioProyeccionId)
    {
        if (escenarioProyeccionId is <= 0)
            throw new DominioException("Escenario de proyección no válido.");

        EscenarioProyeccionId = escenarioProyeccionId;
    }

    public void CambiarSede(string? sede)
    {
        Sede = string.IsNullOrWhiteSpace(sede)
            ? "General"
            : GuardiaDominio.Requerido(sede, "Sede", 100);
    }

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
