using System.Windows;
using SistemaAranceles.Presentation.ViewModels;
using MahApps.Metro.IconPacks;

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
            // También sincronizar con RevealedTextBox si está visible
            if (RevealedTextBox != null)
            {
                RevealedTextBox.Text = PasswordBox.Password;
            }
        }
    }

    private void ToggleReveal_Checked(object sender, RoutedEventArgs e)
    {
        // Mostrar texto claro y ocultar PasswordBox
        if (RevealedTextBox != null && PasswordBox != null)
        {
            RevealedTextBox.Text = PasswordBox.Password;
            PasswordBox.Visibility = Visibility.Collapsed;
            RevealedTextBox.Visibility = Visibility.Visible;
        }
        
        // Cambiar visibilidad de iconos
        if (EyeOpenIcon != null && EyeClosedIcon != null)
        {
            EyeOpenIcon.Visibility = Visibility.Collapsed;
            EyeClosedIcon.Visibility = Visibility.Visible;
        }
    }

    private void ToggleReveal_Unchecked(object sender, RoutedEventArgs e)
    {
        // Ocultar texto claro y mostrar PasswordBox
        if (RevealedTextBox != null && PasswordBox != null)
        {
            PasswordBox.Password = RevealedTextBox.Text;
            RevealedTextBox.Visibility = Visibility.Collapsed;
            PasswordBox.Visibility = Visibility.Visible;
        }
        
        // Cambiar visibilidad de iconos
        if (EyeOpenIcon != null && EyeClosedIcon != null)
        {
            EyeOpenIcon.Visibility = Visibility.Visible;
            EyeClosedIcon.Visibility = Visibility.Collapsed;
        }
    }
}
