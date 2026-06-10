# Épica 4 — Resumen Ejecutivo para Claude Code

**Leído esto, estás listo para empezar KAN-13 ahora mismo.**

---

## 🚀 Estado Actual

- ✅ Ramas creadas: `feature/KAN-13/14/15/16-*`
- ✅ Épica 3 (Inflación) completada.
- ✅ Arquitectura Clean + RBAC + Auditoría funcionando.
- ❌ **3 bloqueadores críticos** en KAN-14 (resolver primero).

---

## 📋 Lo que Necesitas Hacer (14 SP, ~8–10 días)

### **KAN-13: Configuración (4 SP)** — Empieza AQUÍ

```
🎯 Objetivo: CRUD configuración carrera/cohorte para simulación.

✅ Ya existe:
  - ConfiguracionRetencion entity (Domain/)
  - CriterioReferenciaRetencion entity (Domain/)
  - Base pattern (repos, use cases, DTOs, UI)

TODO:
  1. Crear repos: IRepositorioConfiguracionRetencion + impl ✏️
  2. Crear 7 use cases: Crear, Actualizar, Listar, Obtener, Eliminar, + 2 de criterio
  3. Crear DTOs (5–6): Solicitud/Respuesta para config + criterio
  4. Crear validadores FluentValidation (3–4)
  5. Crear UI: ConfiguracionRetencionView + ViewModel (WPF)
  6. Registrar DI: App.xaml.cs, Program.cs
  7. Build → 0E/0W → git commit → push

Tiempo: 4 SP (~3 días 1 dev)
Permisos nuevos: TRE.CREAR, TRE.EDITAR, TRE.VER, TRE.ELIMINAR (crear post-release)
```

**Rama:** `git checkout feature/KAN-13-config-carrera-cohorte`

---

### **KAN-14: Simulación Motor (5 SP)** — CRÍTICO

```
🎯 Objetivo: Motor ciclo-a-ciclo + persistencia.

⚠️ BLOQUEADORES CRÍTICOS (resuelve primero):
  ❌ 1. Entidades SimulacionRetencion + DetalleSimulacionRetencion NO existen → CREAR (1h)
  ❌ 2. Tablas BD NO migrables → Crear migración EF (2–3h)
  ❌ 3. Repositorios NO existen → CREAR (2h)

Después de desbloquear:
  1. Crear 6 use cases: Crear (⭐ motor), Actualizar, Listar, Obtener, Eliminar, Limpiar
  2. Motor: ciclos, retención%, graduación%, inflación (consume ObtenerInflacionProyectadaParaDependientesUseCase)
  3. DTOs (4): Solicitud + Respuesta, Detalles
  4. Validadores FluentValidation (3)
  5. Mapeos AutoMapper
  6. Registrar DI
  7. Prueba: dotnet ef database update → BD actualizada
  8. Build → 0E/0W → commit → push

Tiempo: 5 SP (~4 días 1 dev)
Dependencias: Inflación (KAN-12) ✅ + bloqueadores arriba ❌
Transacciones: Motor DEBE usar BEGIN/COMMIT/ROLLBACK (EF TransactionScope)
```

**Rama:** `git checkout feature/KAN-14-simulacion-cohorte`

---

### **KAN-15: Indicadores + Gráficos (3 SP)** — Paralelo OK

```
🎯 Objetivo: Visualizar Ret% y Titu% + exportar Excel.

TODO:
  1. Crear 2 use cases: ObtenerIndicadoresRetencion, ExportarIndicadoresRetencionExcel
  2. Crear DTOs (2): IndicadorRetencionDto, ListaIndicadoresRespuestaDto
  3. Instalar OxyPlot: dotnet add ... package OxyPlot.Wpf
  4. Crear ViewModel: IndicadoresRetencionViewModel (gráficos + export)
  5. Crear View: IndicadoresRetencionView.xaml (DataGrid + Charts)
  6. Mapeos
  7. Registrar DI
  8. Build → 0E/0W → commit → push

Tiempo: 3 SP (~2 días 1 dev)
Bloquea: Ninguno (puede hacerse en paralelo a KAN-14)
Requiere: KAN-14 completado (datos en BD)
```

**Rama:** `git checkout feature/KAN-15-indicadores-retencion`

---

### **KAN-16: Trazabilidad + Edición (2 SP)** — Integración Auditoría

```
🎯 Objetivo: Editar criterios + registrar cambios en auditoria_log.

TODO:
  1. Crear 3 use cases: ActualizarCriterioConTrazabilidad, ObtenerHistorialCriterio, ObtenerTrazabilidadSimulacion
  2. Crear DTOs (3): CriterioConTrazabilidad, EntradaAuditoria, TrazabilidadSimulacion
  3. NO requiere tablas nuevas (usa auditoria_log existente) ✅
  4. Registrar DI
  5. Build → 0E/0W → commit → push

Tiempo: 2 SP (~1 día 1 dev)
Bloquea: Ninguno
Requiere: Auditoría (KAN-09) ✅ ya funciona
```

**Rama:** `git checkout feature/KAN-16-edicion-trazabilidad`

---

## 📂 Estructura de Carpetas (Crear Esto)

```
src/
├── Domain/Entities/
│   ├── SimulacionRetencion.cs (🆕 KAN-14)
│   └── DetalleSimulacionRetencion.cs (🆕 KAN-14)
├── Domain/Interfaces/Persistencia/
│   ├── IRepositorioConfiguracionRetencion.cs (KAN-13)
│   ├── IRepositorioCriterioReferenciaRetencion.cs (KAN-13)
│   ├── IRepositorioSimulacionRetencion.cs (🆕 KAN-14)
│   └── IRepositorioDetalleSimulacionRetencion.cs (🆕 KAN-14)
├── Application/UseCases/TasaRetencion/
│   ├── KAN-13/
│   │   ├── CrearConfiguracionRetencionUseCase.cs
│   │   ├── ActualizarConfiguracionRetencionUseCase.cs
│   │   ├── ListarConfiguracionesRetencionUseCase.cs
│   │   ├── ObtenerConfiguracionRetencionUseCase.cs
│   │   ├── EliminarConfiguracionRetencionUseCase.cs
│   │   ├── CrearCriterioReferenciaRetencionUseCase.cs
│   │   └── ActualizarCriterioReferenciaRetencionUseCase.cs
│   ├── KAN-14/
│   │   ├── CrearSimulacionRetencionUseCase.cs (⭐ Motor)
│   │   ├── ActualizarSimulacionRetencionUseCase.cs
│   │   ├── ListarSimulacionesRetencionUseCase.cs
│   │   ├── ObtenerSimulacionRetencionUseCase.cs
│   │   ├── EliminarSimulacionRetencionUseCase.cs
│   │   └── LimpiarSimulacionesRetencionUseCase.cs
│   ├── KAN-15/
│   │   ├── ObtenerIndicadoresRetencionUseCase.cs
│   │   └── ExportarIndicadoresRetencionExcelUseCase.cs
│   └── KAN-16/
│       ├── ActualizarCriterioConTrazabilidadUseCase.cs
│       ├── ObtenerHistorialCriterioUseCase.cs
│       └── ObtenerTrazabilidadSimulacionUseCase.cs
├── Application/DTOs/TasaRetencion/
│   └── (15+ DTOs)
├── Infrastructure/Persistence/
│   ├── RepositorioConfiguracionRetencion.cs (KAN-13)
│   ├── RepositorioCriterioReferenciaRetencion.cs (KAN-13)
│   ├── RepositorioSimulacionRetencion.cs (🆕 KAN-14)
│   └── RepositorioDetalleSimulacionRetencion.cs (🆕 KAN-14)
└── Presentation/
    ├── ViewModels/
    │   ├── ConfiguracionRetencionViewModel.cs (KAN-13)
    │   └── IndicadoresRetencionViewModel.cs (KAN-15)
    └── Views/
        ├── ConfiguracionRetencionView.xaml (KAN-13)
        └── IndicadoresRetencionView.xaml (KAN-15)
```

---

## 🔧 Comandos Rápidos

```powershell
# Compilar
cd D:\Tesis\SistemaAranceles
dotnet build .\SistemaAranceles.sln

# Crear migración (KAN-14)
dotnet ef migrations add KAN_14_Retencion \
  --project .\src\Infrastructure\SistemaAranceles.Infrastructure.csproj \
  --startup-project .\src\Presentation\SistemaAranceles.Presentation.csproj

# Aplicar migración
dotnet ef database update

# Instalar OxyPlot (KAN-15)
dotnet add .\src\Presentation\SistemaAranceles.Presentation.csproj package OxyPlot.Wpf --version 2.1.2

# Git
git status
git add .
git commit -m "KAN-13: Config carrera y cohorte (4 SP)"
git push origin feature/KAN-13-config-carrera-cohorte
```

---

## 📚 Documentación Completa

- **EPICA-4-Plan-Tecnico-TasaRetencion.md** — Especificación detallada (user stories, RFs, DTOs, SQL, patterns)
- **EPICA-4-Bloqueadores-Limitantes.md** — Análisis de qué falta + plan de desbloqueo
- **contexto.md** — Contexto maestro del proyecto
- **KAN-03-modelo-db.md** — Diseño ER (tabla 29, índices, campos base)

---

## ⚡ Plan de Trabajo (Recomendado)

### **Día 1–2: KAN-13 (4 SP)**

- 🎯 Usar `feature/KAN-13-config-carrera-cohorte`
- ✅ Repos + use cases + DTOs + validators + UI + DI
- ✅ Build 0E/0W
- ✅ Commit + push

### **Día 3–6: KAN-14 (5 SP)** ⚠️ CRÍTICO

- 🎯 Usar `feature/KAN-14-simulacion-cohorte`
- ⚠️ **Día 3 mañana:** Desbloquear (entidades + repos + migración BD = 4–5 horas)
- ✅ Día 3 tarde: Merge develop, use cases
- ✅ Día 4–5: Motor + DTOs + validadores + mapeos
- ✅ Día 6: Build + tests + commit

### **Día 6–7: KAN-15 (3 SP)** (paralelo)

- 🎯 Usar `feature/KAN-15-indicadores-retencion`
- ✅ Instalar OxyPlot
- ✅ Use cases + DTOs + ViewModel + UI + gráficos
- ✅ Build + commit

### **Día 8: KAN-16 (2 SP)**

- 🎯 Usar `feature/KAN-16-edicion-trazabilidad`
- ✅ Use cases + DTOs (sin bloqueadores)
- ✅ Integración auditoria
- ✅ Build + commit

### **Día 8–10: Merge + Release**

- 🎯 develop ← KAN-13/14/15/16
- 🎯 main ← develop
- 🎯 Tag: `epica-4-release`

---

## 🛑 Bloqueadores a Resolver (KAN-14 Día 1)

Si no resuelves esto AHORA, KAN-14 se atasca:

1. ❌ **Crear entidades:** SimulacionRetencion, DetalleSimulacionRetencion (1h)
2. ❌ **Agregar DbSets:** ContextoAplicacion.cs (30 min)
3. ❌ **Crear migración:** dotnet ef migrations add KAN_14_Retencion (1h)
4. ❌ **Crear repos:** RepositorioSimulacion*, RepositorioDetalleSimulacion* (2h)
5. ✅ **Resto:** Use cases, DTOs, UI (3–4 días)

**Total bloqueadores:** 4.5 horas.  
**Impacto si no resuelves:** KAN-14 paralizado (cero BD = cero persistencia).

---

## 📞 Dudas Frecuentes

**P: ¿Puedo hacer KAN-15 antes de KAN-14?**  
R: No, necesitas datos en BD primero. Puedo hacer UI mockada, pero mejor esperar.

**P: ¿Debo crear pruebas unitarias?**  
R: No es bloqueador en KAN-13..16, pero CRÍTICO para KAN-17 (release pre-prod).

**P: ¿Cómo manejo transacciones en motor?**  
R: Usa `using (var trans = _unidadDeTrabajo.BeginTransaction())` como en LimpiarInflacionUseCase.

**P: ¿Qué de permisos RBAC?**  
R: Créalos en KAN-17 (script SQL + seed), no bloquea KAN-13..16.

**P: ¿OxyPlot es la única opción para gráficos?**  
R: Sí (ya recomendada en equipo), pero LiveCharts2 también funciona.

---

## ✅ Checklist Pre-Start

- [ ] Ramas creadas ✅
- [ ] Leíste EPICA-4-Plan-Tecnico-TasaRetencion.md
- [ ] Leíste EPICA-4-Bloqueadores-Limitantes.md
- [ ] Entiendes bloqueadores 1–4 de KAN-14
- [ ] Tienes acceso a Supabase (conexión BD)
- [ ] Build local funciona: `dotnet build .\SistemaAranceles.sln`
- [ ] Git remoto sincronizado: `git fetch origin`
- [ ] ¿Alguna duda? Revisar contexto.md

---

## 🚀 Ahora Sí: Empieza KAN-13

```bash
cd D:\Tesis\SistemaAranceles
git checkout feature/KAN-13-config-carrera-cohorte
# Crear structure de carpetas + 1er repo
# Ver EPICA-4-Plan-Tecnico-TasaRetencion.md sección "KAN-13"
# Codear 4 SP en 3 días
# Commit + push
```

**Éxito. Adelante.** 🎯
