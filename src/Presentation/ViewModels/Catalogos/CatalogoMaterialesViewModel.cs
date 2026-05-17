using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Infrastructure.Persistence;
using SistemaAranceles.Infrastructure.Persistence.Entidades;
using System.Linq;

namespace SistemaAranceles.Presentation.ViewModels.Catalogos;

public sealed partial class CatalogoMaterialesViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;

    public CatalogoMaterialesViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    [ObservableProperty]
    private ObservableCollection<ItemMaterialInsumo> _items = [];

    [ObservableProperty]
    private ItemMaterialInsumo? _seleccionado;

    [ObservableProperty]
    private string _mensajeError = string.Empty;

    [RelayCommand]
    private async Task CargarAsync()
    {
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] CatalogoMaterialesVM: CargarAsync iniciado.");
        try
        {
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CatalogoMaterialesVM: creando scope...");
            using var scope = _serviceProvider.CreateScope();
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CatalogoMaterialesVM: obteniendo ContextoAplicacion...");
            var ctx = scope.ServiceProvider.GetRequiredService<ContextoAplicacion>();
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CatalogoMaterialesVM: ejecutando ToListAsync sobre item_material_insumo...");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var datos = await ctx.ItemsMaterialInsumo.OrderBy(x => x.NombreItem).ToListAsync(cts.Token);
            Trace.TraceInformation($"[{DateTime.UtcNow:O}] CatalogoMaterialesVM: query OK. {datos.Count} items.");
            Items = new ObservableCollection<ItemMaterialInsumo>(datos);
        }
        catch (OperationCanceledException)
        {
            const string msg = "Timeout (30 s): item_material_insumo no respondió. Verifica la conexión a Supabase.";
            Trace.TraceError($"[{DateTime.UtcNow:O}] CatalogoMaterialesVM: TIMEOUT — {msg}");
            MensajeError = msg;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"[{DateTime.UtcNow:O}] CatalogoMaterialesVM: EXCEPCION: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException is not null)
                Trace.TraceError($"[{DateTime.UtcNow:O}] CatalogoMaterialesVM: InnerException: {ex.InnerException.Message}");
            MensajeError = ex.Message;
        }
        Trace.TraceInformation($"[{DateTime.UtcNow:O}] CatalogoMaterialesVM: CargarAsync finalizado.");
    }
}
