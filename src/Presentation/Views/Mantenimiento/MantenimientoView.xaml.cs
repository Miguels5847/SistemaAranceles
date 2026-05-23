using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SistemaAranceles.Presentation.Views.Mantenimiento;

public partial class MantenimientoView : UserControl
{
    public MantenimientoView() => InitializeComponent();

    private void ProyeccionSemestral_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        var nombre = e.PropertyName ?? string.Empty;

        if (nombre.Contains("DESCRIP", StringComparison.OrdinalIgnoreCase))
        {
            e.Column.Width = new DataGridLength(150);
            e.Column.MinWidth = 140;
        }
        else if (nombre.Equals("VALOR", StringComparison.OrdinalIgnoreCase))
        {
            e.Column.Width = new DataGridLength(105);
            e.Column.MinWidth = 95;
        }
        else if (nombre.Equals("UNIDAD", StringComparison.OrdinalIgnoreCase))
        {
            e.Column.Width = new DataGridLength(95);
            e.Column.MinWidth = 90;
        }
        else if (!nombre.Any(char.IsDigit))
        {
            e.Column.Width = new DataGridLength(135);
            e.Column.MinWidth = 125;
        }
        else
        {
            e.Column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            e.Column.MinWidth = 115;
        }

        if (e.Column is DataGridTextColumn textColumn)
        {
            textColumn.ElementStyle = CrearEstiloCeldaTexto(nombre);
        }
    }

    private void DataGrid_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        ProyeccionScrollViewer.ScrollToVerticalOffset(ProyeccionScrollViewer.VerticalOffset - e.Delta);
    }

    private static Style CrearEstiloCeldaTexto(string nombreColumna)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(6, 0, 6, 0)));
        style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));

        if (nombreColumna.Equals("UNIDAD", StringComparison.OrdinalIgnoreCase)
            || (!nombreColumna.Any(char.IsDigit) && !nombreColumna.Contains("DESCRIP", StringComparison.OrdinalIgnoreCase)))
        {
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
        }
        else if (!nombreColumna.Contains("DESCRIP", StringComparison.OrdinalIgnoreCase))
        {
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
        }

        return style;
    }
}
