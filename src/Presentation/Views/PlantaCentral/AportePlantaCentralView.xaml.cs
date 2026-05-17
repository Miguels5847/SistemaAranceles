using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SistemaAranceles.Presentation.ViewModels.PlantaCentral;

namespace SistemaAranceles.Presentation.Views.PlantaCentral;

public partial class AportePlantaCentralView : UserControl
{
    public AportePlantaCentralView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private AportePlantaCentralViewModel? _vm;

    public AportePlantaCentralView(AportePlantaCentralViewModel vm) : this()
    {
        DataContext = vm;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = e.NewValue as AportePlantaCentralViewModel;
        if (_vm is not null)
        {
            _vm.PropertyChanged += OnVmPropertyChanged;
            GenerarColumnasTablaResumen();
        }
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AportePlantaCentralViewModel.TablaResumenAporte)
            || e.PropertyName == nameof(AportePlantaCentralViewModel.EncabezadosPeriodos))
        {
            GenerarColumnasTablaResumen();
        }
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (DataContext is AportePlantaCentralViewModel vm)
        {
            await vm.CargarCommand.ExecuteAsync(null);
        }
    }

    private void GenerarColumnasTablaResumen()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(GenerarColumnasTablaResumen);
            return;
        }

        dgTablaAporte.Columns.Clear();
        if (_vm is null || _vm.TablaResumenAporte.Count == 0)
        {
            return;
        }

        dgTablaAporte.Columns.Add(new DataGridTextColumn
        {
            Header = string.Empty,
            Binding = new Binding(nameof(FilaAportePlantaCentralTablaViewModel.Concepto)),
            Width = new DataGridLength(175),
            MinWidth = 175,
            FontWeight = FontWeights.Bold,
            ElementStyle = CrearEstiloTexto(TextAlignment.Left),
        });

        for (var i = 0; i < _vm.EncabezadosPeriodos.Count; i++)
        {
            dgTablaAporte.Columns.Add(new DataGridTextColumn
            {
                Header = _vm.EncabezadosPeriodos[i],
                Binding = new Binding($"{nameof(FilaAportePlantaCentralTablaViewModel.Valores)}[{i}]"),
                Width = new DataGridLength(125),
                MinWidth = 125,
                ElementStyle = CrearEstiloTexto(TextAlignment.Right),
            });
        }
    }

    private static Style CrearEstiloTexto(TextAlignment alignment)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, alignment));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(6, 0, 6, 0)));
        style.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
        return style;
    }
}
