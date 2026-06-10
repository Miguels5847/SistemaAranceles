# Épica 4 — Análisis de Bloqueadores y Limitantes

**Fecha:** 2026-04-16  
**Estado Actualizado:** Post-KAN-11 (Inflación cerrada), Pre-KAN-13 (Retención iniciada)

---

## Actualización de Incidencia (2026-04-21)

### Inconsistencia de tasas en simulación (ADM-EMPRESAS -> Histórico Ciclos 10)

**Síntoma reportado**

- Cabecera previa a ejecutar: Retención 79, Graduación 60.
- Cabecera posterior a ejecutar: Retención 39.0, Graduación 4.
- Simulaciones registradas RET: 38.95 y 3.99.
- Simulación por ciclos: 79 y 60.

**Causa raíz**

- La cabecera de simulación tomaba `RetencionPorcentajeFinal` y `GraduacionPorcentajeFinal` cuando había una simulación seleccionada.
- Esos valores son indicadores finales compuestos del motor (resultado acumulado), no las tasas configuradas de entrada.
- La sección de simulación por ciclos sí usa las tasas de configuración (`TasaRetencionPorcentaje`, `TasaGraduacionPorcentaje`).

**Corrección aplicada**

- Se alineó la cabecera para mostrar siempre tasas configuradas, también cuando existe simulación seleccionada.
- Archivo ajustado: `src/Presentation/ViewModels/TasaRetencion/SimulacionRetencionViewModel.cs`.
- Cambio clave en `ActualizarResumenCabecera()`:
  - Antes: `simulacion.RetencionPorcentajeFinal` / `simulacion.GraduacionPorcentajeFinal`.
  - Ahora: `simulacion.TasaRetencionConfigurada` / `simulacion.TasaGraduacionConfigurada`.

**Resultado esperado tras fix**

- Cabecera: 79 / 60.
- Simulaciones registradas RET: 79 / 60 (columnas cfg).
- Simulación por ciclos: 79 / 60.

**Nota funcional**

- Los indicadores finales compuestos (por ejemplo 38.95 y 3.99) siguen siendo válidos como métrica de salida del modelo, pero ya no se presentan en los campos de tasa configurada para evitar ambigüedad.

---

## 🟢 LO QUE SÍ ESTÁ LISTO

### 1. Entidades de Dominio (Parcial)

✅ **ConfiguracionRetencion.cs** — Existe, métodos validados.  
✅ **CriterioReferenciaRetencion.cs** — Existe, métodos validados.  
❌ **SimulacionRetencion.cs** — FALTA crear.  
❌ **DetalleSimulacionRetencion.cs** — FALTA crear.

**Acción:** Crear 2 entidades (1–2 horas).

### 2. Tabla de Auditoría (auditoria_log)

✅ Existe desde KAN-09.  
✅ Tiene campos para registrar cambios (valores_anteriores_json, valores_nuevos_json).  
✅ IAuditoriaServicio.RegistrarAsync() ready.

**Acción:** Reutilizar sin cambios.

### 3. Dependencia: Inflación

✅ KAN-12 completado.  
✅ `ObtenerInflacionProyectadaParaDependientesUseCase` existe y funciona.  
✅ Retorna DTO con valor proyectado + metadata.

**Acción:** Consumir directamente en KAN-14.

### 4. Infraestructura: RBAC + Sesiones

✅ Sistema de permisos activo (KAN-08, KAN-11).  
✅ `SesionActual.TienePermiso()` funcional.  
✅ Auditoría automática en creaciones/actualizaciones.

**Acción:** Crear 4 permisos nuevos: `TRE.CREAR`, `TRE.EDITAR`, `TRE.VER`, `TRE.ELIMINAR`.

### 5. Arquitectura Clean

✅ Pattern establecido: Use Cases → DTOs → Validadores → Repositories.  
✅ DI Container funcional (App.xaml.cs).  
✅ EF Core 8 + PostgreSQL Supabase verificado.

**Acción:** Seguir patrón existente, sin cambios.

### 6. WPF + MVVM

✅ ViewModels + Views creados en KAN-06 (Usuarios), KAN-07 (Login), KAN-10 (Inflación).  
✅ CommunityToolkit.Mvvm activo.  
✅ Converters, Commands funcionales.

**Acción:** Crear ViewModels/Views para ConfiguracionRetencion e IndicadoresRetencion.

### 7. Librerías Necesarias

✅ **FluentValidation** — Instalado, usado en múltiples módulos.  
✅ **AutoMapper** — Instalado para mapeos DTOs.  
✅ **ClosedXML** — Instalado, usado en exportaciones.  
✅ **BCrypt.Net-Next** — Instalado (sesiones).  
✅ **EF Core 8** — Instalado, migraciones funcionales.

**Acción:** Instalar solo **OxyPlot.Wpf** para KAN-15 (gráficos).

### 8. Script SQL Preparado

✅ Modelo ED diseñado en KAN-03-modelo-db.md.  
✅ Tablas definidas con índices en este plan.  
✅ Campos base (creado_en, creado_por, actualizado_en, esta_activo) coherentes con patrón.

**Acción:** Crear migración EF Core (no SQL manual).

---

## 🔴 BLOQUEADORES CRÍTICOS (BLOQUEAN INICIO)

### 1. Entidades SimulacionRetencion + DetalleSimulacionRetencion NO EXISTEN

**Severidad:** CRÍTICA — Bloquea KAN-14  
**Impacto:** Sin entidades, no hay BD, no hay persistencia, no hay motor.

**Síntomas:**

```csharp
// Esto falla hoy:
var simulacion = new SimulacionRetencion(...);  // ❌ No existe
```

**Solución:**

```csharp
// src/Domain/Entities/SimulacionRetencion.cs
public sealed class SimulacionRetencion : EntidadDominioBase
{
    public int ConfiguracionRetencionId { get; private set; }
    public int CohorteAño { get; private set; }
    public DateTime FechaSimulacion { get; private set; }
    public decimal RetencionPorcentajeFinal { get; private set; }
    public decimal GraduacionPorcentajeFinal { get; private set; }
    public decimal EstudiantesTotalesInicio { get; private set; }
    public decimal EstudiantesRetenidos { get; private set; }
    public decimal EstudiantesGraduados { get; private set; }
    public decimal CostoMatrículaPromedio { get; private set; }
    public ICollection<DetalleSimulacionRetencion> Detalles { get; private set; }
    // ... constructores, métodos validación
}
```

**Tiempo:** 1 hora.  
**Crítica:** ⚠️ **Debe hacerse antes de KAN-14.**

---

### 2. Tablas BD NO MIGRABLES

**Severidad:** CRÍTICA — Bloquea persistencia  
**Impacto:** `dotnet ef database update` fallará si no están los DbSets en ContextoAplicacion.

**Problema:**

```csharp
// En ContextoAplicacion.cs:
// ❌ Falta:
// public DbSet<SimulacionRetencion> SimulacionesRetencion { get; set; }
// public DbSet<DetalleSimulacionRetencion> DetalleSimulacionesRetencion { get; set; }
```

**Solución:**

1. Crear entidades (ver bloqueador #1).
2. Agregar DbSets en `ContextoAplicacion.cs`:

```csharp
public DbSet<SimulacionRetencion> SimulacionesRetencion { get; set; }
public DbSet<DetalleSimulacionRetencion> DetalleSimulacionesRetencion { get; set; }
```

3. Agregar modelBuilder en `OnModelCreating`:

```csharp
modelBuilder.Entity<SimulacionRetencion>()
    .HasKey(s => s.Id);

modelBuilder.Entity<SimulacionRetencion>()
    .HasOne(s => s.ConfiguracionRetencion)
    .WithMany(c => c.Simulaciones)
    .HasForeignKey(s => s.ConfiguracionRetencionId)
    .OnDelete(DeleteBehavior.Restrict);

modelBuilder.Entity<DetalleSimulacionRetencion>()
    .HasKey(d => d.Id);

// Índices
modelBuilder.Entity<SimulacionRetencion>()
    .HasIndex(s => new { s.ConfiguracionRetencionId, s.CohorteAño })
    .IsUnique();
```

4. Crear migración:

```bash
dotnet ef migrations add KAN_14_Retencion \
  --project .\src\Infrastructure\SistemaAranceles.Infrastructure.csproj \
  --startup-project .\src\Presentation\SistemaAranceles.Presentation.csproj
```

5. Aplicar:

```bash
dotnet ef database update
```

**Tiempo:** 2–3 horas (entidades + migración + validación).  
**Crítica:** ⚠️ **Debe hacerse en KAN-14 Fase 1.**

---

### 3. Repositorios NO EXISTEN

**Severidad:** ALTA — Bloquea Use Cases  
**Impacto:** Use cases no pueden acceder a BD.

**Problema:**

```csharp
// ❌ No existen:
IRepositorioSimulacionRetencion
IRepositorioDetalleSimulacionRetencion
// Tampoco sus implementaciones (Repository class)
```

**Solución:**
Crear en `src/Infrastructure/Persistence/`:

```csharp
public class RepositorioSimulacionRetencion : RepositorioGenerico<SimulacionRetencion>, IRepositorioSimulacionRetencion
{
    public RepositorioSimulacionRetencion(ContextoAplicacion contexto) : base(contexto) { }

    public async Task<SimulacionRetencion> ObtenerPorConfigYCohortAsync(int configId, int cohorteAño)
    {
        return await _contexto.SimulacionesRetencion
            .FirstOrDefaultAsync(s => s.ConfiguracionRetencionId == configId && s.CohorteAño == cohorteAño);
    }
}

public class RepositorioDetalleSimulacionRetencion : RepositorioGenerico<DetalleSimulacionRetencion>, IRepositorioDetalleSimulacionRetencion
{
    public RepositorioDetalleSimulacionRetencion(ContextoAplicacion contexto) : base(contexto) { }

    public async Task<IEnumerable<DetalleSimulacionRetencion>> ObtenerPorSimulacionAsync(int simulacionId)
    {
        return await _contexto.DetalleSimulacionesRetencion
            .Where(d => d.SimulacionRetencionId == simulacionId)
            .OrderBy(d => d.Ciclo)
            .ToListAsync();
    }
}
```

**Tiempo:** 2 horas.  
**Crítica:** ⚠️ **Debe hacerse en KAN-14 Fase 1.**

---

## 🟡 BLOQUEADORES DE MEDIANO IMPACTO (NO BLOQUEAN PERO DEMORAN)

### 4. Permisos RBAC NO CREADOS

**Severidad:** MEDIA — Bloquea autorización en tiempo de ejecución  
**Impacto:** Use cases fallarán con `sesion.VerificarPermiso("TRE.CREAR")` → excepción si no existe.

**Problema:**

```sql
-- ❌ Falta en BD:
INSERT INTO permiso(codigo, nombre, esta_activo) VALUES
  ('TRE.CREAR', 'Crear configuración retención', true),
  ('TRE.EDITAR', 'Editar configuración retención', true),
  ('TRE.VER', 'Ver indicadores retención', true),
  ('TRE.ELIMINAR', 'Eliminar simulación retención', true);
```

**Solución:**
Opción A (Manual post-deploy):

```sql
-- Script de permisos
INSERT INTO permiso(codigo, nombre) VALUES ... (4 filas)
```

Opción B (Seed en migración):

```csharp
modelBuilder.Entity<Permiso>().HasData(
    new Permiso { Id = 50, Codigo = "TRE.CREAR", Nombre = "Crear configuración retención" },
    new Permiso { Id = 51, Codigo = "TRE.EDITAR", Nombre = "Editar configuración retención" },
    new Permiso { Id = 52, Codigo = "TRE.VER", Nombre = "Ver indicadores retención" },
    new Permiso { Id = 53, Codigo = "TRE.ELIMINAR", Nombre = "Eliminar simulación retención" }
);
```

**Tiempo:** 30 min — 1 hora.  
**Recomendación:** Crear en KAN-17 (post-KAN-16) como parte de seed de datos de producción.

---

### 5. Librería OxyPlot NO INSTALADA

**Severidad:** MEDIA — Bloquea KAN-15  
**Impacto:** `using OxyPlot` falla, gráficos no renderean.

**Problema:**

```csharp
// ❌ En KAN-15:
using OxyPlot;  // ← No existe
using OxyPlot.Wpf;
```

**Solución:**

```bash
dotnet add .\src\Presentation\SistemaAranceles.Presentation.csproj package OxyPlot.Wpf --version 2.1.2
```

**Tiempo:** 5 min instalación + 1–2 horas integración en ViewModels.  
**Recomendación:** Instalar al iniciar KAN-15.

---

### 6. ViewModel IndicadoresRetencionViewModel NO EXISTE

**Severidad:** MEDIA — Bloquea KAN-15 UI  
**Impacto:** Vista no tiene lógica, no puede mostrar datos.

**Solución:**
Crear `src/Presentation/ViewModels/IndicadoresRetencionViewModel.cs`:

```csharp
public partial class IndicadoresRetencionViewModel : ObservableObject
{
    private readonly ObtenerIndicadoresRetencionUseCase _obtenerIndicadores;
    private readonly ExportarIndicadoresRetencionExcelUseCase _exportarExcel;

    [ObservableProperty]
    private ObservableCollection<IndicadorRetencionDto> listaIndicadores;

    [RelayCommand]
    public async Task Filtrar()
    {
        var indicadores = await _obtenerIndicadores.EjecutarAsync(
            FiltroCarrera, FiltroEscenario
        );
        ListaIndicadores = new(indicadores);
    }

    [RelayCommand]
    public async Task ExportarExcel()
    {
        var ruta = await _exportarExcel.EjecutarAsync(ListaIndicadores.ToList());
        System.Diagnostics.Process.Start("explorer.exe", ruta);
    }
}
```

**Tiempo:** 2–3 horas.  
**Crítica:** ⚠️ **Debe hacerse en KAN-15.**

---

## 🟠 LIMITANTES POTENCIALES (REQUIEREN ATENCIÓN)

### 7. Falta de pruebas unitarias en motor de simulación

**Severidad:** MEDIA (no bloquea, pero aumenta riesgo)  
**Impacto:** Errores en cálculo de ciclos (retención/graduación) pueden pasar a producción.

**Recomendación:**

```csharp
// Tests/Application/TasaRetencion/CrearSimulacionRetencionUseCaseTests.cs
[TestFixture]
public class CrearSimulacionRetencionUseCaseTests
{
    [Test]
    public async Task DadoConfiguracion_CuandoSimula_EntoncesCalculaCorrectamente()
    {
        // Arrange: config 100 estudiantes, 80% retención, 90% graduación, 8 ciclos
        var config = new ConfiguracionRetencion(
            carreraId: 1, escenarioId: 1, totalCiclos: 8,
            tasaRet: 80m, tasaGrad: 90m
        );

        // Act
        var resultado = await useCase.EjecutarAsync(solicitud, sesion);

        // Assert
        Assert.That(resultado.RetencionPorcentajeFinal, Is.GreaterThan(75).And.LessThan(85));
        Assert.That(resultado.GraduacionPorcentajeFinal, Is.GreaterThan(70).And.LessThan(80));
    }
}
```

**Tiempo:** 4–6 horas (post-KAN-14, antes de release).  
**Crítica:** ⏳ **KAN-17 o pre-release (no bloquea KAN-13..16).**

---

### 8. Falta de script de rollback BD

**Severidad:** BAJA (no bloquea, pero importante para ops)  
**Impacto:** Si migración falla en producción, no hay rollback automático.

**Recomendación:**

```bash
# sql/KAN-14-Rollback.sql
DROP TABLE IF EXISTS detalle_simulacion_retencion CASCADE;
DROP TABLE IF EXISTS simulacion_retencion CASCADE;
DROP INDEX IF EXISTS idx_simulacion_retencion_config;
DROP INDEX IF EXISTS idx_simulacion_retencion_cohorte;
```

**Tiempo:** 30 min (post-KAN-14).  
**Crítica:** ⏳ **KAN-17 (documentación, no código).**

---

### 9. Falta de validación de integridad referencial en UI

**Severidad:** BAJA (EF lo maneja, pero UX es pobre)  
**Impacto:** Usuario intenta editar config que ya tiene simulaciones; error de BD genérico.

**Recomendación:**

```csharp
public async Task ActualizarConfigCommand(ConfiguracionRetencionRespuestaDto config)
{
    var tieneSimulaciones = await _repoSim.ExistenPorConfigAsync(config.Id);
    if (tieneSimulaciones)
    {
        MessageBox.Show("No puede editar: ya existen simulaciones.", "Validación");
        return;
    }
    // ... continuar actualización
}
```

**Tiempo:** 1 hora (KAN-13).  
**Crítica:** ⏳ **Mejora UX, no bloquea.**

---

## 📊 Matriz de Bloqueadores

| ID  | Bloqueador            | Severidad  | Bloquea | Tiempo | KAN | Acción         |
| --- | --------------------- | ---------- | ------- | ------ | --- | -------------- |
| 1   | Entidades Sim+Detalle | 🔴 CRÍTICA | KAN-14  | 1h     | 14  | Crear ASAP     |
| 2   | Tablas BD + migración | 🔴 CRÍTICA | KAN-14  | 2–3h   | 14  | Crear ASAP     |
| 3   | Repositorios          | 🔴 ALTA    | KAN-14  | 2h     | 14  | Crear ASAP     |
| 4   | Permisos RBAC         | 🟡 MEDIA   | KAN-13+ | 1h     | 17  | Pre-deploy     |
| 5   | OxyPlot               | 🟡 MEDIA   | KAN-15  | 1h     | 15  | Install ASAP   |
| 6   | ViewModel Indicadores | 🟡 MEDIA   | KAN-15  | 2h     | 15  | Crear ASAP     |
| 7   | Pruebas motor         | 🟠 BAJA    | Release | 6h     | 17  | Pre-release    |
| 8   | Script rollback       | 🟠 BAJA    | Ops     | 1h     | 17  | Post-migración |
| 9   | Validación UI         | 🟠 BAJA    | UX      | 1h     | 13  | Mejora         |

---

## ✅ Plan de Desbloqueadores

### Fase 0 (Hoy — 2026-04-16)

```bash
# Crear ramas ✅ HECHO
git checkout -b feature/KAN-13-config-carrera-cohorte
git checkout -b feature/KAN-14-simulacion-cohorte
git checkout -b feature/KAN-15-indicadores-retencion
git checkout -b feature/KAN-16-edicion-trazabilidad
```

### Fase 1 (KAN-14 Día 1)

```bash
# Crear bloqueadores 1–3
1. Crear SimulacionRetencion + DetalleSimulacionRetencion (1h)
2. Agregar DbSets + migración (1h)
3. Crear repositorios (1h)
4. Validar build (30 min)
5. Commit: "KAN-14 Phase 1: Entidades + BD"
```

### Fase 2 (KAN-13, KAN-14, KAN-15)

```bash
# Crear bloqueadores 5–6 en paralelo
KAN-13: Use cases configuración (4 SP)
KAN-14: Motor simulación (5 SP) — requiere Fase 1
KAN-15: ViewModels + OxyPlot (3 SP) — requiere bloqueador 5
```

### Fase 3 (KAN-16)

```bash
# Trazabilidad (2 SP) — no depende de bloqueadores, reutiliza auditoría
```

### Fase 4 (Post-KAN-16)

```bash
# Crear permisos + pruebas + scripts rollback
```

---

## 🎯 Recomendación Final

**Estado:** Épica 4 es 100% viable, pero **3 bloqueadores críticos** deben resolverse en KAN-14 Phase 1.

**Orden de acción:**

1. ✅ Ramas creadas → inicio KAN-13.
2. ⚠️ **KAN-14 Fase 1:** Resolver bloqueadores 1–3 (4 horas).
3. ✅ KAN-13, KAN-14, KAN-15, KAN-16 en paralelo (8–10 días).
4. 🔄 Merge develop + main (2 días post-KAN-16).
5. ⏳ Post-release: permisos, pruebas, rollback (2 horas).

**Riesgo:** Si no resuelves bloqueadores 1–3 antes de KAN-14 Fase 2, todo colapsa.  
**Mitigación:** Priorizar entidades + BD sobre UI en KAN-14.

---

**¿Alguna duda o bloqueador adicional?** Revisar sección correspondiente.
