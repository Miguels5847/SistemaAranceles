using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Presentation.ViewModels.Catalogos;

namespace SistemaAranceles.Presentation.Views.Catalogos;

public partial class CatalogoCargosView : UserControl
{
    public CatalogoCargosView()
    {
        InitializeComponent();
        if (System.Windows.Application.Current is SistemaAranceles.Presentation.App)
        {
            var sp = SistemaAranceles.Presentation.App.ServiceProvider;
            if (sp != null)
                DataContext = sp.GetService(typeof(CatalogoCargosViewModel)) as CatalogoCargosViewModel;
        }
    }

    public CatalogoCargosView(CatalogoCargosViewModel vm) : this()
    {
        DataContext = vm;
    }

    private async void Importar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is CatalogoCargosViewModel vm)
        {
            // Usar carrera 1 por defecto para ejemplo si existe
            await vm.ImportarHoja6EjemploCommand.ExecuteAsync(1);
        }
    }
}
