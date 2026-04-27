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
        DataContextChanged += OnDataContextChanged;
    }

    private EstudiantesViewModel? _vm;

    private void OnDataContextChanged(object sender,
        System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (_vm is not null) _vm.PropertyChanged -= OnVmPropertyChanged;
        _vm = e.NewValue as EstudiantesViewModel;
        if (_vm is not null) _vm.PropertyChanged += OnVmPropertyChanged;
    }

    private void OnVmPropertyChanged(object? sender,
        System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EstudiantesViewModel.DetalleConsolidado))
        {
            var detalle = _vm?.DetalleConsolidado;
            GenerarColumnasMatricula(detalle);
            GenerarColumnasDocentes(detalle);
        }
    }

    // ─── Tabla 1: Matrícula por período ────────────────────────────────────
    /// <summary>
    /// Columna fija CICLO + una columna por período con encabezado "AAAA\nABR/SEP\npar:N" + TOTAL.
    /// </summary>
    private void GenerarColumnasMatricula(ProyeccionConsolidadaDto? detalle)
    {
        if (!Dispatcher.CheckAccess())
        { Dispatcher.Invoke(() => GenerarColumnasMatricula(detalle)); return; }

        dgMatriculaPeriodo.Columns.Clear();
        if (detalle is null || detalle.MatriculaPorPeriodo.Count == 0) return;

        var paralelos     = detalle.ParalelosPorPeriodo;
        var totalPeriodos = paralelos.Length;

        dgMatriculaPeriodo.Columns.Add(new DataGridTextColumn
        {
            Header  = "CICLO",
            Binding = new Binding("Ciclo"),
            Width   = new DataGridLength(110)
        });

        for (var p = 0; p < totalPeriodos; p++)
        {
            var periodo  = detalle.TablaPeriodos.Count > p ? detalle.TablaPeriodos[p] : null;
            var anio     = periodo?.Anio.ToString() ?? string.Empty;
            var semestre = periodo?.Semestre ?? (p % 2 == 0 ? "ABR" : "SEP");
            var par      = p < paralelos.Length ? paralelos[p] : 1;

            dgMatriculaPeriodo.Columns.Add(new DataGridTextColumn
            {
                Header  = $"{anio}\n{semestre}\npar:{par}",
                Binding = new Binding($"Periodos[{p}]") { StringFormat = "N0" },
                Width   = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }

        dgMatriculaPeriodo.Columns.Add(new DataGridTextColumn
        {
            Header  = "TOTAL",
            Binding = new Binding("Total") { StringFormat = "N0" },
            Width   = new DataGridLength(80)
        });
    }

    // ─── Tabla 2: Docentes por período (misma estructura que Matrícula) ────
    /// <summary>
    /// Columna fija TIPO + una columna por período "AAAA\nABR/SEP" + TOTAL.
    /// Valores enteros (Math.Ceiling) — no existen fracciones de docente.
    /// </summary>
    private void GenerarColumnasDocentes(ProyeccionConsolidadaDto? detalle)
    {
        if (!Dispatcher.CheckAccess())
        { Dispatcher.Invoke(() => GenerarColumnasDocentes(detalle)); return; }

        dgDocentesPeriodo.Columns.Clear();
        if (detalle is null || detalle.DocentesPorPeriodo.Count == 0) return;

        var totalPeriodos = detalle.ParalelosPorPeriodo.Length;

        // Columna fija: TIPO DE DOCENTE
        dgDocentesPeriodo.Columns.Add(new DataGridTextColumn
        {
            Header  = "Tipo",
            Binding = new Binding("Tipo"),
            Width   = new DataGridLength(160)
        });

        // Una columna por período
        for (var p = 0; p < totalPeriodos; p++)
        {
            var periodo  = detalle.TablaPeriodos.Count > p ? detalle.TablaPeriodos[p] : null;
            var anio     = periodo?.Anio.ToString() ?? string.Empty;
            var semestre = periodo?.Semestre ?? (p % 2 == 0 ? "ABR" : "SEP");

            dgDocentesPeriodo.Columns.Add(new DataGridTextColumn
            {
                Header  = $"{anio}\n{semestre}",
                Binding = new Binding($"Periodos[{p}]") { StringFormat = "N0" },
                Width   = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
        }

        // Columna fija: TOTAL (máximo acumulado)
        dgDocentesPeriodo.Columns.Add(new DataGridTextColumn
        {
            Header  = "TOTAL",
            Binding = new Binding("Total") { StringFormat = "N0" },
            Width   = new DataGridLength(70)
        });
    }
}
