using System.Windows.Controls;
using SistemaAranceles.Presentation.ViewModels.PlantaCentral;

namespace SistemaAranceles.Presentation.Views.PlantaCentral;

public partial class AportePlantaCentralView : UserControl
{
    public AportePlantaCentralView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public AportePlantaCentralView(AportePlantaCentralViewModel vm) : this()
    {
        DataContext = vm;
    }

    private async void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (DataContext is AportePlantaCentralViewModel vm)
        {
            await vm.CargarCommand.ExecuteAsync(null);
        }
    }
}
