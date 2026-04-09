using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.Usuarios;
using SistemaAranceles.Application.UseCases.Autenticacion;
using SistemaAranceles.Presentation.Mensajes;
using SistemaAranceles.Presentation.State;
using SistemaAranceles.Presentation.ViewModels.Usuarios;
using System.Diagnostics;

namespace SistemaAranceles.Presentation.ViewModels;

public sealed class ItemMenu
{
    public string Titulo { get; init; } = string.Empty;
    public string Icono { get; init; } = string.Empty;
    public System.Windows.Input.ICommand? Comando { get; init; }
    public bool EsVisible { get; init; } = true;
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private readonly UsuariosViewModel _usuariosViewModel;
    private readonly Func<EditarUsuarioViewModel> _editarUsuarioViewModelFactory;
    private int _cerrandoSesion;
    private int _cargandoUsuarios;

    public MainViewModel(
        IServiceProvider serviceProvider,
        SesionActual sesionActual,
        UsuariosViewModel usuariosViewModel,
        Func<EditarUsuarioViewModel> editarUsuarioViewModelFactory)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
        _usuariosViewModel = usuariosViewModel;
        _editarUsuarioViewModelFactory = editarUsuarioViewModelFactory;

        WeakReferenceMessenger.Default.Register<NavegarAMensaje>(this, (_, msg) =>
        {
            if (msg.DestinoPagina == "Usuarios")
                LanzarSinEsperar(MostrarUsuariosAsync(msg.MensajeExito));
        });

        WeakReferenceMessenger.Default.Register<EditarUsuarioMensaje>(this, (_, msg) =>
        {
            LanzarSinEsperar(MostrarEditarUsuarioAsync(msg.Usuario));
        });

        ConstruirMenu();
        if (_sesionActual.EsAdministrador)
        {
            PaginaActual = _usuariosViewModel;
            _ = MostrarUsuariosAsync();
        }
        else
        {
            PaginaActual = null;
            MensajePagina = "Bienvenido. Tu perfil no tiene acceso a Gestión de Usuarios.";
        }
    }

    [ObservableProperty] private string _bienvenida = string.Empty;
    [ObservableProperty] private ObservableObject? _paginaActual;
    [ObservableProperty] private string _mensajePagina = string.Empty;
    [ObservableProperty] private bool _estaCerrandoSesion;
    [ObservableProperty] private string _mensajeCierreSesion = "Cerrando sesión...";

    public ObservableCollection<ItemMenu> MenuItems { get; } = [];

    public string NombreUsuario => _sesionActual.NombreCompleto;
    public string RolUsuario => _sesionActual.RolNombre;

    private void ConstruirMenu()
    {
        MenuItems.Clear();

        if (_sesionActual.EsAdministrador)
        {
            MenuItems.Add(new ItemMenu
            {
                Titulo = "Usuarios",
                Icono = "👤",
                Comando = new AsyncRelayCommand(() => MostrarUsuariosAsync())
            });
        }

        MenuItems.Add(new ItemMenu
        {
            Titulo = "Carreras",
            Icono = "🎓",
            Comando = new RelayCommand(() => MensajePagina = "Módulo Carreras — disponible en Épica 3")
        });

        MenuItems.Add(new ItemMenu
        {
            Titulo = "Inflación",
            Icono = "📈",
            Comando = new RelayCommand(() => MensajePagina = "Módulo Inflación — disponible en Épica 3")
        });

        MenuItems.Add(new ItemMenu
        {
            Titulo = "Proyecciones",
            Icono = "📊",
            Comando = new RelayCommand(() => MensajePagina = "Módulo Proyecciones — disponible en Épica 4")
        });

        MenuItems.Add(new ItemMenu
        {
            Titulo = "Análisis Financiero",
            Icono = "💰",
            Comando = new RelayCommand(() => MensajePagina = "Módulo Análisis Financiero — disponible en Épica 5")
        });

        MenuItems.Add(new ItemMenu
        {
            Titulo = "Cerrar Sesión",
            Icono = "🚪",
            Comando = new RelayCommand(() => _ = CerrarSesionAsync())
        });

        Bienvenida = $"Bienvenido, {_sesionActual.NombreCompleto}  |  Rol: {_sesionActual.RolNombre}";
    }

    private async Task MostrarUsuariosAsync(string? mensajeExito = null)
    {
        if (!_sesionActual.EsAdministrador)
        {
            MensajePagina = "Acceso denegado a Gestión de Usuarios.";
            return;
        }

        if (Interlocked.Exchange(ref _cargandoUsuarios, 1) == 1)
        {
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] MainViewModel: MostrarUsuariosAsync ignorado por carga en curso.");
            return;
        }

        MensajePagina = string.Empty;
        try
        {
            PaginaActual = _usuariosViewModel;
            await _usuariosViewModel.CargarCommand.ExecuteAsync(null);

            if (!string.IsNullOrWhiteSpace(mensajeExito))
            {
                _usuariosViewModel.MensajeExito = mensajeExito;
            }
        }
        finally
        {
            Interlocked.Exchange(ref _cargandoUsuarios, 0);
        }
    }

    [RelayCommand]
    private async Task MostrarNuevoUsuarioAsync()
    {
        if (!_sesionActual.EsAdministrador)
        {
            MensajePagina = "Acceso denegado a Gestión de Usuarios.";
            return;
        }

        var vm = _editarUsuarioViewModelFactory();
        PaginaActual = vm;
        MensajePagina = string.Empty;

        try
        {
            await vm.InicializarAsync();
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[{DateTime.UtcNow:O}] MainViewModel: error al inicializar nuevo usuario -> {ex.Message}.");
            vm.MensajeError = "No se pudo inicializar el formulario de usuario. Intente nuevamente.";
        }
    }

    private async Task MostrarEditarUsuarioAsync(UsuarioDto usuario)
    {
        if (!_sesionActual.EsAdministrador)
        {
            MensajePagina = "Acceso denegado a Gestión de Usuarios.";
            return;
        }

        var vm = _editarUsuarioViewModelFactory();
        PaginaActual = vm;
        MensajePagina = string.Empty;

        try
        {
            // Mostrar primero la vista para que el overlay "Cargando datos" sea visible durante la inicialización.
            await vm.InicializarAsync(usuario);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[{DateTime.UtcNow:O}] MainViewModel: error al inicializar edición de usuario -> {ex.Message}.");
            vm.MensajeError = "No se pudo cargar los datos del usuario. Intente nuevamente.";
        }
    }

    private async Task CerrarSesionAsync()
    {
        if (Interlocked.Exchange(ref _cerrandoSesion, 1) == 1)
        {
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] MainViewModel: CerrarSesionAsync ignorado por ejecución en curso.");
            return;
        }

        EstaCerrandoSesion = true;
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] MainViewModel: inicio CerrarSesionAsync.");
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cerrarSesionUseCase = scope.ServiceProvider.GetRequiredService<CerrarSesionUseCase>();
            await cerrarSesionUseCase.EjecutarAsync(
                _sesionActual.UsuarioId,
                _sesionActual.TokenSesion);
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[{DateTime.UtcNow:O}] MainViewModel: error en CerrarSesionAsync -> {ex.Message}.");
        }
        finally
        {
            _sesionActual.CerrarSesion();
            WeakReferenceMessenger.Default.UnregisterAll(this);
            WeakReferenceMessenger.Default.Send(new CerrarSesionMensaje());
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] MainViewModel: fin CerrarSesionAsync.");
            EstaCerrandoSesion = false;
            Interlocked.Exchange(ref _cerrandoSesion, 0);
        }
    }

    private static void LanzarSinEsperar(Task tarea)
    {
        _ = tarea.ContinueWith(
            antecedente => Trace.TraceError($"[{DateTime.UtcNow:O}] MainViewModel: tarea en segundo plano falló -> {antecedente.Exception?.GetBaseException().Message}"),
            TaskContinuationOptions.OnlyOnFaulted);
    }
}
