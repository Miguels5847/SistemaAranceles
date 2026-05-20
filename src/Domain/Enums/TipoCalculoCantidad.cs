namespace SistemaAranceles.Domain.Enums;

/// <summary>
/// Origen de la cantidad de un activo fijo, derivado del analisis de la hoja Excel
/// "3 Recursos fisicos" (KAN-24).
/// </summary>
public enum TipoCalculoCantidad
{
    /// <summary>Cantidad fija ingresada por el usuario (muebles, computadores).</summary>
    Manual = 0,

    /// <summary>Cantidad = total estudiantes del semestre x FactorMultiplicador (Operatividad EVEA).</summary>
    PorEstudiante = 1,

    /// <summary>Cantidad = total docentes del semestre x FactorMultiplicador + Offset (Licencias Zoom).</summary>
    PorDocente = 2,

    /// <summary>Cantidad fija por hito del plan curricular en semestres especificos (Laboratorio Redes).</summary>
    PorHito = 3
}
