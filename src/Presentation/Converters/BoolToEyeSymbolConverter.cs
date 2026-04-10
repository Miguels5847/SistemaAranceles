using System.Globalization;
using System.Windows.Data;

namespace SistemaAranceles.Presentation.Converters;

public class BoolToEyeSymbolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isMostrar)
        {
            return isMostrar ? "👁️" : "🔐";
        }
        return "🔐";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
