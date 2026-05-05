using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SistemaAranceles.Infrastructure.Persistence;
using SistemaAranceles.Infrastructure.Persistence.Entidades;

namespace SistemaAranceles.Presentation.Services;

public sealed class SemillaCapitalTrabajoService
{
    private readonly ContextoAplicacion _contexto;

    public SemillaCapitalTrabajoService(ContextoAplicacion contexto)
    {
        _contexto = contexto;
    }

    public async Task<bool> CargarSemillaCapitalTrabajoAsync()
    {
        // Verificar si ya hay cargos; si existen, no cargar nuevamente
        var cargosExistentes = await _contexto.CargosFacultad
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
            new CargoFacultad { CarreraId = 1, NombreCargo = "Ocasional Tipo 2 (Técnico Docente)",  TipoCargo = "Docente",  SueldoBaseMensual = 1350.00m, EsCargoDocente = true,  CantidadDefault = 0.25m },
        };

        await _contexto.CargosFacultad.AddRangeAsync(cargos);

        // Cargar 16 ítems de materiales
        var materiales = new[]
        {
            // Sección B: Materiales y Suministros (7 ítems)
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Resma de papel bond de 75 gramos", CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Resma", CantidadBase = 6.0m, PrecioUnitario = 3.25m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Cartuchos de impresora (Color)", CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad", CantidadBase = 0.6m, PrecioUnitario = 50.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Cartuchos de impresora (Negro)", CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad", CantidadBase = 0.6m, PrecioUnitario = 40.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Carpetas de cartón", CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad", CantidadBase = 60.0m, PrecioUnitario = 1.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Porta files", CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad", CantidadBase = 30.0m, PrecioUnitario = 2.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Esferos, micro minas, lápiz, borradores, correctores", CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Unidad", CantidadBase = 30.0m, PrecioUnitario = 0.25m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Grapas, clips (CAJA)", CategoriaNombre = "MATERIALES_SUMINISTROS", UnidadNombre = "Caja", CantidadBase = 0.6m, PrecioUnitario = 1.00m, EsCantidadFija = false },

            // Sección C: Suministros de Aseo y Limpieza (7 ítems)
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Desinfectante (Galón)", CategoriaNombre = "ASEO_LIMPIEZA", UnidadNombre = "Galón", CantidadBase = 0.9m, PrecioUnitario = 4.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Jabón Líquido (Galón)", CategoriaNombre = "ASEO_LIMPIEZA", UnidadNombre = "Galón", CantidadBase = 1.08m, PrecioUnitario = 3.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Papel Higiénico (Rollo Grande)", CategoriaNombre = "ASEO_LIMPIEZA", UnidadNombre = "Rollo", CantidadBase = 23.4m, PrecioUnitario = 10.50m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Escoba", CategoriaNombre = "ASEO_LIMPIEZA", UnidadNombre = "Unidad", CantidadBase = 2.0m, PrecioUnitario = 10.50m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Paquete de Fundas de Basura", CategoriaNombre = "ASEO_LIMPIEZA", UnidadNombre = "Paquete", CantidadBase = 6.0m, PrecioUnitario = 0.80m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Trapeador", CategoriaNombre = "ASEO_LIMPIEZA", UnidadNombre = "Unidad", CantidadBase = 3.0m, PrecioUnitario = 2.50m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Cloro (Galón)", CategoriaNombre = "ASEO_LIMPIEZA", UnidadNombre = "Galón", CantidadBase = 0.9m, PrecioUnitario = 2.50m, EsCantidadFija = false },

            // Sección D: Accesorios y Materiales (2 ítems)
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Grapadora", CategoriaNombre = "ACCESORIOS_MATERIALES", UnidadNombre = "Unidad", CantidadBase = 5.25m, PrecioUnitario = 15.00m, EsCantidadFija = false },
            new ItemMaterialInsumo { CarreraId = 1, NombreItem = "Perforadora", CategoriaNombre = "ACCESORIOS_MATERIALES", UnidadNombre = "Unidad", CantidadBase = 5.25m, PrecioUnitario = 10.00m, EsCantidadFija = false },
        };

        await _contexto.ItemsMaterialInsumo.AddRangeAsync(materiales);

        // Guardar cambios con manejo de errores para exponer inner exception
        try
        {
            await _contexto.SaveChangesAsync();
            return true; // Semilla cargada exitosamente
        }
        catch (DbUpdateException dbEx)
        {
            System.Diagnostics.Trace.TraceError($"SemillaCapitalTrabajoService: DbUpdateException al guardar semilla: {dbEx.Message}");
            if (dbEx.InnerException is not null)
                System.Diagnostics.Trace.TraceError($"InnerException: {dbEx.InnerException.Message}");
            throw; // rethrow para que la capa superior lo vea también
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"SemillaCapitalTrabajoService: Excepción al guardar semilla: {ex.Message}");
            if (ex.InnerException is not null)
                System.Diagnostics.Trace.TraceError($"InnerException: {ex.InnerException.Message}");
            throw;
        }
    }
}
