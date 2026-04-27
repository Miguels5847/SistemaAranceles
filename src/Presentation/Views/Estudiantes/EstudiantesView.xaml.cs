using System.Windows.Controls;
using System.Windows.Data;
using SistemaAranceles.Application.DTOs.Estudiantes;
using SistemaAranceles.Presentation.ViewModels.Estudiantes;

namespace SistemaAranceles.Presentation.Views.Estudiantes;

public partial class EstudiantesView : UserControl
{
    public EstudiantesView()
    {
        InitializeComponent();

        // FIX-2: suscribirse al cambio de DetalleConsolidado para construir
        //        las columnas dinámicas del DataGrid de matrícula.
        DataContextChanged += OnDataContextChanged;
    }

    private EstudiantesViewModel? _vm;

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null)
            _vm.PropertyChanged -= OnVmPropertyChanged;

        _vm = e.NewValue as EstudiantesViewModel;

        if (_vm is not null)
            _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EstudiantesViewModel.DetalleConsolidado))
            GenerarColumnasMatricula(_vm?.DetalleConsolidado);
    }

    /// <summary>
    /// Construye las columnas del DataGrid cada vez que cambia DetalleConsolidado.
    /// Columna fija "CICLO" + una columna por período + columna "TOTAL".
    /// </summary>
    private void GenerarColumnasMatricula(ProyeccionConsolidadaDto? detalle)
    {
        // Asegurarse de ejecutar en el hilo UI
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => GenerarColumnasMatricula(detalle));
            return;
        }

        dgMatriculaPeriodo.Columns.Clear();

        if (detalle is null || detalle.MatriculaPorPeriodo.Count == 0)
            return;

        var paralelos   = detalle.ParalelosPorPeriodo;
        var totalPeriodos = paralelos.Length;

        // Columna fija: CICLO
        dgMatriculaPeriodo.Columns.Add(new DataGridTextColumn
        {
            Header  = "CICLO",
            Binding = new Binding("Ciclo"),
            Width   = new DataGridLength(110)
        });

        // Una columna por período
        // El anio base lo tomamos de la primera fila de TablaPeriodos
        for (var p = 0; p < totalPeriodos; p++)
        {
            var periodo  = detalle.TablaPeriodos.Count > p ? detalle.TablaPeriodos[p] : null;
            var anio     = periodo?.Anio.ToString() ?? "";
            var semestre = periodo?.Semestre ?? (p % 2 == 0 ? "ABR" : "SEP");
            var par      = p < paralelos.Length ? paralelos[p] : 1;

            dgMatriculaPeriodo.Columns.Add(new DataGridTextColumn
            {
                Header  = $"{anio}\n{semestre}\npar:{par}",
                Binding = new Binding($"Periodos[{p}]") { StringFormat = "N0" },
                Width   = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }

        // Columna fija: TOTAL
        dgMatriculaPeriodo.Columns.Add(new DataGridTextColumn
        {
            Header  = "TOTAL",
            Binding = new Binding("Total") { StringFormat = "N0" },
            Width   = new DataGridLength(80)
        });
    }
}
