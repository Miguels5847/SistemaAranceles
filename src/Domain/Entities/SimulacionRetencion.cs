using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class SimulacionRetencion : EntidadDominioBase
{
    private readonly List<DetalleSimulacionRetencion> _detalles = [];

    private SimulacionRetencion()
    {
    }

    public SimulacionRetencion(int configuracionRetencionId, int cohorteAnio)
    {
        ConfiguracionRetencionId = GuardiaDominio.EnteroPositivo(configuracionRetencionId, "Configuración de retención");
        CohorteAnio = ValidarCohorte(cohorteAnio);
        FechaSimulacion = DateTime.UtcNow;
    }

    public int ConfiguracionRetencionId { get; private set; }
    public int CohorteAnio { get; private set; }
    public DateTime FechaSimulacion { get; private set; }
    public decimal RetencionPorcentajeFinal { get; private set; }
    public decimal GraduacionPorcentajeFinal { get; private set; }
    public decimal EstudiantesTotalesInicio { get; private set; }
    public decimal EstudiantesRetenidos { get; private set; }
    public decimal EstudiantesGraduados { get; private set; }
    public decimal CostoMatriculaPromedio { get; private set; }

    public IReadOnlyCollection<DetalleSimulacionRetencion> Detalles => _detalles;

    public void EstablecerFechaSimulacion(DateTime fechaSimulacion)
    {
        FechaSimulacion = fechaSimulacion;
    }

    public void ActualizarCohorte(int cohorteAnio)
    {
        CohorteAnio = ValidarCohorte(cohorteAnio);
    }

    public void ReemplazarDetalles(IEnumerable<DetalleSimulacionRetencion> detalles)
    {
        ArgumentNullException.ThrowIfNull(detalles);
        _detalles.Clear();
        _detalles.AddRange(detalles);
    }

    public void ActualizarIndicadoresFinales(decimal estudiantesInicio, decimal estudiantesRetenidos, decimal estudiantesGraduados, decimal costoMatriculaPromedio)
    {
        EstudiantesTotalesInicio = GuardiaDominio.DecimalNoNegativo(estudiantesInicio, "Estudiantes totales inicio", 4);
        EstudiantesRetenidos = GuardiaDominio.DecimalNoNegativo(estudiantesRetenidos, "Estudiantes retenidos", 4);
        EstudiantesGraduados = GuardiaDominio.DecimalNoNegativo(estudiantesGraduados, "Estudiantes graduados", 4);
        CostoMatriculaPromedio = GuardiaDominio.DecimalNoNegativo(costoMatriculaPromedio, "Costo matrícula promedio", 2);

        if (EstudiantesTotalesInicio == 0)
        {
            RetencionPorcentajeFinal = 0;
            GraduacionPorcentajeFinal = 0;
            return;
        }

        RetencionPorcentajeFinal = decimal.Round((EstudiantesRetenidos / EstudiantesTotalesInicio) * 100m, 4);
        GraduacionPorcentajeFinal = decimal.Round((EstudiantesGraduados / EstudiantesTotalesInicio) * 100m, 4);
    }

    private static int ValidarCohorte(int cohorteAnio)
    {
        if (cohorteAnio < 2012 || cohorteAnio > 2050)
            throw new DominioException("El año de cohorte debe estar entre 2012 y 2050.");

        return cohorteAnio;
    }
}
