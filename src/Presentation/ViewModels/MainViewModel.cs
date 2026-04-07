using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
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
    private readonly SesionActual _sesionActual;
    private readonly CerrarSesionUseCase _cerrarSesionUseCase;
    private readonly UsuariosViewModel _usuariosViewModel;
    private readonly Func<EditarUsuarioViewModel> _editarUsuarioViewModelFactory;
    private int _cerrandoSesion;

    public MainViewModel(
        SesionActual sesionActual,
        CerrarSesionUseCase cerrarSesionUseCase,
        UsuariosViewModel usuariosViewModel,
        Func<EditarUsuarioViewModel> editarUsuarioViewModelFactory)
    {
        _sesionActual = sesionActual;
        _cerrarSesionUseCase = cerrarSesionUseCase;
        _usuariosViewModel = usuariosViewModel;
        _editarUsuarioViewModelFactory = editarUsuarioViewModelFactory;

        WeakReferenceMessenger.Default.Register<NavegarAMensaje>(this, (_, msg) =>
        {
            if (msg.DestinoPagina == "Usuarios")
                _ = MostrarUsuariosAsync();
        });

        ConstruirMenu();
        PaginaActual = _usuariosViewModel;
    }

    [ObservableProperty] private string _bienvenida = string.Empty;
    [ObservableProperty] private ObservableObject? _paginaActual;
    [ObservableProperty] private string _mensajePagina = string.Empty;

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
                Comando = new AsyncRelayCommand(MostrarUsuariosAsync)
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

    private async Task MostrarUsuariosAsync()
    {
        MensajePagina = string.Empty;
        PaginaActual = _usuariosViewModel;
        await _usuariosViewModel.CargarCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task MostrarNuevoUsuarioAsync()
    {
        var vm = _editarUsuarioViewModelFactory();
        await vm.InicializarAsync();
        PaginaActual = vm;
    }

    private async Task CerrarSesionAsync()
    {
        if (Interlocked.Exchange(ref _cerrandoSesion, 1) == 1)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] MainViewModel: CerrarSesionAsync ignorado por ejecución en curso.");
            return;
        }

        Trace.WriteLine($"[{DateTime.UtcNow:O}] MainViewModel: inicio CerrarSesionAsync.");
        try
        {
            await _cerrarSesionUseCase.EjecutarAsync(
                _sesionActual.UsuarioId,
                _sesionActual.TokenSesion);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[{DateTime.UtcNow:O}] MainViewModel: error en CerrarSesionAsync -> {ex.Message}.");
        }
        finally
        {
            _sesionActual.CerrarSesion();
            WeakReferenceMessenger.Default.UnregisterAll(this);
            WeakReferenceMessenger.Default.Send(new CerrarSesionMensaje());
            Trace.WriteLine($"[{DateTime.UtcNow:O}] MainViewModel: fin CerrarSesionAsync.");
            Interlocked.Exchange(ref _cerrandoSesion, 0);
        }
    }
}
