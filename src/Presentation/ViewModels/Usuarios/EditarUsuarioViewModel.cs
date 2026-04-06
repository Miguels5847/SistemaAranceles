using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using SistemaAranceles.Application.DTOs.Roles;
using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.Usuarios;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.Usuarios;

public sealed partial class EditarUsuarioViewModel : ObservableObject
{
    private readonly CrearUsuarioUseCase _crearUseCase;
    private readonly ActualizarUsuarioUseCase _actualizarUseCase;
    private readonly IRepositorioRol _repositorioRol;
    private readonly SesionActual _sesionActual;

    private int _usuarioId;

    public EditarUsuarioViewModel(
        CrearUsuarioUseCase crearUseCase,
        ActualizarUsuarioUseCase actualizarUseCase,
        IRepositorioRol repositorioRol,
        SesionActual sesionActual)
    {
        _crearUseCase = crearUseCase;
        _actualizarUseCase = actualizarUseCase;
        _repositorioRol = repositorioRol;
        _sesionActual = sesionActual;
    }

    [ObservableProperty] private string _titulo = "Nuevo Usuario";
    [ObservableProperty] private string _nombreCompleto = string.Empty;
    [ObservableProperty] private string _correoInstitucional = string.Empty;
    [ObservableProperty] private string _contrasena = string.Empty;
    [ObservableProperty] private string _rolSeleccionado = string.Empty;
    [ObservableProperty] private string _estadoSeleccionado = "Activo";
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _esNuevo = true;

    public ObservableCollection<RolDto> Roles { get; } = [];
    public ObservableCollection<string> Estados { get; } = ["Activo", "Suspendido", "Inactivo"];

    public async Task InicializarAsync(UsuarioDto? usuarioExistente = null)
    {
        var roles = await _repositorioRol.ListarAsync();
        Roles.Clear();
        foreach (var r in roles)
            Roles.Add(new RolDto { Id = r.Id, Nombre = r.Nombre, Descripcion = r.Descripcion });

        if (usuarioExistente is not null)
        {
            _usuarioId = usuarioExistente.Id;
            EsNuevo = false;
            Titulo = "Editar Usuario";
            NombreCompleto = usuarioExistente.NombreCompleto;
            CorreoInstitucional = usuarioExistente.CorreoInstitucional;
            EstadoSeleccionado = usuarioExistente.Estado;
            RolSeleccionado = usuarioExistente.Roles.FirstOrDefault() ?? string.Empty;
        }
        else
        {
            EsNuevo = true;
            Titulo = "Nuevo Usuario";
            RolSeleccionado = Roles.FirstOrDefault()?.Nombre ?? string.Empty;
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (string.IsNullOrWhiteSpace(NombreCompleto) || string.IsNullOrWhiteSpace(CorreoInstitucional))
        {
            MensajeError = "Nombre y correo son obligatorios.";
            return;
        }

        EstaCargando = true;
        try
        {
            if (EsNuevo)
            {
                if (string.IsNullOrWhiteSpace(Contrasena) || Contrasena.Length < 8)
                {
                    MensajeError = "La contraseña debe tener al menos 8 caracteres.";
                    return;
                }

                await _crearUseCase.EjecutarAsync(new CrearUsuarioDto
                {
                    NombreCompleto = NombreCompleto,
                    CorreoInstitucional = CorreoInstitucional,
                    Contrasena = Contrasena,
                    RolNombre = RolSeleccionado
                }, _sesionActual.UsuarioId);

                MensajeExito = "Usuario creado correctamente.";
            }
            else
            {
                await _actualizarUseCase.EjecutarAsync(new ActualizarUsuarioDto
                {
                    Id = _usuarioId,
                    NombreCompleto = NombreCompleto,
                    CorreoInstitucional = CorreoInstitucional,
                    NuevaContrasena = string.IsNullOrWhiteSpace(Contrasena) ? null : Contrasena,
                    RolNombre = RolSeleccionado,
                    Estado = EstadoSeleccionado
                }, _sesionActual.UsuarioId);

                MensajeExito = "Usuario actualizado correctamente.";
            }

            WeakReferenceMessenger.Default.Send(new NavegarAMensaje("Usuarios"));
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
        finally
        {
            EstaCargando = false;
        }
    }
}
