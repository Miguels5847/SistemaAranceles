using System;
using System.Globalization;
using System.Windows.Data;

namespace SistemaAranceles.Presentation.Converters;

public class DecimalStringConverter : IValueConverter
{
    private static readonly NumberFormatInfo DisplayFormat = new NumberFormatInfo
    {
        NumberDecimalSeparator = ",",
        NumberGroupSeparator = ".",
        NumberGroupSizes = new[] { 3 }
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
        {
            return d.ToString("N2", DisplayFormat);
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s) return 0m;
        s = s.Trim();
        if (string.IsNullOrEmpty(s)) return 0m;

        // Determine decimal separator heuristically
        int lastComma = s.LastIndexOf(',');
        int lastDot = s.LastIndexOf('.');

        char? decimalSep = null;
        if (lastComma >= 0 && lastDot >= 0)
        {
            decimalSep = lastComma > lastDot ? ',' : '.';
        }
        else if (lastComma >= 0)
        {
            // If only comma present assume decimal
            decimalSep = ',';
        }
        else if (lastDot >= 0)
        {
            decimalSep = '.';
        }

        string normalized = s;
        if (decimalSep is not null)
        {
            var dec = decimalSep.Value;
            var other = dec == ',' ? '.' : ',';
            // remove other separators (thousands)
            normalized = normalized.Replace(other.ToString(), string.Empty);
            // replace decimal separator with dot for invariant parsing
            normalized = normalized.Replace(dec.ToString(), ".");
        }

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            return result;

        return 0m;
    }
}
