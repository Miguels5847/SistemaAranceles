using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.UseCases.Usuarios;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.Usuarios;

public sealed partial class UsuariosViewModel : ObservableObject
{
    private readonly ListarUsuariosUseCase _listarUseCase;
    private readonly EliminarUsuarioUseCase _eliminarUseCase;
    private readonly SesionActual _sesionActual;

    public UsuariosViewModel(
        ListarUsuariosUseCase listarUseCase,
        EliminarUsuarioUseCase eliminarUseCase,
        SesionActual sesionActual)
    {
        _listarUseCase = listarUseCase;
        _eliminarUseCase = eliminarUseCase;
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

    public bool PuedeGestionar => _sesionActual.EsAdministrador;

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (EstaCargando)
            return;

        MensajeError = string.Empty;
        EstaCargando = true;
        try
        {
            var lista = await _listarUseCase.EjecutarAsync();
            Usuarios = new ObservableCollection<UsuarioDto>(lista);
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

    [RelayCommand(CanExecute = nameof(HayUsuarioSeleccionado))]
    private void Editar()
    {
        if (UsuarioSeleccionado is null || !PuedeGestionar)
            return;

        WeakReferenceMessenger.Default.Send(new EditarUsuarioMensaje(UsuarioSeleccionado));
    }

    [RelayCommand(CanExecute = nameof(HayUsuarioSeleccionado))]
    private async Task EliminarAsync()
    {
        if (UsuarioSeleccionado is null) return;

        if (UsuarioSeleccionado.Id == _sesionActual.UsuarioId)
        {
            MensajeError = "No puede eliminarse a sí mismo.";
            return;
        }

        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        EstaCargando = true;

        try
        {
            await _eliminarUseCase.EjecutarAsync(UsuarioSeleccionado.Id, _sesionActual.UsuarioId);
            MensajeExito = $"Usuario '{UsuarioSeleccionado.NombreCompleto}' eliminado correctamente.";
            await CargarAsync();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al eliminar: {ex.Message}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private bool HayUsuarioSeleccionado() => UsuarioSeleccionado is not null && PuedeGestionar;

    partial void OnUsuarioSeleccionadoChanged(UsuarioDto? value)
    {
        EditarCommand.NotifyCanExecuteChanged();
        EliminarCommand.NotifyCanExecuteChanged();
    }
}
