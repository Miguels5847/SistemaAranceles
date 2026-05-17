using System;
using System.Globalization;
using System.Windows;
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

        // Si el texto termina en separador decimal (coma o punto),
        // el usuario aún está escribiendo los decimales → no interrumpir.
        if (s.EndsWith(",") || s.EndsWith("."))
            return Binding.DoNothing;

        // Si termina en un solo dígito decimal (ej: "890,8" o "890.8"),
        // el usuario todavía puede escribir el segundo decimal → no interrumpir.
        int lastCommaIdx = s.LastIndexOf(',');
        int lastDotIdx   = s.LastIndexOf('.');

        int decSepIdx = -1;
        char decSepChar = '.';

        if (lastCommaIdx >= 0 && lastDotIdx >= 0)
        {
            // Ambos presentes: el que aparece más a la derecha es el decimal
            if (lastCommaIdx > lastDotIdx) { decSepIdx = lastCommaIdx; decSepChar = ','; }
            else                           { decSepIdx = lastDotIdx;   decSepChar = '.'; }
        }
        else if (lastCommaIdx >= 0) { decSepIdx = lastCommaIdx; decSepChar = ','; }
        else if (lastDotIdx   >= 0) { decSepIdx = lastDotIdx;   decSepChar = '.'; }

        if (decSepIdx >= 0)
        {
            int decimalsTyped = s.Length - decSepIdx - 1;
            // Solo 1 decimal escrito y no hay más separadores después → esperar segundo dígito
            if (decimalsTyped == 1)
                return Binding.DoNothing;
        }

        // ── Normalización para parseo ──────────────────────────────────────
        string normalized = s;
        if (decSepIdx >= 0)
        {
            char other = decSepChar == ',' ? '.' : ',';
            normalized = normalized.Replace(other.ToString(), string.Empty); // quitar miles
            normalized = normalized.Replace(decSepChar.ToString(), ".");     // decimal → punto
        }
        else
        {
            // Sin separador decimal: quitar cualquier separador de miles
            normalized = normalized.Replace(".", string.Empty).Replace(",", string.Empty);
        }

        if (decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
            return result;

        return Binding.DoNothing; // texto inválido → no borrar lo que el usuario escribió
    }
}
