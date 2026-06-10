# Plan de Trabajo — Épica 7: Recursos & Depreciación (KAN-24 → KAN-27)

**Fecha:** 2026-05-16
**Sprint:** S6 (May 27 – Jun 2) · **13 SP** · tier-3 · Prioridad P3
**Rama base:** `feature/KAN-24-CRUDActivos`
**Basado en:**

- `docs/depreciacion.md` — normativa NIIF/LORTI y regla de corte
- Hojas Excel `3 Recursos físicos` + `Depreciación`
- `src/Application/UseCases/RecursosFisicosDepreciacion/CU-RD-01..04.MD` (specs ya existentes, RN-94..RN-113)

---

## 0. Síntesis del dominio (qué replicamos del Excel)

| Bloque Excel                                                                 | KAN        | Traducción backend                                                          |
| ---------------------------------------------------------------------------- | ---------- | --------------------------------------------------------------------------- |
| `3 Recursos físicos` Bloque 1 (situación inicial, A4:E62)                    | **KAN-24** | CRUD `ActivoFijo` + totales por categoría                                   |
| `3 Recursos físicos` Bloques 2 y 3 (proyección unidades + monetaria, 64:183) | **KAN-25** | `InversionFutura` (cantidad proyectada por año/semestre) + matriz monetaria |
| Hoja `Depreciación` completa (E:T)                                           | **KAN-26** | `IDepreciacionService` on-demand (línea recta + acumulada)                  |
| Visualización hoja `Depreciación`                                            | **KAN-27** | `DepreciacionView` (tabla pivote por semestre, sin lógica)                  |

### Decisión normativa (de `depreciacion.md`) — **regla de corte obligatoria**

El Excel arrastra la depreciación indefinidamente (`G4=F4=H4…`) generando valores en libros negativos. Esto es un **bug del modelo original**. El nuevo backend **debe cortar**: cuando `semestres_transcurridos ≥ vida_util_anios × 2`, la depreciación del período = 0 y la acumulada queda fija en `valor_total − residual`. Cumple NIIF/NIC 16 y LORTI. **No se replica el comportamiento Excel.**

### Bugs Excel a NO portar

- `#¡REF!` en `Depreciación!11:13` → el backend nunca permite este estado (FK válida obligatoria).
- `T37 = M37 + …` referencia columna `M` vacía (debió ser `L37`) → cálculo correcto en código.
- Factor `× 0.95` hardcoded asume residual 5% → backend usa `(1 − porcentaje_residual)` parametrizado por activo.

### Decisiones abiertas (resolver antes de codificar)

1. **Inflación en KAN-25:** el Excel Bloque 3 = `cantidad × valor_unitario` (sin inflación). Pero `CU-RD-02 / RN-99` dice `Costo = Cantidad × Valor_unitario × Factor_inflación`. → **Recomendación:** exponer ambos: `monto_nominal` (Excel 1:1) y `monto_ajustado_inflacion` (consume `RepositorioInflacionAnual`, factor 1.0 si falta — RN-100). Confirmar con docente.
2. **Persistencia depreciación:** `depreciacion.md` y la guía técnica dicen _on-demand sin tabla_; `CU-RD-04` dice "el sistema guarda los resultados". → **Recomendación:** cálculo on-demand (sin tabla), "guardar" = caché en memoria/última ejecución; el Balance/Costos consumen el servicio, no una tabla. Evita estado obsoleto ante cambios (RN-113 recalcular <2s).

---

## 1. Convenciones del proyecto a respetar (Clean Architecture)

- **Domain:** entidad hereda `EntidadDominioBase` (Id `protected`, `RehidratarId`), ctor privado + ctor factory, setters privados, mutadores `CambiarX`, validación con `GuardiaDominio`, errores `DominioException`. Enum nuevo en `src/Domain/Enums`.
- **EF:** `IEntityTypeConfiguration` en `src/Infrastructure/Persistence/Configuraciones` (snake_case, `ApplyConfigurationsFromAssembly`). `DbSet` en `ContextoAplicacion`. DbContext **Scoped**; en VMs async usar scope manual (pooler 6543).
- **Soft delete:** `esta_activo=false` + `eliminado_en` + `eliminado_por_usuario_id`; query filter excluye inactivos. Hard delete solo si nunca tuvo dependencias.
- **Persistencia BD:** script idempotente en `sql/KAN24_*.sql` ejecutado en Supabase SQL Editor + marcar migración EF como aplicada (patrón `KAN05_mark_migration_applied.sql`). No depender de `dotnet ef database update` contra el pooler.
- **Use cases:** `src/Application/UseCases/RecursosFisicosDepreciacion/`, naming `…Command` / `…Query`. DTOs en `src/Application/DTOs/RecursosFisicosDepreciacion/`. Interfaces repo en `src/Application/Interfaces/Persistencia`.
- **RBAC:** permisos nuevos `RD.*`; gate en menú con `SesionActual.TienePermiso("RD.VER")`; nunca permiso por operación contra BD.
- **Auditoría:** fire-and-forget vía `IAuditoriaServicio` (no bloquea ni revierte).
- **UI:** ViewModel `[ObservableProperty]`/`[RelayCommand]`, View XAML, registro DI en `App.xaml.cs`, ítem de menú en `MainViewModel.ConstruirMenu()`.

---

## 2. KAN-24 — CRUD Activos Fijos (4 SP) · `RF-RD-01` · `CU-RD-01`

**Representa:** Bloque 1 de `3 Recursos físicos` (situación inicial).

### Domain

- Enum `CategoriaActivoFijo { MueblesEnseres, LaboratoriosEquipos, EquipoComputo, EquipoOficina }`.
- Entidad `ActivoFijo : EntidadDominioBase`:
  - `CarreraId` (FK, tenant), `Descripcion`, `Categoria`, `Cantidad`, `UnidadMedida` (default "UNI"), `ValorUnitario`, `VidaUtilAnios`, `PorcentajeResidual` (default `0.05m`), `FechaAdquisicion`.
  - Calculado: `ValorTotal => Cantidad * ValorUnitario` (nunca aceptado del cliente — **RN-95**).
  - Soft delete: `EstaActivo`, `EliminadoEn`, `EliminadoPorUsuarioId`.
  - Defaults vida útil por categoría (**RN-98**): Muebles/Laboratorios=10, Cómputo=3, Oficina=1.
  - Guards (`GuardiaDominio`): `Cantidad ≥ 0`, `ValorUnitario ≥ 0`, `VidaUtilAnios ≥ 1`, `0 ≤ PorcentajeResidual < 1`, `Descripcion` requerido, vida útil ≥ mínimo legal de la categoría (validación NIIF/LORTI de `depreciacion.md` §5).

### Application

- DTOs: `ActivoFijoDto`, `CrearActivoFijoDto`, `ActualizarActivoFijoDto`, `TotalesActivosPorCategoriaDto`.
- `IRepositorioActivoFijo`: `CrearAsync`, `ObtenerPorIdAsync`, `ListarPorCarreraAsync(carreraId, categoria?)`, `ActualizarAsync`, `EliminarLogicoAsync`, `ObtenerTotalesPorCategoriaAsync`.
- Use cases: `CrearActivoFijoCommand`, `ActualizarActivoFijoCommand`, `EliminarActivoFijoCommand` (soft), `ListarActivosFijosQuery`, `ObtenerTotalesActivosQuery` (replica `E62`).

### Infrastructure

- `ConfiguracionActivoFijo` (tabla `activo_fijo`, columnas snake_case, índice `(carrera_id, categoria)`, query filter `esta_activo=true`).
- `RepositorioActivoFijo`.
- `DbSet<ActivoFijo>` en `ContextoAplicacion`.
- `sql/KAN24_activo_fijo.sql` (DDL idempotente + `INSERT __EFMigrationsHistory`).

### Presentation

- Registro DI en `App.xaml.cs` (repo + 5 use cases + VM + View).
- Permisos `RD.VER`, `RD.CREAR`, `RD.EDITAR`, `RD.ELIMINAR` → `sql/KAN24_permisos.sql` (patrón `KAN23_permisos_tre_es.sql`: idempotente, asignar Admin/Analista/Visualizador).
- Ítem de menú "Recursos y Depreciación" en `MainViewModel` gated por `RD.VER`.

### Tests

- Unit Domain: cálculo `ValorTotal`, guards (cantidad/valor negativos), default vida útil por categoría, validación vida ≥ mínimo legal.
- Unit Application: totales por categoría y total general.

**DoD:** build 0/0, script SQL probado en Supabase, CRUD funcional, totales = `E62`, auditoría registrada.

---

## 3. KAN-25 — Inversiones Futuras (3 SP) · `RF-RD-02` · `CU-RD-02`

**Representa:** Bloques 2 (unidades) y 3 (monetario) de `3 Recursos físicos`.

### Domain

- Entidad `InversionFutura : EntidadDominioBase`: `ActivoFijoId` (FK KAN-24), `Anio`, `Semestre` (1|2), `CantidadProyectada`. Soft delete.
- Calculado: `MontoNominal = CantidadProyectada × ActivoFijo.ValorUnitario`. (Inflación → ver Decisión Abierta #1.)
- Guards: `CantidadProyectada ≥ 0`, `Semestre ∈ {1,2}`, `Anio` dentro del horizonte.

### Application

- DTOs: `InversionFuturaDto`, `MatrizInversionesDto` (filas=activos, columnas=(año,semestre)), `TotalesPorPeriodoDto`.
- `IRepositorioInversionFutura`.
- Use cases: `RegistrarInversionFuturaCommand`, `ActualizarInversionFuturaCommand`, `EliminarInversionFuturaCommand`, `ListarInversionesFuturasQuery`, `ObtenerMatrizInversionesQuery` (Bloque 3), `ObtenerTotalesPorPeriodoQuery` (fila "Inversiones Necesarias" `B183:I183`, lo consumirá Inversión Inicial — **RN-102**).
- **Horizonte parametrizable** (no hardcodear 8 columnas — **RN-101/CU-RD-02**): default = duración de la carrera, override `?aniosProyeccion=N`.
- Integración inflación: `RepositorioInflacionAnual`, factor 1.0 si falta/negativo (**RN-100**).

### Infrastructure

- `ConfiguracionInversionFutura` (tabla `inversion_futura`, FK `activo_fijo_id`, único `(activo_fijo_id, anio, semestre)`).
- `RepositorioInversionFutura`. `sql/KAN25_inversion_futura.sql`.
- **Política de borrado:** bloquear soft-delete de `ActivoFijo` si tiene inversiones futuras activas (validación en `EliminarActivoFijoCommand`); o cascada lógica si el usuario confirma. → ajustar KAN-24.

### Tests

- Unit: `MontoNominal`, matriz completa, totales por período, horizonte parametrizable, factor inflación faltante=1.0.

**DoD:** build 0/0, matriz = Bloque 3 Excel, totales por período = `B183:I183`, bloqueo de borrado validado.

---

## 4. KAN-26 — Depreciación Lineal (4 SP) · `RF-RD-03/04` · `CU-RD-03/04`

**Objetivo:** implementar el servicio de depreciación on‑demand que reproduce correctamente la lógica financiera esperada por NIIF/NIC 16 y la intención del Excel, corrigiendo los errores detectados en la hoja `Depreciación` y consumiendo las proyecciones de `3 Recursos físicos` (Bloques A/B/C) y `InversionFutura` cuando existan.

### Principios de diseño (ajustados al estado actual de la app)

- Cálculo on‑demand: `IDepreciacionService` no crea tablas persistentes; devuelve tablas y filas para consumo inmediato por vistas y otros servicios. Caché en memoria opcional (TTL corto) para evitar recálculos intensivos.
- Datos de entrada: `ActivoFijo` (KAN-24) + `InversionFutura` (KAN-25) + `RepositorioInflacionAnual`. Si `InversionFutura` aún no existe, `IDepreciacionService` deberá aceptar proyecciones derivadas de `3 Recursos físicos` (Bloque C) expuestas por un adaptador/seed temporal.
- Parametrización: `PorcentajeResidual` por activo (no hardcode 5%), `VidaUtilAnios` por activo (default por categoría). Horizonte parametrizable en semestres (por defecto: 4 años = 8 semestres o duración de la carrera si está disponible).

### Reglas y fórmulas (implementación precisa)

- `Residual = ValorTotal × PorcentajeResidual` (parametrizado).
- `DepAnual = (ValorTotal − Residual) / VidaUtilAnios`.
- `DepSemestral = DepAnual / 2`.
- Primer semestre: si el activo se adquirió en semestre P0, aplicar la regla de prorrata: primer registro puede ser `DepSemestral` (medio año) si la política lo exige; el servicio debe exponer la opción `ProrratearPrimerSemestre` (default true) para compatibilidad con Excel (F=E/2).
- Corte obligatorio (RN-106): para cada unidad de adquisición el contador de semestres crece; cuando `semestres_transcurridos ≥ VidaUtilAnios × 2` → `depPeriodo = 0` y `acumulada` = `ValorTotal − Residual` (congelada). Nunca permitir valor en libros negativo.

### Inversiones futuras y proyecciones (integración con KAN-25 / hoja 3)

- Cada registro de `InversionFutura` representa una nueva adquisición que inicia su propio conteo de semestres desde su `Anio/Semestre` de compra.
- Si KAN-25 aún no está disponible, `IDepreciacionService` ofrecerá un adaptador que consume la matriz monetaria del Bloque C de `3 Recursos físicos` (columnas por año que ya incorporan factor de inflación en la hoja Excel). El adaptador mapeará cada columna año→semestres (ej.: año X → semestres S1,S2) y dividirá monto por unidad adquirida en semestres usando la vida útil del activo.

### API mínima propuesta

- `Task<TablaDepreciacionDto> ObtenerTablaPeriodoAsync(int carreraId, Semestre periodoDesde, Semestre periodoHasta, bool prorratearPrimerSemestre = true)`
- `Task<DepreciacionAcumuladaDto> ObtenerAcumuladaAsync(int carreraId, Semestre periodo, CancellationToken ct = default)`
- `Task<TotalesDepreciacionDto> ObtenerTotalesPorCategoriaAsync(int carreraId, Semestre periodoDesde, Semestre periodoHasta)`

### Salidas y formato

- Filas por `ActivoFijo` (incluye valor inicial, residual, vida, y columnas semestrales con `depPeriodo` o `depAcumulada` según la vista solicitada).
- Totales por categoría y fila `TOTAL` replicando la fila TOTAL de la hoja Excel pero con la regla de corte aplicada.

### Tests y criterios de aceptación

- Validar contra ejemplos numéricos de `depreciacion.md` §4 (mobiliario 10a, cómputo 3a, vehículo 5a) incluyendo el caso de corte.
- Validar integración: cuando existan proyecciones en `3 Recursos físicos` Bloque C, que los montos proyectados se incorporen en las columnas semestrales correctas y contribuyan a la depreciación acumulada (usar factor de inflación desde `RepositorioInflacionAnual`).
- Casos límite: activo con vida útil 1 año (debe comportarse correctamente), activo congelado tras fin de vida útil, múltiples adquisiciones del mismo `ActivoFijo` en diferentes semestres.
- Performance: recálculo de tabla para una carrera con ~200 activos + proyecciones debe <2s en máquina de desarrollo (RN-113).

**DoD:** `IDepreciacionService` implementado y testeado; ejemplos de `depreciacion.md` pasan; adaptador para consumir Bloque C disponible; documentado el mapping año→semestres.

---

## 5. KAN-27 — Vista Depreciación (2 SP) · `RF-RD-04`

**Objetivo:** construir la vista que consume `IDepreciacionService` y presenta la tabla pivote por semestres de la hoja `Depreciación`, sin realizar cálculos financieros en el cliente.

### Requisitos de la vista

- Componente: `DepreciacionView.xaml` + `DepreciacionViewModel` (CommunityToolkit.Mvvm).
- Columnas fijas izquierda: `Descripción`, `Valor inicial`, `Valor residual`, `Vida útil`.
- Columnas dinámicas: una columna por semestre en el horizonte solicitado (encabezados como `2023-S1`, `2023-S2`, …). Generadas en runtime según la petición a `IDepreciacionService`.
- Modes/Tabs: "Depreciación del período" (dep semestral) y "Depreciación acumulada" (acumulada hasta periodo).
- Agrupamiento: filas agrupables por `Categoria` con subtotales por grupo y fila `TOTAL`.
- Filtros: rango de años/semestres, categoría (ComboBox), carrera.
- Visual: resaltar activos cuya vida útil se agota dentro del horizonte; tooltip con detalle de adquisiciones futuras que aportan a la fila.
- Export: botón Exportar → ClosedXML que reproduce la estructura de la hoja `Depreciación` (E–T / N–T según modo).

### Integración y robustez frente a errores de Excel

- `DepreciacionViewModel` debe solicitar la tabla ya preparada por `IDepreciacionService` y renderizarla tal cual; no debe tener fórmulas ni lógica financiera propia.
- Corregir inconsistencias del Excel: no replicar arrastre de medio año indefinido; no usar referencias rotas; no depender de celdas hardcodeadas en el Excel. La vista documentará diferencias esperadas con el Excel (regla de corte, prorrata, inflación aplicada).

### Tests y aceptación

- UI tests: validar columnas generadas dinámicamente, agrupamiento y subtotales.
- Export tests: el archivo XLSX exportado debe contener los mismos totales que la vista y respetar la regla de corte.

**DoD:** `DepreciacionView` funcional navegable desde menú, obtiene datos de `IDepreciacionService`, exporta XLSX y pasa pruebas de integración.

---

## 6. Orden y dependencias

```
KAN-24 (CRUD base) ──► KAN-25 (FK a ActivoFijo) ──► KAN-26 (consume 24+25) ──► KAN-27 (consume 26)
```

Estrictamente secuencial. Cada KAN cierra con build 0/0 + tests verdes + script SQL probado en Supabase **antes** de iniciar el siguiente.

| KAN    | SP  | Rama sugerida                         | Entregable clave                  |
| ------ | --- | ------------------------------------- | --------------------------------- |
| KAN-24 | 4   | `feature/KAN-24-CRUDActivos` (actual) | CRUD + permisos `RD.*` + menú     |
| KAN-25 | 3   | `feature/KAN-25-InversionesFuturas`   | Matriz + totales por período      |
| KAN-26 | 4   | `feature/KAN-26-DepreciacionLineal`   | `IDepreciacionService` + tests §4 |
| KAN-27 | 2   | `feature/KAN-27-VistaDepreciacion`    | `DepreciacionView`                |

---

## 7. Riesgos

- **Regla de corte:** divergencia intencional vs Excel. Documentar en el informe de épica que los totales NO cuadran con el Excel viejo a partir del fin de vida útil (es correcto, el Excel estaba mal).
- **Inflación KAN-25:** decisión #1 pendiente con docente; bloquea forma final de `MontoProyectado`.
- **Pooler 6543:** scope manual de DbContext en VMs async (matriz/tabla pueden ser consultas grandes — usar `WHERE col = ANY(@ids)`, sin N+1).
- **Lock de binario:** cerrar app antes de build de Presentation.
- **Migraciones:** no aplicar EF contra pooler; script SQL idempotente + marcar `__EFMigrationsHistory`.
