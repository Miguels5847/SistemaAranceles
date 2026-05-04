using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Presentation.ViewModels.Catalogos;
using SistemaAranceles.Application.UseCases.CargosFacultad;
using SistemaAranceles.Application.DTOs.CargosFacultad;

namespace SistemaAranceles.Presentation.ViewModels.CapitalTrabajo;

public sealed partial class CapitalTrabajoViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CatalogoCargosViewModel _catalogoCargos;
    private readonly CatalogoMaterialesViewModel _catalogoMateriales;

    public CapitalTrabajoViewModel(IServiceProvider serviceProvider,
        CatalogoCargosViewModel catalogoCargos,
        CatalogoMaterialesViewModel catalogoMateriales)
    {
        _serviceProvider = serviceProvider;
        _catalogoCargos = catalogoCargos;
        _catalogoMateriales = catalogoMateriales;
    }

    [ObservableProperty]
    private ObservableCollection<CargoFacultad> _cargos = [];

    [ObservableProperty]
    private ObservableCollection<object> _materiales = [];

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [RelayCommand]
    private async Task CargarAsync(int? carreraId = null)
    {
        MensajeError = string.Empty;
        try
        {
            // Cargar ambos catálogos
            await _catalogoCargos.CargarCommand.ExecuteAsync(carreraId);
            await _catalogoMateriales.CargarCommand.ExecuteAsync(null);

            Cargos = new ObservableCollection<CargoFacultad>(_catalogoCargos.Cargos);

            // Construir secciones de materiales por categoría
            var items = _catalogoMateriales.Items ?? new System.Collections.ObjectModel.ObservableCollection<SistemaAranceles.Infrastructure.Persistence.Entidades.ItemMaterialInsumo>();

            // Emitir un objeto combinado con categoría para binding sencillo
            var agrupados = items.Select(i => new
            {
                i.Id,
                NombreItem = i.NombreItem,
                Categoria = i.CategoriaNombre,
                PrecioUnitario = i.PrecioUnitario
            }).ToList();

            Materiales = new ObservableCollection<object>(agrupados);
        }
        catch (System.Exception ex)
        {
            MensajeError = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ImportarHoja6EjemploAsync(object carreraIdObj)
    {
        int carreraId = 0;
        if (carreraIdObj is int i) carreraId = i;
        else if (carreraIdObj is string s && int.TryParse(s, out var parsed)) carreraId = parsed;

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
        catch (System.Exception ex)
        {
            MensajeError = ex.Message;
        }
    }
}
