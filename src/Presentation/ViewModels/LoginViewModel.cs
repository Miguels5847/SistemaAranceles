using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SistemaAranceles.Application.UseCases.Autenticacion;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly LoginUseCase _loginUseCase;
    private readonly SesionActual _sesionActual;

    public LoginViewModel(LoginUseCase loginUseCase, SesionActual sesionActual)
    {
        _loginUseCase = loginUseCase;
        _sesionActual = sesionActual;
    }

    [ObservableProperty]
    private string _correo = string.Empty;

    [ObservableProperty]
    private string _contrasena = string.Empty;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private bool _estaCargando;

    [RelayCommand(CanExecute = nameof(PuedeLogin))]
    private async Task LoginAsync()
    {
        MensajeError = string.Empty;
        EstaCargando = true;

        try
        {
            var sesion = await _loginUseCase.EjecutarAsync(Correo, Contrasena);
            _sesionActual.IniciarSesion(
                sesion.UsuarioId,
                sesion.NombreCompleto,
                sesion.Correo,
                sesion.RolNombre,
                sesion.TokenSesion);

            WeakReferenceMessenger.Default.Send(
                new LoginExitosoMensaje(sesion.UsuarioId, sesion.NombreCompleto, sesion.RolNombre));
        }
        catch (UnauthorizedAccessException ex)
        {
            MensajeError = ex.Message;
        }
        catch (Exception ex)
        {
            MensajeError = $"Error inesperado: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private bool PuedeLogin() =>
        !string.IsNullOrWhiteSpace(Correo) && !string.IsNullOrWhiteSpace(Contrasena) && !EstaCargando;

    partial void OnCorreoChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnContrasenaChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnEstaCargandoChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();
}
