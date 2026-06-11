using System.Globalization;
using System.Windows.Data;
using MahApps.Metro.IconPacks;

namespace SistemaAranceles.Presentation.Converters;

/// <summary>
/// Convierte el nombre del icono guardado como texto en <see cref="PackIconMaterialKind"/>
/// (menú lateral, KAN-49). Un nombre desconocido o vacío produce None: celda en blanco,
/// nunca una excepción de binding.
/// </summary>
public sealed class NombreIconoMaterialConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string nombre && Enum.TryParse<PackIconMaterialKind>(nombre, ignoreCase: true, out var kind)
            ? kind
            : PackIconMaterialKind.None;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
