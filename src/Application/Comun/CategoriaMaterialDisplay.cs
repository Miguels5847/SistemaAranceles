using System.Globalization;

namespace SistemaAranceles.Application.Comun;

/// <summary>
/// Convierte los códigos de categoría de materiales (MATERIALES_SUMINISTROS, etc.) a un
/// nombre legible para pantalla, PDF y XLSX. El código crudo sigue siendo el valor
/// almacenado/lógico; esto es solo presentación (KAN-49).
/// </summary>
public static class CategoriaMaterialDisplay
{
    public static string Formatear(string? categoria) => categoria?.Trim().ToUpperInvariant() switch
    {
        null or "" => string.Empty,
        "MATERIALES_SUMINISTROS" => "Materiales y Suministros",
        "ASEO_LIMPIEZA" => "Suministros de Aseo y Limpieza",
        "ACCESORIOS_MATERIALES" => "Accesorios y Materiales",
        "OTRO" => "Otros",
        _ => Capitalizar(categoria)
    };

    /// <summary>Orden de presentación de las categorías (el mismo en pantalla, PDF y XLSX).</summary>
    public static int Orden(string? categoria) => categoria?.Trim().ToUpperInvariant() switch
    {
        "MATERIALES_SUMINISTROS" => 1,
        "ASEO_LIMPIEZA" => 2,
        "ACCESORIOS_MATERIALES" => 3,
        "OTRO" => 99,
        _ => 50
    };

    /// <summary>Fallback para categorías futuras: GUION_BAJO → "Guion Bajo".</summary>
    private static string Capitalizar(string categoria)
    {
        var palabras = categoria.Trim().Replace('_', ' ').ToLowerInvariant();
        return CultureInfo.GetCultureInfo("es-EC").TextInfo.ToTitleCase(palabras);
    }
}
