using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.Interfaces.Persistencia;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.State;
using System.Windows;

namespace SistemaAranceles.Presentation.ViewModels.CargosFacultad;

public sealed partial class CargosFacultadViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly SesionActual _sesionActual;
    private bool _bloquearRecargaPorCarrera;

    public CargosFacultadViewModel(IServiceProvider serviceProvider, SesionActual sesionActual)
    {
        _serviceProvider = serviceProvider;
        _sesionActual = sesionActual;
    }

    [ObservableProperty]
    private ObservableCollection<Carrera> _carreras = [];

    [ObservableProperty]
    private int _carreraSeleccionadaId;

    [ObservableProperty]
    private ObservableCollection<CargoFacultadCalculadoDto> _cargos = [];

    [ObservableProperty]
    private CargoFacultadCalculadoDto? _cargoSeleccionado;

    [ObservableProperty]
    private string _nombreCargo = string.Empty;

    [ObservableProperty]
    private string _tipoCargo = "Admin";

    [ObservableProperty]
    private string _sueldoBaseMensual = string.Empty;

    [ObservableProperty]
    private bool _esCargoDocente;

    [ObservableProperty]
    private string _estudiantesUnidadAcademica = "285";

    [ObservableProperty]
    private string _estudiantesCarreraPeriodo = "30";

    [ObservableProperty]
    private string _factorInflacion = "1";

    [ObservableProperty]
    private string _valorDecimoCuartoSemestral = "200";

    [ObservableProperty]
    private bool _estaCargando;

    [ObservableProperty]
    private bool _estaGuardando;

    [ObservableProperty]
    private bool _estaEliminando;

    [ObservableProperty]
    private bool _estaEditando;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private string _mensajeExito = string.Empty;

    public bool PuedeVer => _sesionActual.TienePermiso("AF.VER") || _sesionActual.EsAdministrador;
    public bool PuedeEditar => PuedeVer;
    public bool PuedeEliminar => PuedeVer;

    public string TituloModulo => "Sueldos y Planta Central";

    public string TituloFormulario => EstaEditando ? "Editar cargo de facultad" : "Nuevo cargo de facultad";

    [RelayCommand]
    private async Task CargarAsync()
    {
        if (!PuedeVer)
        {
            MensajeError = "Acceso denegado al módulo de Sueldos y Planta Central.";
            return;
        }

        if (EstaCargando)
            return;

        EstaCargando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var repoCarrera = scope.ServiceProvider.GetRequiredService<IRepositorioCarrera>();
            var carreras = await repoCarrera.ListarAsync();
            Carreras = new ObservableCollection<Carrera>(carreras.OrderBy(x => x.Nombre));

            _bloquearRecargaPorCarrera = true;
            try
            {
                if (Carreras.Count > 0 && (CarreraSeleccionadaId <= 0 || Carreras.All(x => x.Id != CarreraSeleccionadaId)))
                    CarreraSeleccionadaId = Carreras[0].Id;
            }
            finally
            {
                _bloquearRecargaPorCarrera = false;
            }

            await CargarCargosAsync();
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar cargos de facultad: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaCargando = false;
        }
    }

    [RelayCommand]
    private void Nuevo()
    {
        CargoSeleccionado = null;
        EstaEditando = false;
        NombreCargo = string.Empty;
        TipoCargo = "Admin";
        SueldoBaseMensual = string.Empty;
        EsCargoDocente = false;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    [RelayCommand]
    private void SeleccionarParaEditar()
    {
        if (!PuedeEditar)
            return;

        if (CargoSeleccionado is null)
        {
            MensajeError = "Seleccione un cargo para editar.";
            return;
        }

        EstaEditando = true;
        NombreCargo = CargoSeleccionado.NombreCargo;
        TipoCargo = CargoSeleccionado.TipoCargo;
        SueldoBaseMensual = CargoSeleccionado.SueldoBaseMensual.ToString(CultureInfo.InvariantCulture);
        EsCargoDocente = CargoSeleccionado.EsCargoDocente;
        CarreraSeleccionadaId = CargoSeleccionado.CarreraId;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormulario));
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para guardar cargos de facultad.";
            return;
        }

        if (EstaGuardando)
            return;

        if (CarreraSeleccionadaId <= 0)
        {
            MensajeError = "Seleccione una carrera.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NombreCargo))
        {
            MensajeError = "Ingrese el nombre del cargo.";
            return;
        }

        if (string.IsNullOrWhiteSpace(TipoCargo))
        {
            MensajeError = "Ingrese el tipo de cargo.";
            return;
        }

        if (!TryParseDecimalFlexible(SueldoBaseMensual, out var sueldoBaseMensual) || sueldoBaseMensual < 0m)
        {
            MensajeError = "Ingrese un sueldo base válido.";
            return;
        }

        EstaGuardando = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mensajeExito = string.Empty;

            if (EstaEditando && CargoSeleccionado is not null)
            {
                var useCase = scope.ServiceProvider.GetRequiredService<ActualizarCargoFacultadCommand>();
                await useCase.EjecutarAsync(new ActualizarCargoFacultadDto
                {
                    Id = CargoSeleccionado.Id,
                    CarreraId = CarreraSeleccionadaId,
                    NombreCargo = NombreCargo,
                    TipoCargo = TipoCargo,
                    SueldoBaseMensual = sueldoBaseMensual,
                    EsCargoDocente = EsCargoDocente,
                });

                mensajeExito = "Cargo actualizado correctamente.";
            }
            else
            {
                var useCase = scope.ServiceProvider.GetRequiredService<AgregarCargoFacultadCommand>();
                await useCase.EjecutarAsync(new CrearCargoFacultadDto
                {
                    CarreraId = CarreraSeleccionadaId,
                    NombreCargo = NombreCargo,
                    TipoCargo = TipoCargo,
                    SueldoBaseMensual = sueldoBaseMensual,
                    EsCargoDocente = EsCargoDocente,
                });

                mensajeExito = "Cargo registrado correctamente.";
            }

            await CargarCargosAsync();
            Nuevo();
            MensajeExito = mensajeExito;
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al guardar cargo: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaGuardando = false;
        }
    }

    [RelayCommand]
    private async Task EliminarSeleccionadoAsync()
    {
        if (!PuedeEliminar)
        {
            MensajeError = "No tiene permiso para eliminar cargos de facultad.";
            return;
        }

        if (CargoSeleccionado is null)
        {
            MensajeError = "Seleccione un cargo para eliminar.";
            return;
        }

        var respuesta = MessageBox.Show(
            $"¿Eliminar el cargo '{CargoSeleccionado.NombreCargo}'?",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (respuesta != MessageBoxResult.Yes)
            return;

        if (EstaEliminando)
            return;

        EstaEliminando = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<EliminarCargoFacultadCommand>();
            await useCase.EjecutarAsync(CargoSeleccionado.Id);

            await CargarCargosAsync();
            Nuevo();
            MensajeExito = "Cargo eliminado correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al eliminar cargo: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaEliminando = false;
        }
    }

    partial void OnCarreraSeleccionadaIdChanged(int value)
    {
        if (_bloquearRecargaPorCarrera || EstaCargando)
            return;

        _ = CargarCargosAsync();
    }

    private async Task CargarCargosAsync()
    {
        if (!PuedeVer || CarreraSeleccionadaId <= 0)
        {
            Cargos = [];
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ObtenerCargosFacultadPorCarreraQuery>();
            var parametros = ConstruirParametrosCalculo();
            var lista = await useCase.EjecutarAsync(CarreraSeleccionadaId, parametros);
            Cargos = new ObservableCollection<CargoFacultadCalculadoDto>(lista);

            if (CargoSeleccionado is not null && Cargos.All(x => x.Id != CargoSeleccionado.Id))
                CargoSeleccionado = null;
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar lista de cargos: {ObtenerDetalle(ex)}";
        }
    }

    private ParametrosCalculoCargoFacultadDto ConstruirParametrosCalculo()
    {
        if (!TryParseDecimalFlexible(EstudiantesCarreraPeriodo, out var estudiantesCarreraPeriodo))
            throw new InvalidOperationException("Ingrese un valor válido para los estudiantes de carrera del período.");

        if (!TryParseDecimalFlexible(EstudiantesUnidadAcademica, out var estudiantesUnidadAcademica))
            throw new InvalidOperationException("Ingrese un valor válido para los estudiantes de la unidad académica.");

        if (!TryParseDecimalFlexible(FactorInflacion, out var factorInflacion) || factorInflacion <= 0m)
            throw new InvalidOperationException("Ingrese un factor de inflación válido.");

        if (!TryParseDecimalFlexible(ValorDecimoCuartoSemestral, out var valorDecimoCuarto) || valorDecimoCuarto < 0m)
            throw new InvalidOperationException("Ingrese un valor válido para el décimo cuarto semestral.");

        return new ParametrosCalculoCargoFacultadDto
        {
            EstudiantesCarreraPeriodo = estudiantesCarreraPeriodo,
            EstudiantesUnidadAcademica = estudiantesUnidadAcademica,
            FactorInflacion = factorInflacion,
            ValorBaseDecimoCuartoSemestral = valorDecimoCuarto,
        };
    }

    private static bool TryParseDecimalFlexible(string? value, out decimal result)
    {
        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result))
            return true;

        var normalizado = value?.Replace(',', '.');
        return decimal.TryParse(normalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out result)
            || decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out result);
    }

    private static string ObtenerDetalle(Exception ex)
        => ex.InnerException?.Message ?? ex.Message;
}