using System.Windows.Controls;
using SistemaAranceles.Presentation.ViewModels.Catalogos;

namespace SistemaAranceles.Presentation.Views.Catalogos;

public partial class CatalogoMaterialesView : UserControl
{
    public CatalogoMaterialesView()
    {
        InitializeComponent();
        if (System.Windows.Application.Current is SistemaAranceles.Presentation.App)
        {
            var sp = SistemaAranceles.Presentation.App.ServiceProvider;
            if (sp != null)
                DataContext = sp.GetService(typeof(CatalogoMaterialesViewModel)) as CatalogoMaterialesViewModel;
        }
    }

    public CatalogoMaterialesView(CatalogoMaterialesViewModel vm) : this()
    {
        DataContext = vm;
    }
}
