using System.Windows;
using SistemaAranceles.Presentation.ViewModels;

namespace SistemaAranceles.Presentation.Views;

public partial class LoginView : Window
{
    public LoginView(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Contrasena = PasswordBox.Password;
        }
    }
}
