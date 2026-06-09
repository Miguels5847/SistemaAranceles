using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SistemaAranceles.Application.DTOs.CargosFacultad;

namespace SistemaAranceles.Presentation.Views.CargosFacultad;

public partial class ResumenSueldosWindow : Window
{
    public ResumenSueldosWindow()
    {
        InitializeComponent();
    }

    public void CargarResumen(ResumenSueldosVistaDto resumen)
    {
        ArgumentNullException.ThrowIfNull(resumen);

        TxtSubtitulo.Text = string.IsNullOrWhiteSpace(resumen.CarreraNombre)
            ? $"{resumen.Periodos.Count} períodos · {resumen.Filas.Count} cargos"
            : $"Carrera: {resumen.CarreraNombre} · {resumen.Periodos.Count} períodos · {resumen.Filas.Count} cargos";

        TxtGranTotal.Text = resumen.GranTotal.ToString("C2", CultureInfo.CurrentCulture);

        // Update dynamic total-accumulated label: compute years from number of periods
        var periodosCount = resumen.Periodos?.Count ?? 0;
        var anos = periodosCount / 2m; // 2 períodos = 1 año
        var anosTexto = (anos % 1m == 0m)
            ? ((int)anos).ToString(CultureInfo.CurrentCulture)
            : anos.ToString("0.##", CultureInfo.CurrentCulture);
        var sufijoAnio = anos == 1m ? "año" : "años";

        TxtLabelTotalAcumulado.Text = $"Total acumulado {anosTexto} {sufijoAnio} (suma todos los períodos × 6 meses)";

        ConstruirColumnasResumen(resumen);
        ConstruirColumnasTotales(resumen);

        dgResumen.ItemsSource = resumen.Filas;
        dgTotales.ItemsSource = new[] { resumen };
    }

    private void ConstruirColumnasResumen(ResumenSueldosVistaDto resumen)
    {
        dgResumen.Columns.Clear();

        dgResumen.Columns.Add(new DataGridTextColumn
        {
            Header = "PESO",
            Binding = new Binding(nameof(FilaResumenSueldosDto.Peso)) { StringFormat = "N4" },
            Width = new DataGridLength(80),
        });
        dgResumen.Columns.Add(new DataGridTextColumn
        {
            Header = "Nº",
            Binding = new Binding(nameof(FilaResumenSueldosDto.NumeroPersonas)) { StringFormat = "N2" },
            Width = new DataGridLength(70),
        });
        dgResumen.Columns.Add(new DataGridTextColumn
        {
            Header = "PERSONAL",
            Binding = new Binding(nameof(FilaResumenSueldosDto.NombreCargo)),
            Width = new DataGridLength(220),
        });

        for (int i = 0; i < resumen.Periodos.Count; i++)
        {
            var p = resumen.Periodos[i];
            dgResumen.Columns.Add(new DataGridTextColumn
            {
                Header = $"P{p.NumeroPeriodo}-{p.Anio}",
                Binding = new Binding($"{nameof(FilaResumenSueldosDto.ValoresPorPeriodo)}[{i}]") { StringFormat = "N2" },
                Width = new DataGridLength(110),
            });
        }

    }

    private void ConstruirColumnasTotales(ResumenSueldosVistaDto resumen)
    {
        dgTotales.Columns.Clear();

        dgTotales.Columns.Add(new DataGridTextColumn
        {
            Binding = new Binding { Source = string.Empty },
            Width = new DataGridLength(80),
        });
        dgTotales.Columns.Add(new DataGridTextColumn
        {
            Binding = new Binding { Source = string.Empty },
            Width = new DataGridLength(70),
        });
        dgTotales.Columns.Add(new DataGridTextColumn
        {
            Binding = new Binding { Source = "TOTAL" },
            Width = new DataGridLength(220),
            FontWeight = FontWeights.Bold,
        });

        for (int i = 0; i < resumen.Periodos.Count; i++)
        {
            dgTotales.Columns.Add(new DataGridTextColumn
            {
                Binding = new Binding($"{nameof(ResumenSueldosVistaDto.TotalesPorPeriodo)}[{i}]") { StringFormat = "N2" },
                Width = new DataGridLength(110),
                FontWeight = FontWeights.Bold,
            });
        }

    }
}
