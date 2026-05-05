using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Domain.Entities;
using SistemaAranceles.Infrastructure.Persistence;
using SistemaAranceles.Infrastructure.Persistence.Entidades;
using SistemaAranceles.Presentation.ViewModels.Catalogos;
using SistemaAranceles.Presentation.Services;

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

    public CatalogoCargosViewModel CatalogoCargos => _catalogoCargos;

    [ObservableProperty] private ObservableCollection<object> _materiales = [];
    [ObservableProperty] private ObservableCollection<object> _materialesSuministros = [];
    [ObservableProperty] private ObservableCollection<object> _aseoLimpieza = [];
    [ObservableProperty] private ObservableCollection<object> _accesoriosMateriales = [];

    [ObservableProperty] private string _mensajeError = string.Empty;
    [ObservableProperty] private string _mensajeExito = string.Empty;

    // --- Formulario CRUD Materiales ---
    [ObservableProperty] private bool _formMatVisible;
    [ObservableProperty] private bool _formMatEsEdicion;
    [ObservableProperty] private int _formMatId;
    [ObservableProperty] private string _formMatNombre = string.Empty;
    [ObservableProperty] private decimal _formMatCantidad;
    [ObservableProperty] private decimal _formMatPrecio;
    [ObservableProperty] private string _formMatCategoria = string.Empty;

    [RelayCommand]
    private async Task CargarAsync(int? carreraId = null)
    {
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: CargarAsync iniciado. carreraId={carreraId}");
        MensajeError = string.Empty;
        MensajeExito = string.Empty;
        try
        {
            // --- Semilla: scope propio, se cierra antes de cualquier otro query ---
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: iniciando SemillaCapitalTrabajoService...");
            bool cargada;
            // SemillaCapitalTrabajoService ya maneja su propio scope internamente;
            // lo resolvemos desde un scope breve solo para instanciarlo.
            using (var scopeSemilla = _serviceProvider.CreateScope())
            {
                var semilla = scopeSemilla.ServiceProvider.GetRequiredService<SemillaCapitalTrabajoService>();
                cargada = await semilla.CargarSemillaCapitalTrabajoAsync();
            }
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: semilla completada. cargada={cargada}");
            if (cargada)
                MensajeExito = "\u2713 Datos maestros de Capital de Trabajo cargados exitosamente.";

            // --- Cargos: scope propio, secuencial ---
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: cargando cargos...");
            await _catalogoCargos.CargarCommand.ExecuteAsync(carreraId ?? 1);
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: cargos OK ({_catalogoCargos.Cargos.Count} items). MensajeError cargos='{_catalogoCargos.MensajeError}'");

            // --- Materiales: scope propio, secuencial ---
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: refrescando materiales...");
            await RefrescarMaterialesAsync();
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: materiales OK. MensajeError materiales='{_catalogoMateriales.MensajeError}' Total={Materiales.Count}");
        }
        catch (System.Exception ex)
        {
            Trace.TraceError($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: EXCEPCION en CargarAsync: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException is not null)
                Trace.TraceError($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: InnerException: {ex.InnerException.Message}");
            MensajeError = ex.Message;
        }
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] CapitalTrabajoVM: CargarAsync finalizado.");
    }

    private async Task RefrescarMaterialesAsync()
    {
        await _catalogoMateriales.CargarCommand.ExecuteAsync(null);
        var items = _catalogoMateriales.Items
            ?? new ObservableCollection<ItemMaterialInsumo>();

        var agrupados = items.Select(i => new MaterialItemVm
        {
            Id = i.Id,
            NombreItem = i.NombreItem,
            Categoria = i.CategoriaNombre,
            Cantidad = i.CantidadBase,
            PrecioUnitario = i.PrecioUnitario
        }).ToList();

        Materiales             = new ObservableCollection<object>(agrupados);
        MaterialesSuministros  = new ObservableCollection<object>(agrupados.Where(i => i.Categoria == "MATERIALES_SUMINISTROS"));
        AseoLimpieza           = new ObservableCollection<object>(agrupados.Where(i => i.Categoria == "ASEO_LIMPIEZA"));
        AccesoriosMateriales   = new ObservableCollection<object>(agrupados.Where(i => i.Categoria == "ACCESORIOS_MATERIALES"));
    }

    // \u2500\u2500 CRUD Materiales \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500

    [RelayCommand]
    private void AbrirNuevoMaterial(string categoria)
    {
        FormMatId       = 0;
        FormMatNombre   = string.Empty;
        FormMatCantidad = 0m;
        FormMatPrecio   = 0m;
        FormMatCategoria = categoria;
        FormMatEsEdicion = false;
        FormMatVisible  = true;
    }

    [RelayCommand]
    private void AbrirEditarMaterial(object item)
    {
        if (item is not MaterialItemVm vm) return;
        FormMatId        = vm.Id;
        FormMatNombre    = vm.NombreItem;
        FormMatCantidad  = vm.Cantidad;
        FormMatPrecio    = vm.PrecioUnitario;
        FormMatCategoria = vm.Categoria;
        FormMatEsEdicion = true;
        FormMatVisible   = true;
    }

    [RelayCommand]
    private void CancelarFormMat()
    {
        FormMatVisible = false;
        MensajeError   = string.Empty;
    }

    [RelayCommand]
    private async Task GuardarMaterialAsync()
    {
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ContextoAplicacion>();

            if (!FormMatEsEdicion)
            {
                var nuevo = new ItemMaterialInsumo
                {
                    CarreraId       = 1,
                    NombreItem      = FormMatNombre,
                    CategoriaNombre = FormMatCategoria,
                    UnidadNombre    = "Unidad",
                    CantidadBase    = FormMatCantidad,
                    PrecioUnitario  = FormMatPrecio,
                    EsCantidadFija  = false,
                };
                ctx.ItemsMaterialInsumo.Add(nuevo);
            }
            else
            {
                var existente = await ctx.ItemsMaterialInsumo.FindAsync(FormMatId);
                if (existente is not null)
                {
                    existente.NombreItem     = FormMatNombre;
                    existente.CantidadBase   = FormMatCantidad;
                    existente.PrecioUnitario = FormMatPrecio;
                }
            }

            await ctx.SaveChangesAsync();
            FormMatVisible = false;
            await RefrescarMaterialesAsync();
        }
        catch (System.Exception ex) { MensajeError = ex.Message; }
    }

    [RelayCommand]
    private async Task EliminarMaterialAsync(object item)
    {
        if (item is not MaterialItemVm vm) return;
        MensajeError = string.Empty;
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<ContextoAplicacion>();
            var existente = await ctx.ItemsMaterialInsumo.FindAsync(vm.Id);
            if (existente is not null)
            {
                ctx.ItemsMaterialInsumo.Remove(existente);
                await ctx.SaveChangesAsync();
            }
            await RefrescarMaterialesAsync();
        }
        catch (System.Exception ex) { MensajeError = ex.Message; }
    }

    // DTO local para los DataGrids de materiales
    public sealed class MaterialItemVm
    {
        public int Id { get; init; }
        public string NombreItem { get; init; } = string.Empty;
        public string Categoria { get; init; } = string.Empty;
        public decimal Cantidad { get; init; }
        public decimal PrecioUnitario { get; init; }
    }
}
