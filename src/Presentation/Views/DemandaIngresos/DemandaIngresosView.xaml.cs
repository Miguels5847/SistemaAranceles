using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using SistemaAranceles.Application.DTOs.DemandaIngresos;
using SistemaAranceles.Presentation.ViewModels.DemandaIngresos;

namespace SistemaAranceles.Presentation.Views.DemandaIngresos;

public partial class DemandaIngresosView : UserControl
{
    private INotifyPropertyChanged? _viewModelActual;

    public DemandaIngresosView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ActualizarColumnasDemanda();
            ActualizarColumnasDocentes();
            ActualizarColumnasIngresos();
        };
        DataContextChanged += (_, args) =>
        {
            if (_viewModelActual is not null)
                _viewModelActual.PropertyChanged -= OnViewModelPropertyChanged;

            _viewModelActual = args.NewValue as INotifyPropertyChanged;
            if (_viewModelActual is not null)
                _viewModelActual.PropertyChanged += OnViewModelPropertyChanged;

            ActualizarColumnasDemanda();
            ActualizarColumnasDocentes();
            ActualizarColumnasIngresos();
        };
    }

    private DataGrid? ObtenerDocentesGrid()
        => FindName("DocentesNecesariosGrid") as DataGrid;

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DemandaIngresosViewModel.DemandaProyectada))
        {
            ActualizarColumnasDemanda();
            ActualizarColumnasDocentes();
        }
        else if (e.PropertyName == nameof(DemandaIngresosViewModel.Ingresos))
        {
            ActualizarColumnasIngresos();
        }
    }

    private void ActualizarColumnasDemanda()
    {
        if (DemandaProyectadaGrid is null)
            return;

        DemandaProyectadaGrid.Columns.Clear();
        DemandaProyectadaGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Ciclo",
            Binding = new Binding(nameof(DemandaMatrizFilaView.CicloDisplay)),
            Width = new DataGridLength(130)
        });

        if (DataContext is DemandaIngresosViewModel vm && vm.DemandaProyectada is not null)
        {
            for (var i = 0; i < vm.DemandaProyectada.EtiquetasPeriodos.Count; i++)
            {
                DemandaProyectadaGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = vm.DemandaProyectada.EtiquetasPeriodos[i],
                    Binding = new Binding($"Periodos[{i}]") { StringFormat = "N0" },
                    Width = new DataGridLength(110)
                });
            }
        }

        DemandaProyectadaGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Total",
            Binding = new Binding(nameof(DemandaMatrizFilaView.TotalDisplay)),
            Width = new DataGridLength(110)
        });
    }

    private void ActualizarColumnasDocentes()
    {
        var docentesGrid = ObtenerDocentesGrid();
        if (docentesGrid is null)
            return;

        docentesGrid.Columns.Clear();
        docentesGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Tipo",
            Binding = new Binding(nameof(DemandaDocenteFilaDto.Tipo)),
            Width = new DataGridLength(220)
        });

        if (DataContext is DemandaIngresosViewModel vm && vm.DemandaProyectada is not null)
        {
            for (var i = 0; i < vm.DemandaProyectada.EtiquetasPeriodos.Count; i++)
            {
                docentesGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = vm.DemandaProyectada.EtiquetasPeriodos[i],
                    Binding = new Binding($"Periodos[{i}]") { StringFormat = "N0" },
                    Width = new DataGridLength(110)
                });
            }
        }

        docentesGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Total",
            Binding = new Binding(nameof(DemandaDocenteFilaDto.TotalDisplay)),
            Width = new DataGridLength(110)
        });
    }

    private void ActualizarColumnasIngresos()
    {
        if (IngresosProyectadosGrid is null)
            return;

        IngresosProyectadosGrid.Columns.Clear();
        IngresosProyectadosGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Ciclo",
            Binding = new Binding(nameof(IngresosMatrizFilaView.CicloDisplay)),
            Width = new DataGridLength(130)
        });

        if (DataContext is DemandaIngresosViewModel vm && vm.Ingresos is not null)
        {
            for (var i = 0; i < vm.Ingresos.EtiquetasPeriodos.Count; i++)
            {
                IngresosProyectadosGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = vm.Ingresos.EtiquetasPeriodos[i],
                    Binding = new Binding($"Periodos[{i}]") { StringFormat = "C2" },
                    Width = new DataGridLength(130)
                });
            }
        }

        IngresosProyectadosGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Total",
            Binding = new Binding(nameof(IngresosMatrizFilaView.TotalDisplay)),
            Width = new DataGridLength(140)
        });
    }
}
