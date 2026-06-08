using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaAranceles.Domain.Constantes;
using SistemaAranceles.Domain.Enums;
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

        const decimal sueldoTPMensual = 432.00m;
        var tarifaTP = sueldoTPMensual / (ConstantesDocentes.HorasTPMaxSemana * ConstantesDocentes.SemanasPorMes);

        var cargos = new[]
        {
            new CargoFacultad { CarreraId = 1, NombreCargo = "Decano",                              SueldoBaseMensual = 3880.00m, EsCargoDocente = false, CantidadDefault = 1m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Subdecano",                           SueldoBaseMensual = 2880.00m, EsCargoDocente = false, CantidadDefault = 1m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Director de Carrera",                 SueldoBaseMensual = 2200.00m, EsCargoDocente = false, CantidadDefault = 1m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Secretario",                          SueldoBaseMensual = 1000.00m, EsCargoDocente = false, CantidadDefault = 1m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Auxiliar de Secretaria",              SueldoBaseMensual = 850.00m,  EsCargoDocente = false, CantidadDefault = 1m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Coordinador",                         SueldoBaseMensual = 900.00m,  EsCargoDocente = false, CantidadDefault = 1m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Bienestar Estudiantil",               SueldoBaseMensual = 900.00m,  EsCargoDocente = false, CantidadDefault = 1m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Bibliotecario",                       SueldoBaseMensual = 800.00m,  EsCargoDocente = false, CantidadDefault = 1m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Auxiliar de Servicio",                SueldoBaseMensual = 450.00m,  EsCargoDocente = false, CantidadDefault = 2m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Guardia",                             SueldoBaseMensual = 650.00m,  EsCargoDocente = false, CantidadDefault = 1m,    TipoContrato = TipoContrato.Administrativo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Tiempo Completo PhD",                 SueldoBaseMensual = 2800.00m, EsCargoDocente = true,  CantidadDefault = 0.24m, TipoContrato = TipoContrato.PhD },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Tiempo Completo Mgs.",                SueldoBaseMensual = 1800.00m, EsCargoDocente = true,  CantidadDefault = 0.60m, TipoContrato = TipoContrato.Mgs },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Medio Tiempo",                        SueldoBaseMensual = 900.00m,  EsCargoDocente = true,  CantidadDefault = 0m,    TipoContrato = TipoContrato.MedioTiempo },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Tiempo Parcial",                      SueldoBaseMensual = 0m,       EsCargoDocente = true,  CantidadDefault = 0.16m, TipoContrato = TipoContrato.TiempoParcial, TarifaHora = tarifaTP },
            new CargoFacultad { CarreraId = 1, NombreCargo = "Ocasional Tipo 2 (T\u00e9cnico Docente)",  SueldoBaseMensual = 1350.00m, EsCargoDocente = true,  CantidadDefault = 0.25m, TipoContrato = TipoContrato.Tecnico },
        };

        await ctx.CargosFacultad.AddRangeAsync(cargos);

        // Mismo cat\u00e1logo can\u00f3nico que usa el bot\u00f3n "Generar materiales por defecto" (fuente \u00fanica).
        var materiales = CatalogoMaterialesPorDefecto.Items
            .Select(m => new ItemMaterialInsumo
            {
                CarreraId = 1,
                NombreItem = m.Nombre,
                CategoriaNombre = m.Categoria,
                UnidadNombre = m.Unidad,
                CantidadBase = m.CantidadBase,
                PrecioUnitario = m.PrecioUnitario,
                EsCantidadFija = false
            })
            .ToArray();

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
