using System.Windows;
using SistemaAranceles.Presentation.ViewModels;

namespace SistemaAranceles.Presentation;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        AjustarAPantalla();
    }

    // La ventana está diseñada a 1400x820; en pantallas más chicas (p. ej. laptops 1366x768)
    // se recorta al área de trabajo del monitor y se abre maximizada para no salirse.
    private void AjustarAPantalla()
    {
        var area = SystemParameters.WorkArea;

        MinWidth = Math.Min(MinWidth, area.Width);
        MinHeight = Math.Min(MinHeight, area.Height);

        if (Width >= area.Width || Height >= area.Height)
        {
            Width = Math.Min(Width, area.Width);
            Height = Math.Min(Height, area.Height);
            WindowState = WindowState.Maximized;
        }
    }
}
