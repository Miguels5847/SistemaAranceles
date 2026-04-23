namespace SistemaAranceles.Application.Options;

public sealed class InflacionOpciones
{
    /// <summary>
    /// Métodos soportados:
    /// - regresion-lineal (default)
    /// - promedio-suave
    /// </summary>
    public string MetodoProyeccion { get; set; } = "promedio-suave";

    /// <summary>
    /// Piso mínimo permitido para valores proyectados.
    /// Nunca se permite 0 o valores negativos en la proyección final.
    /// </summary>
    public decimal PisoMinimoProyeccion { get; set; } = 0.50m;

    /// <summary>
    /// Techo de referencia para escenarios con historial insuficiente.
    /// Se usa para mantener una proyección estable entre piso y techo.
    /// </summary>
    public decimal TechoEstableSinHistorial { get; set; } = 1.00m;

    /// <summary>
    /// Ventana de años recientes para promedio suave (3 a 5 recomendado).
    /// </summary>
    public int VentanaAniosRecientes { get; set; } = 5;

    /// <summary>
    /// Valor objetivo de convergencia para la tendencia suavizada.
    /// Si no se especifica, se utiliza TechoEstableSinHistorial.
    /// </summary>
    public decimal? ValorObjetivoConvergencia { get; set; }

    /// <summary>
    /// Intensidad de convergencia anual hacia el objetivo (30% a 50% recomendado).
    /// </summary>
    public decimal FactorConvergenciaAnual { get; set; } = 0.40m;

    /// <summary>
    /// Techo máximo permitido para la proyección anual.
    /// Sirve como control de seguridad ante configuraciones fuera de escala.
    /// </summary>
    public decimal TechoMaximoProyeccion { get; set; } = 5.00m;
}