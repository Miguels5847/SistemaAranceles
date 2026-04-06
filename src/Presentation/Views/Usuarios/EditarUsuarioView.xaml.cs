using System.Windows.Controls;
using SistemaAranceles.Presentation.ViewModels.Usuarios;

namespace SistemaAranceles.Presentation.Views.Usuarios;

public partial class EditarUsuarioView : UserControl
{
    public EditarUsuarioView()
    {
        InitializeComponent();
    }

    private void PwdBox_PasswordChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is EditarUsuarioViewModel vm)
        {
            vm.Contrasena = PwdBox.Password;
        }
    }
}
