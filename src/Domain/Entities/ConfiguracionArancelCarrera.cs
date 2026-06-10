using SistemaAranceles.Domain.Common;
using SistemaAranceles.Domain.Enums;

namespace SistemaAranceles.Domain.Entities;

/// <summary>
/// Configuración de arancel y matrícula por carrera (KAN-32, Épica 9).
/// El arancel es por carrera (opcionalmente por escenario), NO global.
/// </summary>
public sealed class ConfiguracionArancelCarrera : EntidadDominioBase
{
    private ConfiguracionArancelCarrera()
    {
    }

    public ConfiguracionArancelCarrera(
        int carreraId,
        int? escenarioProyeccionId,
        ModoCalculoArancel modoCalculo,
        decimal? arancelManual,
        decimal? porcentajeMatricula,
        bool usaPorcentajeMatriculaInstitucional)
    {
        CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");
        AsignarEscenario(escenarioProyeccionId);
        CambiarConfiguracion(modoCalculo, arancelManual, porcentajeMatricula, usaPorcentajeMatriculaInstitucional);
        EstaActivo = true;
    }

    public int CarreraId { get; private set; }
    public int? EscenarioProyeccionId { get; private set; }
    public ModoCalculoArancel ModoCalculo { get; private set; }
    public decimal? ArancelManual { get; private set; }
    public decimal? PorcentajeMatricula { get; private set; }
    public bool UsaPorcentajeMatriculaInstitucional { get; private set; }
    public bool EstaActivo { get; private set; }

    public void AsignarEscenario(int? escenarioProyeccionId)
    {
        if (escenarioProyeccionId is int valor && valor <= 0)
        {
            throw new DominioException("El escenario debe ser válido o nulo.");
        }
        EscenarioProyeccionId = escenarioProyeccionId;
    }

    public void CambiarConfiguracion(
        ModoCalculoArancel modoCalculo,
        decimal? arancelManual,
        decimal? porcentajeMatricula,
        bool usaPorcentajeMatriculaInstitucional)
    {
        ModoCalculo = modoCalculo;

        if (modoCalculo is ModoCalculoArancel.Manual or ModoCalculoArancel.OptimoFinanciero)
        {
            if (arancelManual is null or <= 0m)
            {
                throw new DominioException("El arancel es obligatorio en modo Manual u Óptimo financiero y debe ser mayor a cero.");
            }
            ArancelManual = decimal.Round(arancelManual.Value, 2);
        }
        else
        {
            ArancelManual = arancelManual is { } v && v > 0m ? decimal.Round(v, 2) : null;
        }

        UsaPorcentajeMatriculaInstitucional = usaPorcentajeMatriculaInstitucional;
        if (usaPorcentajeMatriculaInstitucional)
        {
            PorcentajeMatricula = null;
        }
        else
        {
            if (porcentajeMatricula is null)
            {
                throw new DominioException("Debe indicar un porcentaje de matrícula si no usa el institucional.");
            }
            PorcentajeMatricula = GuardiaDominio.Porcentaje(porcentajeMatricula.Value, "Porcentaje de matrícula");
        }
    }

    public void Activar() => EstaActivo = true;
    public void Desactivar() => EstaActivo = false;

    /// <summary>
    /// Calcula el arancel efectivo. Retorna null si Modo=Automático y no hay costo de carrera.
    /// </summary>
    public decimal? CalcularArancelEfectivo(decimal? costoCarrera, int totalSemestres)
    {
        return ModoCalculo switch
        {
            ModoCalculoArancel.Manual => ArancelManual,
            ModoCalculoArancel.OptimoFinanciero => ArancelManual,
            ModoCalculoArancel.AutomaticoCostoCarrera when costoCarrera is > 0m && totalSemestres > 0
                => decimal.Round(costoCarrera.Value / totalSemestres, 2),
            _ => null
        };
    }

    public decimal CalcularMatricula(decimal arancelEfectivo, decimal porcentajeMatriculaInstitucional)
    {
        var porcentaje = UsaPorcentajeMatriculaInstitucional
            ? porcentajeMatriculaInstitucional
            : PorcentajeMatricula ?? 0m;
        return decimal.Round(arancelEfectivo * porcentaje / 100m, 2);
    }
}
