using System.Windows;
using System.Windows.Controls;
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
            if (RevealedTextBox != null && RevealedTextBox.Text != PasswordBox.Password)
            {
                RevealedTextBox.Text = PasswordBox.Password;
            }
        }
    }

    private void RevealedTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (DataContext is LoginViewModel vm)
        {
            vm.Contrasena = RevealedTextBox.Text;
            if (PasswordBox != null && PasswordBox.Password != RevealedTextBox.Text)
            {
                PasswordBox.Password = RevealedTextBox.Text;
            }
        }
    }
}
