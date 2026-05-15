using System.Windows.Controls;
using SistemaAranceles.Presentation.ViewModels.DatosInstitucionales;

namespace SistemaAranceles.Presentation.Views.DatosInstitucionales;

public partial class DatosInstitucionalesView : UserControl
{
    public DatosInstitucionalesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public DatosInstitucionalesView(DatosInstitucionalesViewModel vm) : this()
    {
        DataContext = vm;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (DataContext is DatosInstitucionalesViewModel vm)
        {
            await vm.CargarCommand.ExecuteAsync(null);
        }
    }
}
