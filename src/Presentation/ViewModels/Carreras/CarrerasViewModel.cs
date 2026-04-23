using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.Interfaces.Servicios;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;

namespace SistemaAranceles.Presentation.ViewModels.Carreras;

public sealed partial class CarreraItemViewModel : ObservableObject
{
    [ObservableProperty] private int _id;
    [ObservableProperty] private string _codigo = string.Empty;
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _facultadNombre = string.Empty;
    [ObservableProperty] private int _totalCiclos;
}

public sealed partial class CarrerasViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;

    public CarrerasViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty] private ObservableCollection<CarreraItemViewModel> _carreras = [];
    [ObservableProperty] private CarreraItemViewModel? _carreraSeleccionada;

    [ObservableProperty] private string _codigo = string.Empty;
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _facultadNombre = string.Empty;
    [ObservableProperty] private string _totalCiclos = "9";

    [ObservableProperty] private bool _estaEditando;
    [ObservableProperty] private bool _estaCargando;
    [ObservableProperty] private bool _estaGuardando;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    public bool PuedeVer => _sesionActual.TienePermiso("CA.VER") || _sesionActual.EsAdministrador;
    public bool PuedeEditar => _sesionActual.TienePermiso("CA.CREAR") || _sesionActual.TienePermiso("CA.EDITAR") || _sesionActual.EsAdministrador;

    public string TituloFormulario => EstaEditando ? "Editar carrera" : "Nueva carrera";

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al módulo de Carreras.";
            return;
        }

        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var lista = await repo.ListarAsync();

            Carreras = new ObservableCollection<CarreraItemViewModel>(
                lista.OrderBy(x => x.Codigo)
                    .Select(x => new CarreraItemViewModel
                    {
                        Id = x.Id,
                        Codigo = x.Codigo,
                        Nombre = x.Nombre,
                        FacultadNombre = x.FacultadNombre,
                        TotalCiclos = x.TotalCiclos
                    }));
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar carreras: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private void Nuevo()
    {
        CarreraSeleccionada = null;
        EstaEditando = false;
        Codigo = string.Empty;
        Nombre = string.Empty;
        FacultadNombre = string.Empty;
        TotalCiclos = "9";
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    [RelayCommand]
    private void SeleccionarParaEditar()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para editar carreras.";
            return;
        }

        if (CarreraSeleccionada is null)
        {
            MensajeError = "Seleccione una carrera para editar.";
            return;
        }

        EstaEditando = true;
        Codigo = CarreraSeleccionada.Codigo;
        Nombre = CarreraSeleccionada.Nombre;
        FacultadNombre = CarreraSeleccionada.FacultadNombre;
        TotalCiclos = CarreraSeleccionada.TotalCiclos.ToString();
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para guardar carreras.";
            return;
        }

        if (EstaGuardando) return;

        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        if (string.IsNullOrWhiteSpace(Codigo))
        {
            MensajeError = "Ingrese el código de carrera.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Nombre))
        {
            MensajeError = "Ingrese el nombre de la carrera.";
            return;
        }

        if (string.IsNullOrWhiteSpace(FacultadNombre))
        {
            MensajeError = "Ingrese la facultad.";
            return;
        }

        if (!int.TryParse(TotalCiclos, out var ciclos) || ciclos <= 0)
        {
            MensajeError = "Total de ciclos inválido.";
            return;
        }

        EstaGuardando = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var unidadTrabajo = scope.ServiceProvider.GetRequiredService<IUnidadTrabajo>();

            if (EstaEditando && CarreraSeleccionada is not null)
            {
                var entidad = await repo.ObtenerPorIdAsync(CarreraSeleccionada.Id)
                    ?? throw new InvalidOperationException("No se encontró la carrera a editar.");

                entidad.CambiarCodigo(Codigo);
                entidad.CambiarNombre(Nombre);
                entidad.CambiarFacultad(FacultadNombre);
                entidad.CambiarTotalCiclos(ciclos);
                await repo.ActualizarAsync(entidad);
                await unidadTrabajo.GuardarCambiosAsync();
                MensajeExito = "Carrera actualizada correctamente.";
            }
            else
            {
                var existe = await repo.ExisteCodigoAsync(Codigo);
                if (existe)
                    throw new InvalidOperationException("Ya existe una carrera con ese código.");

                var nueva = new Carrera(Codigo, Nombre, FacultadNombre, ciclos);
                await repo.AgregarAsync(nueva);
                await unidadTrabajo.GuardarCambiosAsync();
                MensajeExito = "Carrera creada correctamente.";
            }

            await CargarAsync();
            Nuevo();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al guardar carrera: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    private static string ObtenerDetalle(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;
}
