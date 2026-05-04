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

    [ObservableProperty]
    private ObservableCollection<CargoFacultad> _cargos = [];

    [ObservableProperty]
    private CargoFacultad? _cargoSeleccionado;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [ObservableProperty]
    private bool _estaCargando;

    [RelayCommand]
    private async Task CargarAsync(int? carreraId = null)
    {
        if (EstaCargando)
            return;

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
            var preview = lista.Select(x => new CargoFacultad(x.CarreraId, x.NombreCargo, x.TipoCargo, x.SueldoBaseMensual, x.EsCargoDocente)).ToList();

            Cargos = new ObservableCollection<CargoFacultad>(preview);
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

    [RelayCommand]
    private async Task ImportarHoja6EjemploAsync(int carreraId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var agregar = scope.ServiceProvider.GetRequiredService<AgregarCargoFacultadCommand>();

            var ejemplo = new[]
            {
                ("Decano", 3880m, false),
                ("Subdecano", 2880m, false),
                ("Director de Carrera", 2200m, false),
                ("Secretario", 1000m, false),
                ("Auxiliar de Secretaria", 850m, false),
                ("Coordinador", 900m, false),
                ("Bienestar Estudiantil", 900m, false),
                ("Tiempo Completo PhD", 2800m, true),
                ("Tiempo Completo Mgs.", 1800m, true),
                ("Medio Tiempo", 900m, true),
                ("Tiempo Parcial", 432m, true),
                ("Técnico Docente", 1350m, true),
                ("Bibliotecario", 800m, false),
                ("Auxiliar de Servicio", 450m, false),
                ("Guardia", 650m, false),
            };

            foreach (var (nombre, sueldo, esDocente) in ejemplo)
            {
                var dto = new CrearCargoFacultadDto
                {
                    CarreraId = carreraId,
                    NombreCargo = nombre,
                    TipoCargo = esDocente ? "Docente" : "Admin",
                    SueldoBaseMensual = sueldo,
                    EsCargoDocente = esDocente
                };

                await agregar.EjecutarAsync(dto);
            }

            await CargarAsync(carreraId);
        }
        catch (Exception ex)
        {
            MensajeError = ex.Message;
        }
    }
}
