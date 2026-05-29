using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using SistemaAranceles.Application.DTOs.CostosGastos;
using SistemaAranceles.Presentation.ViewModels.CostosGastos;

namespace SistemaAranceles.Presentation.Views.CostosGastos;

public partial class CostosGastosView : UserControl
{
    private INotifyPropertyChanged? _viewModelActual;

    public CostosGastosView()
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
        if (e.PropertyName == nameof(CostosGastosViewModel.MatrizInvVinBecas)
            || e.PropertyName == nameof(CostosGastosViewModel.EtiquetasInvVinBecas))
        {
            ActualizarColumnasInvVinBecas();
        }
        else if (e.PropertyName == nameof(CostosGastosViewModel.MatrizCostosGastos)
              || e.PropertyName == nameof(CostosGastosViewModel.EtiquetasCostosGastos))
        {
            ActualizarColumnasCostosGastos();
            ActualizarColumnasPonderacion();
            ActualizarColumnasDescontado();
        }
        else if (e.PropertyName == nameof(CostosGastosViewModel.ResultadoCostoCarrera)
              || e.PropertyName == nameof(CostosGastosViewModel.EtiquetasCostoCarrera))
        {
            ActualizarColumnasCostoCarrera();
        }
    }

    private void ActualizarTodasLasColumnas()
    {
        ActualizarColumnasInvVinBecas();
        ActualizarColumnasCostosGastos();
        ActualizarColumnasPonderacion();
        ActualizarColumnasDescontado();
        ActualizarColumnasCostoCarrera();
    }

    private void ActualizarColumnasInvVinBecas()
        => ActualizarColumnasRubros(InvVinBecasGrid, ObtenerEtiquetasInvVinBecas());

    private void ActualizarColumnasCostosGastos()
        => ActualizarColumnasRubros(CostosGastosGrid, ObtenerEtiquetasCostosGastos());

    private void ActualizarColumnasPonderacion()
        => ActualizarColumnasRubros(PonderacionGrid, ObtenerEtiquetasCostosGastos());

    private void ActualizarColumnasDescontado()
        => ActualizarColumnasRubros(DescontadoGrid, ObtenerEtiquetasCostosGastos());

    private void ActualizarColumnasCostoCarrera()
    {
        if (CostoCarreraGrid is null)
            return;

        CostoCarreraGrid.Columns.Clear();
        CostoCarreraGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Concepto",
            Binding = new Binding(nameof(CostoCarreraMatrizFilaView.Concepto)),
            Width = new DataGridLength(240)
        });

        var etiquetas = ObtenerEtiquetasCostoCarrera();
        for (var i = 0; i < etiquetas.Count; i++)
        {
            CostoCarreraGrid.Columns.Add(new DataGridTextColumn
            {
                Header = etiquetas[i],
                Binding = new Binding($"PeriodosDisplay[{i}]"),
                Width = new DataGridLength(130)
            });
        }

        CostoCarreraGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Total",
            Binding = new Binding(nameof(CostoCarreraMatrizFilaView.TotalDisplay)),
            Width = new DataGridLength(140)
        });
    }

    private static void ActualizarColumnasRubros(DataGrid? grid, IReadOnlyList<string> etiquetas)
    {
        if (grid is null)
            return;

        grid.Columns.Clear();
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Grupo",
            Binding = new Binding(nameof(CostoGastoRubroDto.Grupo)),
            Width = new DataGridLength(190)
        });
        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Concepto",
            Binding = new Binding(nameof(CostoGastoRubroDto.Concepto)),
            Width = new DataGridLength(240)
        });

        for (var i = 0; i < etiquetas.Count; i++)
        {
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = etiquetas[i],
                Binding = new Binding($"PeriodosDisplay[{i}]"),
                Width = new DataGridLength(130)
            });
        }

        grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Total",
            Binding = new Binding(nameof(CostoGastoRubroDto.TotalDisplay)),
            Width = new DataGridLength(140)
        });
    }

    private IReadOnlyList<string> ObtenerEtiquetasInvVinBecas()
        => DataContext is CostosGastosViewModel vm ? vm.EtiquetasInvVinBecas : [];

    private IReadOnlyList<string> ObtenerEtiquetasCostosGastos()
        => DataContext is CostosGastosViewModel vm ? vm.EtiquetasCostosGastos : [];

    private IReadOnlyList<string> ObtenerEtiquetasCostoCarrera()
        => DataContext is CostosGastosViewModel vm ? vm.EtiquetasCostoCarrera : [];
}
