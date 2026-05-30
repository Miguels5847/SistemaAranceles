using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using SistemaAranceles.Application.DTOs.AnalisisFinanciero;
using SistemaAranceles.Presentation.ViewModels.AnalisisFinanciero;

namespace SistemaAranceles.Presentation.Views.AnalisisFinanciero;

public partial class AnalisisFinancieroView : UserControl
{
    private INotifyPropertyChanged? _viewModelActual;

    public AnalisisFinancieroView()
    {
        InitializeComponent();
        Loaded += (_, _) => ActualizarColumnasPerdidasGanancias();
        DataContextChanged += (_, args) =>
        {
            if (_viewModelActual is not null)
                _viewModelActual.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModelActual = args.NewValue as INotifyPropertyChanged;
            if (_viewModelActual is not null)
                _viewModelActual.PropertyChanged += OnViewModelPropertyChanged;

            ActualizarColumnasPerdidasGanancias();
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnalisisFinancieroViewModel.EstadoPerdidasGanancias)
            || e.PropertyName == nameof(AnalisisFinancieroViewModel.EtiquetasPerdidasGanancias))
        {
            ActualizarColumnasPerdidasGanancias();
        }
    }

    private void ActualizarColumnasPerdidasGanancias()
    {
        if (PerdidasGananciasGrid is null)
            return;

        PerdidasGananciasGrid.Columns.Clear();
        PerdidasGananciasGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Concepto",
            Binding = new Binding(nameof(EstadoPerdidasGananciasRubroDto.Concepto)),
            Width = new DataGridLength(280)
        });

        var etiquetas = ObtenerEtiquetasPerdidasGanancias();
        for (var i = 0; i < etiquetas.Count; i++)
        {
            PerdidasGananciasGrid.Columns.Add(new DataGridTextColumn
            {
                Header = etiquetas[i],
                Binding = new Binding($"PeriodosDisplay[{i}]"),
                Width = new DataGridLength(130)
            });
        }

        PerdidasGananciasGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Total",
            Binding = new Binding(nameof(EstadoPerdidasGananciasRubroDto.TotalDisplay)),
            Width = new DataGridLength(140)
        });
    }

    private IReadOnlyList<string> ObtenerEtiquetasPerdidasGanancias()
        => DataContext is AnalisisFinancieroViewModel vm ? vm.EtiquetasPerdidasGanancias : [];
}
