using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Application.DTOs.CargosFacultad;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Presentation.ViewModels.Catalogos;

public sealed partial class CatalogoCargosViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    public CatalogoCargosViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [ObservableProperty] private ObservableCollection<CargoFacultad> _cargos = [];
    [ObservableProperty] private CargoFacultad? _cargoSeleccionado;
    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private bool _estaCargando;

    // --- Formulario CRUD ---
    [ObservableProperty] private bool _formVisible;
    [ObservableProperty] private bool _esEdicion;
    [ObservableProperty] private int _formCargoId;
    [ObservableProperty] private string _formNombreCargo = string.Empty;
    [ObservableProperty] private string _formTipoCargo = string.Empty;
    [ObservableProperty] private decimal _formSueldo;
    [ObservableProperty] private decimal _formCantidad = 1m;
    [ObservableProperty] private bool _formEsDocente;

    [RelayCommand]
    private async Task CargarAsync(int? carreraId = null)
    {
        if (EstaCargando) return;
        EstaCargando = true;
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var useCase = scope.ServiceProvider.GetRequiredService<ObtenerCargosFacultadPorCarreraQuery>();
            var parametros = new ParametrosCalculoCargoFacultadDto
            {
                EstudiantesCarreraPeriodo = 0,
                EstudiantesUnidadAcademica = 0,
                FactorInflacion = 1m,
                ValorBaseDecimoCuartoSemestral = 0m
            };
            var lista = await useCase.EjecutarAsync(carreraId ?? 0, parametros);
            var preview = lista.Select(x => new CargoFacultad(
                x.CarreraId, x.NombreCargo, x.TipoCargo,
                x.SueldoBaseMensual, x.EsCargoDocente, x.CantidadDefault)).ToList();
            foreach (var (c, dto) in preview.Zip(lista))
                c.RehidratarId(dto.Id);
            Cargos = new ObservableCollection<CargoFacultad>(preview);
        }
        catch (Exception ex) { MensajeError = ex.Message; }
        finally { EstaCargando = false; }
    }

    [RelayCommand]
    private void AbrirNuevo()
    {
        FormCargoId = 0;
        FormNombreCargo = string.Empty;
        FormTipoCargo = string.Empty;
        FormSueldo = 0m;
        FormCantidad = 1m;
        FormEsDocente = false;
        EsEdicion = false;
        FormVisible = true;
    }

    [RelayCommand]
    private void AbrirEditar(CargoFacultad cargo)
    {
        FormCargoId = cargo.Id;
        FormNombreCargo = cargo.NombreCargo;
        FormTipoCargo = cargo.TipoCargo;
        FormSueldo = cargo.SueldoBaseMensual;
        FormCantidad = cargo.CantidadDefault;
        FormEsDocente = cargo.EsCargoDocente;
        EsEdicion = true;
        FormVisible = true;
    }

    [RelayCommand]
    private void CancelarForm()
    {
        FormVisible = false;
        MensajeError = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            int carreraId = Cargos.FirstOrDefault()?.CarreraId ?? 1;

            if (!EsEdicion)
            {
                var cmd = scope.ServiceProvider.GetRequiredService<AgregarCargoFacultadCommand>();
                await cmd.EjecutarAsync(new CrearCargoFacultadDto
                {
                    CarreraId = carreraId,
                    NombreCargo = FormNombreCargo,
                    TipoCargo = FormTipoCargo,
                    SueldoBaseMensual = FormSueldo,
                    EsCargoDocente = FormEsDocente,
                    CantidadDefault = FormCantidad
                });
            }
            else
            {
                var cmd = scope.ServiceProvider.GetRequiredService<ActualizarCargoFacultadCommand>();
                await cmd.EjecutarAsync(new ActualizarCargoFacultadDto
                {
                    Id = FormCargoId,
                    CarreraId = carreraId,
                    NombreCargo = FormNombreCargo,
                    TipoCargo = FormTipoCargo,
                    SueldoBaseMensual = FormSueldo,
                    EsCargoDocente = FormEsDocente,
                    CantidadDefault = FormCantidad
                });
            }

            FormVisible = false;
            await CargarAsync(carreraId);
        }
        catch (Exception ex) { MensajeError = ex.Message; }
    }

    [RelayCommand]
    private async Task EliminarAsync(CargoFacultad cargo)
    {
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var cmd = scope.ServiceProvider.GetRequiredService<EliminarCargoFacultadCommand>();
            await cmd.EjecutarAsync(cargo.Id);
            await CargarAsync(cargo.CarreraId);
        }
        catch (Exception ex) { MensajeError = ex.Message; }
    }

    [RelayCommand]
    private async Task ImportarHoja6EjemploAsync(int carreraId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var agregar = scope.ServiceProvider.GetRequiredService<AgregarCargoFacultadCommand>();
            var ejemplo = new[]
            {
                ("Decano",                              3880m,  false, 1m    ),
                ("Subdecano",                           2880m,  false, 1m    ),
                ("Director de Carrera",                 2200m,  false, 1m    ),
                ("Secretario",                          1000m,  false, 1m    ),
                ("Auxiliar de Secretaria",              850m,   false, 1m    ),
                ("Coordinador",                         900m,   false, 1m    ),
                ("Bienestar Estudiantil",               900m,   false, 1m    ),
                ("Tiempo Completo PhD",                 2800m,  true,  0.24m ),
                ("Tiempo Completo Mgs.",                1800m,  true,  0.60m ),
                ("Medio Tiempo",                        900m,   true,  0m    ),
                ("Tiempo Parcial",                      432m,   true,  0.16m ),
                ("Técnico Docente",                     1350m,  true,  0.25m ),
                ("Bibliotecario",                       800m,   false, 1m    ),
                ("Auxiliar de Servicio",                450m,   false, 2m    ),
                ("Guardia",                             650m,   false, 1m    ),
            };
            foreach (var (nombre, sueldo, esDocente, cantidad) in ejemplo)
            {
                await agregar.EjecutarAsync(new CrearCargoFacultadDto
                {
                    CarreraId = carreraId,
                    NombreCargo = nombre,
                    TipoCargo = esDocente ? "Docente" : "Admin",
                    SueldoBaseMensual = sueldo,
                    EsCargoDocente = esDocente,
                    CantidadDefault = cantidad
                });
            }
            await CargarAsync(carreraId);
        }
        catch (Exception ex) { MensajeError = ex.Message; }
    }
}
