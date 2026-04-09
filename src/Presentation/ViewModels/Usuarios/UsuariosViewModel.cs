using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.UseCases.Usuarios;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.Usuarios;

public sealed partial class UsuariosViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;

    public UsuariosViewModel(
        IServiceProvider serviceProvider,
        SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty]
    private ObservableCollection<UsuarioDto> _usuarios = [];

    [ObservableProperty]
    private UsuarioDto? _usuarioSeleccionado;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    [ObservableProperty]
    private bool _estaCargando;

    [ObservableProperty]
    private bool _isEliminando;

    public bool PuedeGestionar => _sesionActual.EsAdministrador;

    public string TextoEliminarSeleccionado => UsuarioSeleccionado is null
        ? "Eliminar"
        : ObtenerTextoEliminarSeleccionado();

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando)
            return;

        MensajeError = string.Empty;
        EstaCargando = true;
        try
        {
            // Reintentos silenciosos: hasta 3 intentos antes de mostrar error
            Exception? ultimoError = null;
            for (var intento = 1; intento <= 3; intento++)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var listarUseCase = scope.ServiceProvider.GetRequiredService<ListarUsuariosUseCase>();
                    var lista = await listarUseCase.EjecutarAsync();
                    Usuarios = new ObservableCollection<UsuarioDto>(lista);
                    return; // Éxito, salir sin mostrar error
                }
                catch (Exception ex) when (intento < 3 && EsErrorTransitorio(ex))
                {
                    ultimoError = ex;
                    await Task.Delay(250 * intento); // Backoff progresivo: 250ms, 500ms
                }
            }

            // Si llegamos aquí, los 3 intentos fallaron
            if (ultimoError != null)
                MensajeError = $"Error al cargar usuarios: {ultimoError.Message}";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar usuarios: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private void Editar()
    {
        if (!PuedeGestionar)
            return;

        if (UsuarioSeleccionado is null)
        {
            MensajeError = "Seleccione un usuario para editar.";
            return;
        }

        WeakReferenceMessenger.Default.Send(new EditarUsuarioMensaje(UsuarioSeleccionado));
    }

    [RelayCommand]
    private async Task EliminarAsync()
    {
        if (!PuedeGestionar)
            return;

        if (UsuarioSeleccionado is null)
        {
            MensajeError = "Seleccione un usuario para eliminar.";
            return;
        }

        if (UsuarioSeleccionado.Id == _sesionActual.UsuarioId)
        {
            MensajeError = "No puede eliminarse a sí mismo.";
            return;
        }

        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        IsEliminando = true;

        try
        {
            var nombreUsuario = UsuarioSeleccionado.NombreCompleto;
            var estadoUsuario = UsuarioSeleccionado.Estado;
            var eliminadoExitoso = false;
            for (var intento = 1; intento <= 2; intento++)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var eliminarUseCase = scope.ServiceProvider.GetRequiredService<EliminarUsuarioUseCase>();
                    await eliminarUseCase.EjecutarAsync(UsuarioSeleccionado.Id, _sesionActual.UsuarioId);
                    eliminadoExitoso = true;
                    break;
                }
                catch (Exception ex) when (intento == 1 && EsErrorTransitorio(ex))
                {
                    await Task.Delay(350);
                }
            }

            if (!eliminadoExitoso)
            {
                throw new InvalidOperationException("No se pudo eliminar por una falla transitoria de conexión. Intente nuevamente.");
            }

            MensajeExito = string.Equals(estadoUsuario, "Inactivo", StringComparison.OrdinalIgnoreCase)
                ? $"Usuario '{nombreUsuario}' eliminado definitivamente correctamente."
                : $"Usuario '{nombreUsuario}' desactivado correctamente.";
            UsuarioSeleccionado = null;
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al eliminar: {ex.Message}";
        }
        finally
        {
            IsEliminando = false;
        }

        await CargarAsync();
    }

    partial void OnUsuarioSeleccionadoChanged(UsuarioDto? value)
    {
        MensajeError = string.Empty;
        OnPropertyChanged(nameof(TextoEliminarSeleccionado));
    }

    private string ObtenerTextoEliminarSeleccionado()
    {
        if (UsuarioSeleccionado is null)
            return "Eliminar";

        return string.Equals(UsuarioSeleccionado.Estado, "Inactivo", StringComparison.OrdinalIgnoreCase)
            ? "Eliminar definitivamente"
            : "Inactivar";
    }

    private static bool EsErrorTransitorio(Exception ex)
    {
        if (ex is TimeoutException || ex is OperationCanceledException)
            return true;

        // Revisar mensaje de la excepción actual
        var mensaje = ex.Message.ToLowerInvariant();
        if (mensaje.Contains("stream", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("transient", StringComparison.OrdinalIgnoreCase)
            || mensaje.Contains("likely due to a transient failure", StringComparison.OrdinalIgnoreCase))
            return true;

        // Revisar InnerException (EF wrappea errors)
        if (ex.InnerException != null)
        {
            var innerMensaje = ex.InnerException.Message.ToLowerInvariant();
            if (innerMensaje.Contains("stream", StringComparison.OrdinalIgnoreCase)
                || innerMensaje.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                || innerMensaje.Contains("transient", StringComparison.OrdinalIgnoreCase)
                || innerMensaje.Contains("pooling", StringComparison.OrdinalIgnoreCase)
                || innerMensaje.Contains("connection", StringComparison.OrdinalIgnoreCase)
                || ex.InnerException is TimeoutException
                || ex.InnerException is OperationCanceledException)
                return true;
        }

        return false;
    }
}
