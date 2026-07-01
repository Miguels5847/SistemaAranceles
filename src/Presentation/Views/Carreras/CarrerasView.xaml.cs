using System.Windows;
using System.Windows.Controls;
using SistemaAranceles.Presentation.Views.Compartido;

namespace SistemaAranceles.Presentation.Views.Carreras;

public partial class CarrerasView : UserControl
{
    public CarrerasView()
    {
        InitializeComponent();
    }

    private void AbrirGuia_Click(object sender, RoutedEventArgs e)
    {
        var guia = new GuiaCompletaWindow { Owner = Window.GetWindow(this) };
        guia.Show();
    }
}
