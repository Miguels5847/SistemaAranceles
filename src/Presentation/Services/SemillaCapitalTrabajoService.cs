using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Infrastructure.Persistence;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Presentation.Services;

/// <summary>
/// Carga la semilla inicial de Capital de Trabajo (cargos + materiales).
/// Usa IServiceProvider para crear su propio scope y evitar tener dos
/// DbContext abiertos al mismo tiempo contra el pooler de Supabase.
/// </summary>
public sealed class SemillaCapitalTrabajoService
{
    private readonly IServiceProvider _serviceProvider;

    public SemillaCapitalTrabajoService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<bool> CargarSemillaCapitalTrabajoAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<ContextoAplicacion>();

        // Verificar si ya hay cargos; si existen, no cargar nuevamente
        var cargosExistentes = await ctx.CargosFacultad
            .Where(c => c.CarreraId == 1)
            .CountAsync();

        if (cargosExistentes > 0)
            return false; // Ya hay datos cargados

        var cargos = new[]
        {
            new CargoFacultad { CarreraId = 1, NombreCargo = "Decano",                              TipoCargo = "Admin",    SueldoBaseMensual = 3880.00m, EsCargoDocente = false, CantidadDefault = 1m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Subdecano",                           TipoCargo = "Admin",    SueldoBaseMensual = 2880.00m, EsCargoDocente = false, CantidadDefault = 1m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Director de Carrera",                 TipoCargo = "Director", SueldoBaseMensual = 2200.00m, EsCargoDocente = false, CantidadDefault = 1m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Secretario",                          TipoCargo = "Admin",    SueldoBaseMensual = 1000.00m, EsCargoDocente = false, CantidadDefault = 1m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Auxiliar de Secretaria",              TipoCargo = "Admin",    SueldoBaseMensual = 850.00m,  EsCargoDocente = false, CantidadDefault = 1m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Coordinador",                         TipoCargo = "Admin",    SueldoBaseMensual = 900.00m,  EsCargoDocente = false, CantidadDefault = 1m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Bienestar Estudiantil",               TipoCargo = "Admin",    SueldoBaseMensual = 900.00m,  EsCargoDocente = false, CantidadDefault = 1m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Bibliotecario",                       TipoCargo = "Admin",    SueldoBaseMensual = 800.00m,  EsCargoDocente = false, CantidadDefault = 1m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Auxiliar de Servicio",                TipoCargo = "Admin",    SueldoBaseMensual = 450.00m,  EsCargoDocente = false, CantidadDefault = 2m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Guardia",                             TipoCargo = "Admin",    SueldoBaseMensual = 650.00m,  EsCargoDocente = false, CantidadDefault = 1m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Tiempo Completo PhD",                 TipoCargo = "Docente",  SueldoBaseMensual = 2800.00m, EsCargoDocente = true,  CantidadDefault = 0.24m },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Tiempo Completo Mgs.",                TipoCargo = "Docente",  SueldoBaseMensual = 1800.00m, EsCargoDocente = true,  CantidadDefault = 0.60m },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Medio Tiempo",                        TipoCargo = "Docente",  SueldoBaseMensual = 900.00m,  EsCargoDocente = true,  CantidadDefault = 0m    },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Tiempo Parcial",                      TipoCargo = "Docente",  SueldoBaseMensual = 432.00m,  EsCargoDocente = true,  CantidadDefault = 0.16m },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Ocasional Tipo 2 (T\u00e9cnico Docente)",  TipoCargo = "Docente",  SueldoBaseMensual = 1350.00m, EsCargoDocente = true,  CantidadDefault = 0.25m },
        };

        await ctx.CargosFacultad.AddRangeAsync(cargos);

        var materiales = new[]
        {
            // Secci\u00f3n B: Materiales y Suministros (7 \u00edtems)
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Resma de papel bond de 75 gramos",                        CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Resma",   CantidadBase = 6.0m,   PrecioUnitario = 3.25m,  EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Cartuchos de impresora (Color)",                          CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad",  CantidadBase = 0.6m,   PrecioUnitario = 50.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Cartuchos de impresora (Negro)",                          CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad",  CantidadBase = 0.6m,   PrecioUnitario = 40.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Carpetas de cart\u00f3n",                                 CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad",  CantidadBase = 60.0m,  PrecioUnitario = 1.00m,  EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Porta files",                                             CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad",  CantidadBase = 30.0m,  PrecioUnitario = 2.00m,  EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Esferos, micro minas, l\u00e1piz, borradores, correctores", CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad",  CantidadBase = 30.0m,  PrecioUnitario = 0.25m,  EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Grapas, clips (CAJA)",                                    CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Caja",    CantidadBase = 0.6m,   PrecioUnitario = 1.00m,  EsCantidadFija = false },
            // Secci\u00f3n C: Suministros de Aseo y Limpieza (7 \u00edtems)
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Desinfectante (Gal\u00f3n)",                              CategoriaNombre = "ASEO_LIMPIEZA",           UnidadNombre = "Gal\u00f3n",  CantidadBase = 0.9m,   PrecioUnitario = 4.00m,  EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Jab\u00f3n L\u00edquido (Gal\u00f3n)",                    CategoriaNombre = "ASEO_LIMPIEZA",           UnidadNombre = "Gal\u00f3n",  CantidadBase = 1.08m,  PrecioUnitario = 3.00m,  EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Papel Higi\u00e9nico (Rollo Grande)",                    CategoriaNombre = "ASEO_LIMPIEZA",           UnidadNombre = "Rollo",   CantidadBase = 23.4m,  PrecioUnitario = 10.50m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Escoba",                                                  CategoriaNombre = "ASEO_LIMPIEZA",           UnidadNombre = "Unidad",  CantidadBase = 2.0m,   PrecioUnitario = 10.50m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Paquete de Fundas de Basura",                             CategoriaNombre = "ASEO_LIMPIEZA",           UnidadNombre = "Paquete", CantidadBase = 6.0m,   PrecioUnitario = 0.80m,  EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Trapeador",                                               CategoriaNombre = "ASEO_LIMPIEZA",           UnidadNombre = "Unidad",  CantidadBase = 3.0m,   PrecioUnitario = 2.50m,  EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Cloro (Gal\u00f3n)",                                     CategoriaNombre = "ASEO_LIMPIEZA",           UnidadNombre = "Gal\u00f3n",  CantidadBase = 0.9m,   PrecioUnitario = 2.50m,  EsCantidadFija = false },
            // Secci\u00f3n D: Accesorios y Materiales (2 \u00edtems)
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Grapadora",                                               CategoriaNombre = "ACCESORIOS_MATERIALES",   UnidadNombre = "Unidad",  CantidadBase = 5.25m,  PrecioUnitario = 15.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Perforadora",                                             CategoriaNombre = "ACCESORIOS_MATERIALES",   UnidadNombre = "Unidad",  CantidadBase = 5.25m,  PrecioUnitario = 10.00m, EsCantidadFija = false },
        };

        await ctx.ItemsMaterialInsumo.AddRangeAsync(materiales);

        try
        {
            await ctx.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException dbEx)
        {
            System.Diagnostics.Trace.TraceError($"SemillaCapitalTrabajoService: DbUpdateException: {dbEx.Message}");
            if (dbEx.InnerException is not null)
                System.Diagnostics.Trace.TraceError($"InnerException: {dbEx.InnerException.Message}");
            throw;
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"SemillaCapitalTrabajoService: Excepci\u00f3n: {ex.Message}");
            if (ex.InnerException is not null)
                System.Diagnostics.Trace.TraceError($"InnerException: {ex.InnerException.Message}");
            throw;
        }
    }
}
