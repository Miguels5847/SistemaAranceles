using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using System.Windows;

namespace SistemaAranceles.Presentation.ViewModels.CargosFacultad;

public sealed partial class CargosFacultadViewModel
{
    [ObservableProperty]
    private ObservableCollection<ProyeccionCargoFacultadDto> _proyecciones = [];

    [ObservableProperty]
    private ProyeccionCargoFacultadDto? _proyeccionSeleccionada;

    [ObservableProperty]
    private string _periodoAcademicoId = "1";

    [ObservableProperty]
    private string _cantidadPersonas = "1";

    [ObservableProperty]
    private bool _estaEditandoProyeccion;

    [ObservableProperty]
    private bool _estaGuardandoProyeccion;

    [ObservableProperty]
    private bool _estaEliminandoProyeccion;

    public string TituloFormularioProyeccion => EstaEditandoProyeccion ? "Editar proyección de cargo" : "Nueva proyección de cargo";

    public decimal TotalCostoSemestralProyectado => Proyecciones.Sum(x => x.CostoTotalSemestre);

    public decimal TotalPersonasProyectadas => Proyecciones.Sum(x => x.CantidadPersonas);

    partial void OnProyeccionesChanged(ObservableCollection<ProyeccionCargoFacultadDto> value)
    {
        _ = value;
        OnPropertyChanged(nameof(TotalCostoSemestralProyectado));
        OnPropertyChanged(nameof(TotalPersonasProyectadas));
    }

    partial void OnProyeccionSeleccionadaChanged(ProyeccionCargoFacultadDto? value)
    {
        if (value is null)
            return;

        var cargo = Cargos.FirstOrDefault(x => x.Id == value.CargoFacultadId);
        if (cargo is not null)
            CargoSeleccionado = cargo;

        PeriodoAcademicoId = value.PeriodoAcademicoId.ToString(CultureInfo.InvariantCulture);
        CantidadPersonas = value.CantidadPersonas.ToString(CultureInfo.InvariantCulture);
    }

    [RelayCommand]
    private void NuevoProyeccion()
    {
        ProyeccionSeleccionada = null;
        EstaEditandoProyeccion = false;
        PeriodoAcademicoId = "1";
        CantidadPersonas = "1";
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormularioProyeccion));
    }

    [RelayCommand]
    private void SeleccionarParaEditarProyeccion()
    {
        if (!PuedeEditar)
            return;

        if (ProyeccionSeleccionada is null)
        {
            MensajeError = "Seleccione una proyección para editar.";
            return;
        }

        EstaEditandoProyeccion = true;
        CargoSeleccionado = Cargos.FirstOrDefault(x => x.Id == ProyeccionSeleccionada.CargoFacultadId);
        PeriodoAcademicoId = ProyeccionSeleccionada.PeriodoAcademicoId.ToString(CultureInfo.InvariantCulture);
        CantidadPersonas = ProyeccionSeleccionada.CantidadPersonas.ToString(CultureInfo.InvariantCulture);
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        OnPropertyChanged(nameof(TituloFormularioProyeccion));
    }

    [RelayCommand]
    private async Task GuardarProyeccionAsync()
    {
        if (!PuedeEditar)
        {
            MensajeError = "No tiene permiso para guardar proyecciones de cargos.";
            return;
        }

        if (EstaGuardandoProyeccion)
            return;

        if (CargoSeleccionado is null)
        {
            MensajeError = "Seleccione un cargo.";
            return;
        }

        if (!int.TryParse(PeriodoAcademicoId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var periodoAcademicoId) || periodoAcademicoId <= 0)
        {
            MensajeError = "Ingrese un período académico válido.";
            return;
        }

        if (!TryParseDecimalFlexible(CantidadPersonas, out var cantidadPersonas) || cantidadPersonas <= 0m)
        {
            MensajeError = "Ingrese una cantidad de personas válida.";
            return;
        }

        EstaGuardandoProyeccion = true;
        MensajeError = string.Empty;
        MensajeExito = string.Empty;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<GuardarProyeccionCargoFacultadCommand>();
            var parametros = ConstruirParametrosCalculo();

            await useCase.EjecutarAsync(new GuardarProyeccionCargoFacultadDto
            {
                Id = ProyeccionSeleccionada?.Id,
                CargoFacultadId = CargoSeleccionado.Id,
                PeriodoAcademicoId = periodoAcademicoId,
                CantidadPersonas = cantidadPersonas,
                EstudiantesCarreraPeriodo = parametros.EstudiantesCarreraPeriodo,
                EstudiantesUnidadAcademica = parametros.EstudiantesUnidadAcademica,
                FactorInflacion = parametros.FactorInflacion,
                ValorBaseDecimoCuartoSemestral = parametros.ValorBaseDecimoCuartoSemestral,
            });

            await CargarProyeccionesAsync();
            NuevoProyeccion();
            MensajeExito = "Proyección guardada correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al guardar proyección: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaGuardandoProyeccion = false;
        }
    }

    [RelayCommand]
    private async Task EliminarProyeccionSeleccionadaAsync()
    {
        if (!PuedeEliminar)
        {
            MensajeError = "No tiene permiso para eliminar proyecciones de cargos.";
            return;
        }

        if (ProyeccionSeleccionada is null)
        {
            MensajeError = "Seleccione una proyección para eliminar.";
            return;
        }

        var respuesta = MessageBox.Show(
            $"¿Eliminar la proyección del cargo '{ProyeccionSeleccionada.NombreCargo}'?",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (respuesta != MessageBoxResult.Yes)
            return;

        if (EstaEliminandoProyeccion)
            return;

        EstaEliminandoProyeccion = true;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<EliminarProyeccionCargoFacultadCommand>();
            await useCase.EjecutarAsync(ProyeccionSeleccionada.Id);

            await CargarProyeccionesAsync();
            NuevoProyeccion();
            MensajeExito = "Proyección eliminada correctamente.";
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al eliminar proyección: {ObtenerDetalle(ex)}";
        }
        finally
        {
            EstaEliminandoProyeccion = false;
        }
    }

    private async Task CargarProyeccionesAsync()
    {
        if (!PuedeVer || CarreraSeleccionadaId <= 0)
        {
            Proyecciones = [];
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ListarProyeccionesCargoFacultadPorCarreraQuery>();
            var parametros = ConstruirParametrosCalculo();
            var lista = await useCase.EjecutarAsync(CarreraSeleccionadaId, parametros);
            Proyecciones = new ObservableCollection<ProyeccionCargoFacultadDto>(lista);

            if (ProyeccionSeleccionada is not null && Proyecciones.All(x => x.Id != ProyeccionSeleccionada.Id))
                ProyeccionSeleccionada = null;
        }
        catch (Exception ex)
        {
            MensajeError = $"Error al cargar proyecciones: {ObtenerDetalle(ex)}";
            Proyecciones = [];
        }
    }
}