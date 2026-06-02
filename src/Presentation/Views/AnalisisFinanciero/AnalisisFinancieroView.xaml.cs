using System.ComponentModel;
using System.Windows;
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
        Loaded += (_, _) => ActualizarTodasLasColumnas();
        DataContextChanged += (_, args) =>
        {
            if (_viewModelActual is not null)
                _viewModelActual.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModelActual = args.NewValue as INotifyPropertyChanged;
            if (_viewModelActual is not null)
                _viewModelActual.PropertyChanged += OnViewModelPropertyChanged;

            ActualizarTodasLasColumnas();
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnalisisFinancieroViewModel.EstadoPerdidasGanancias)
            || e.PropertyName == nameof(AnalisisFinancieroViewModel.EtiquetasPerdidasGanancias))
        {
            ActualizarColumnasPerdidasGanancias();
        }
        else if (e.PropertyName == nameof(AnalisisFinancieroViewModel.FlujoFondos)
              || e.PropertyName == nameof(AnalisisFinancieroViewModel.EtiquetasFlujoFondos))
        {
            ActualizarColumnasFlujoFondos();
        }
    }

    private void ActualizarTodasLasColumnas()
    {
        ActualizarColumnasPerdidasGanancias();
        ActualizarColumnasFlujoFondos();
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
            Width = new DataGridLength(320),
            CellStyle = TryFindResource("PerdidasGananciasConceptoCellStyle") as Style,
            ElementStyle = TryFindResource("PerdidasGananciasConceptoTextStyle") as Style
        });

        var estiloNumero = TryFindResource("PerdidasGananciasNumeroTextStyle") as Style;
        var etiquetas = ObtenerEtiquetasPerdidasGanancias();
        for (var i = 0; i < etiquetas.Count; i++)
        {
            PerdidasGananciasGrid.Columns.Add(new DataGridTextColumn
            {
                Header = etiquetas[i],
                Binding = new Binding($"PeriodosDisplay[{i}]"),
                Width = new DataGridLength(130),
                ElementStyle = estiloNumero
            });
        }

        PerdidasGananciasGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Total",
            Binding = new Binding(nameof(EstadoPerdidasGananciasRubroDto.TotalDisplay)),
            Width = new DataGridLength(140),
            ElementStyle = estiloNumero
        });
    }

    private void ActualizarColumnasFlujoFondos()
    {
        if (FlujoFondosGrid is null)
            return;

        FlujoFondosGrid.Columns.Clear();
        FlujoFondosGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Concepto",
            Binding = new Binding(nameof(FlujoFondosRubroDto.Concepto)),
            Width = new DataGridLength(320),
            ElementStyle = TryFindResource("FlujoFondosConceptoTextStyle") as Style
        });

        var estiloNumero = TryFindResource("FlujoFondosNumeroTextStyle") as Style;
        var etiquetas = ObtenerEtiquetasFlujoFondos();
        for (var i = 0; i < etiquetas.Count; i++)
        {
            FlujoFondosGrid.Columns.Add(new DataGridTextColumn
            {
                Header = etiquetas[i],
                Binding = new Binding($"PeriodosDisplay[{i}]"),
                Width = new DataGridLength(130),
                ElementStyle = estiloNumero
            });
        }

        FlujoFondosGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Total",
            Binding = new Binding(nameof(FlujoFondosRubroDto.TotalDisplay)),
            Width = new DataGridLength(140),
            ElementStyle = estiloNumero
        });
    }

    private IReadOnlyList<string> ObtenerEtiquetasPerdidasGanancias()
        => DataContext is AnalisisFinancieroViewModel vm ? vm.EtiquetasPerdidasGanancias : [];

    private IReadOnlyList<string> ObtenerEtiquetasFlujoFondos()
        => DataContext is AnalisisFinancieroViewModel vm ? vm.EtiquetasFlujoFondos : [];
}
