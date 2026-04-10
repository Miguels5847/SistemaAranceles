using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.UseCases.Autenticacion;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;

    public LoginViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
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

    [ObservableProperty]
    private bool _mostrarContrasena;

    [RelayCommand]
    private void AlternarVisibilidad()
    {
        MostrarContrasena = !MostrarContrasena;
    }

    [RelayCommand(CanExecute = nameof(PuedeLogin))]
    private async Task LoginAsync()
    {
        Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginViewModel.LoginAsync iniciado para correo '{Correo}'.");
        MensajeError = string.Empty;
        EstaCargando = true;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var loginUseCase = scope.ServiceProvider.GetRequiredService<LoginUseCase>();
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginViewModel: invocando LoginUseCase.");
            var sesion = await loginUseCase.EjecutarAsync(Correo, Contrasena);
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginViewModel: LoginUseCase exitoso. UsuarioId={sesion.UsuarioId}, Rol={sesion.RolNombre}.");
            _sesionActual.IniciarSesion(
                sesion.UsuarioId,
                sesion.NombreCompleto,
                sesion.Correo,
                sesion.RolNombre,
                sesion.TokenSesion);
            _sesionActual.EstablecerPermisos(sesion.PermisosEfectivos);

            WeakReferenceMessenger.Default.Send(
                new LoginExitosoMensaje(sesion.UsuarioId, sesion.NombreCompleto, sesion.RolNombre));
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginViewModel: mensaje LoginExitoso enviado.");
        }
        catch (UnauthorizedAccessException ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginViewModel: UnauthorizedAccessException -> {ex.Message}");
            MensajeError = ex.Message;
        }
        catch (TimeoutException ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginViewModel: TimeoutException -> {ex.Message}");
            MensajeError = "La conexión con Supabase está lenta. Intente nuevamente en unos segundos.";
        }
        catch (OperationCanceledException ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginViewModel: OperationCanceledException -> {ex.Message}");
            MensajeError = "La operación fue cancelada por demora en la conexión. Intente nuevamente.";
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginViewModel: Exception -> {ex}");
            MensajeError = $"Error inesperado: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
            Trace.WriteLine($"[{DateTime.UtcNow:O}] LoginViewModel.LoginAsync finalizado.");
        }
    }

    private bool PuedeLogin() =>
        !string.IsNullOrWhiteSpace(Correo) && !string.IsNullOrWhiteSpace(Contrasena) && !EstaCargando;

    partial void OnCorreoChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnContrasenaChanged(string value) => LoginCommand.NotifyCanExecuteChanged();
    partial void OnEstaCargandoChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();
}
