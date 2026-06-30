using SistemaAranceles.Domain.Common;

namespace SistemaAranceles.Domain.Entities;

public sealed class ConfiguracionRetencion : EntidadDominioBase
{
    private ConfiguracionRetencion()
    {
    }

    public ConfiguracionRetencion(
        int carreraId,
        int escenarioProyeccionId,
        int totalCiclos,
        decimal tasaRetencionPorcentaje,
        decimal tasaGraduacionPorcentaje)
    {
        CarreraId = GuardiaDominio.EnteroPositivo(carreraId, "Carrera");
        EscenarioProyeccionId = GuardiaDominio.EnteroPositivo(escenarioProyeccionId, "Escenario de proyección");
        TotalCiclos = GuardiaDominio.EnteroPositivo(totalCiclos, "Total de ciclos");
        ActualizarTasas(tasaRetencionPorcentaje, tasaGraduacionPorcentaje);
    }

    public int CarreraId { get; private set; }
    public int EscenarioProyeccionId { get; private set; }
    public int TotalCiclos { get; private set; }

    // Tasas POR CICLO: alimentan el resto del sistema (estudiantes, demanda, docentes...). Desde el
    // cambio de KAN-47 ya no son input directo: se DERIVAN de las metas acumuladas (ver DefinirMetas).
    public decimal TasaRetencionPorcentaje { get; private set; }
    public decimal TasaGraduacionPorcentaje { get; private set; }

    // Metas ACUMULADAS (último/primer estudiante de cada mitad de la malla). Son el input del usuario.
    public decimal MetaRetencionPorcentaje { get; private set; }
    public decimal MetaGraduacionPorcentaje { get; private set; }

    public decimal EstudiantesPeriodo1 { get; private set; }
    public decimal EstudiantesPeriodo2 { get; private set; }
    public int ParalelosPeriodo1 { get; private set; }
    public int ParalelosPeriodo2 { get; private set; }

    public void ActualizarTasas(decimal tasaRetencionPorcentaje, decimal tasaGraduacionPorcentaje)
    {
        TasaRetencionPorcentaje = GuardiaDominio.Porcentaje(tasaRetencionPorcentaje, "Tasa de retención");
        TasaGraduacionPorcentaje = GuardiaDominio.Porcentaje(tasaGraduacionPorcentaje, "Tasa de graduación");
    }

    /// <summary>
    /// Define las metas acumuladas (input del usuario) y DERIVA las tasas por ciclo que alimentan
    /// el cálculo completo: tasa = meta^(1/pasos). Reproduce el Excel (hoja "2 Tasa de Retención").
    /// </summary>
    public void DefinirMetas(decimal metaRetencionPorcentaje, decimal metaGraduacionPorcentaje)
    {
        MetaRetencionPorcentaje = GuardiaDominio.Porcentaje(metaRetencionPorcentaje, "Meta de retención");
        MetaGraduacionPorcentaje = GuardiaDominio.Porcentaje(metaGraduacionPorcentaje, "Meta de graduación");
        TasaRetencionPorcentaje = TasaPorCicloDesdeMeta(MetaRetencionPorcentaje, PasosRetencion(TotalCiclos));
        TasaGraduacionPorcentaje = TasaPorCicloDesdeMeta(MetaGraduacionPorcentaje, PasosGraduacion(TotalCiclos));
    }

    /// <summary>Carga metas ya almacenadas SIN re-derivar la tasa (rehidratación desde BD).</summary>
    public void CargarMetas(decimal metaRetencionPorcentaje, decimal metaGraduacionPorcentaje)
    {
        MetaRetencionPorcentaje = GuardiaDominio.Porcentaje(metaRetencionPorcentaje, "Meta de retención");
        MetaGraduacionPorcentaje = GuardiaDominio.Porcentaje(metaGraduacionPorcentaje, "Meta de graduación");
    }

    // Transiciones que mide cada mitad: 1ª mitad = ciclo[mitad]/ciclo[1]; 2ª mitad = ciclo[total]/ciclo[mitad+1].
    public static int PasosRetencion(int totalCiclos) => Math.Max(1, totalCiclos / 2 - 1);
    public static int PasosGraduacion(int totalCiclos) => Math.Max(1, totalCiclos - totalCiclos / 2 - 1);

    public static decimal TasaPorCicloDesdeMeta(decimal metaPorcentaje, int pasos)
    {
        pasos = Math.Max(1, pasos);
        var meta = (double)metaPorcentaje / 100.0;
        if (meta <= 0) return 0m;
        return decimal.Round((decimal)(Math.Pow(meta, 1.0 / pasos) * 100.0), 4);
    }

    public static decimal MetaDesdeTasaPorCiclo(decimal tasaPorCicloPorcentaje, int pasos)
    {
        pasos = Math.Max(1, pasos);
        var tasa = (double)tasaPorCicloPorcentaje / 100.0;
        if (tasa <= 0) return 0m;
        return decimal.Round((decimal)(Math.Pow(tasa, pasos) * 100.0), 4);
    }

    public void ActualizarBaseEstudiantes(decimal estudiantesPeriodo1, decimal estudiantesPeriodo2, int paralelosPeriodo1, int paralelosPeriodo2)
    {
        EstudiantesPeriodo1 = GuardiaDominio.DecimalNoNegativo(estudiantesPeriodo1, "Estudiantes período 1", 4);
        EstudiantesPeriodo2 = GuardiaDominio.DecimalNoNegativo(estudiantesPeriodo2, "Estudiantes período 2", 4);
        ParalelosPeriodo1 = GuardiaDominio.EnteroNoNegativo(paralelosPeriodo1, "Paralelos período 1");
        ParalelosPeriodo2 = GuardiaDominio.EnteroNoNegativo(paralelosPeriodo2, "Paralelos período 2");
    }
}
