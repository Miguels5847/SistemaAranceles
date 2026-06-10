using System.Globalization;
using System.Windows.Data;

namespace SistemaAranceles.Presentation.Converters;

/// <summary>
/// Muestra etiquetas amigables para el modo de cálculo del arancel sin cambiar el valor persistido:
/// "Manual" → "Arancel deseado", "AutomaticoCostoCarrera" → "Arancel referencial por costo de carrera".
/// </summary>
public sealed class ModoArancelDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (value as string) switch
        {
            "Manual" => "Arancel deseado",
            "AutomaticoCostoCarrera" => "Arancel referencial por costo de carrera",
            "OptimoFinanciero" => "Arancel óptimo financiero (VAN≈0)",
            var otro => otro ?? string.Empty
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => (value as string) switch
        {
            "Arancel deseado" => "Manual",
            "Arancel referencial por costo de carrera" => "AutomaticoCostoCarrera",
            "Arancel óptimo financiero (VAN≈0)" => "OptimoFinanciero",
            var otro => otro ?? string.Empty
        };
}
