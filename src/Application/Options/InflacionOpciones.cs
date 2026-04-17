namespace SistemaAranceles.Application.Options;

public sealed class InflacionOpciones
{
    /// <summary>
    /// Métodos soportados:
    /// - regresion-lineal (default)
    /// - promedio-suave
    /// </summary>
    public string MetodoProyeccion { get; set; } = "regresion-lineal";
}