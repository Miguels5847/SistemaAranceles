# Diagnóstico del Sistema — SistemaAranceles

> Análisis completo · Fecha: 2026-04-20 · Rama: `feature/KAN-14-simulacion-cohorte`

---

## 📋 Estado de Épicas

| Épica     | Descripción                  | Sprint          | SP         | Estado          | KAN tickets |
| --------- | ---------------------------- | --------------- | ---------- | --------------- | ----------- |
| 1         | 🏗️ Setup & Arquitectura      | S1 Mar27–Abr5   | 14         | ✅ Completa     | KAN-01..05  |
| 2         | 👤 Usuarios & RBAC           | S1-2 Abr6–18    | 14         | ✅ Completa     | KAN-06..09  |
| 3         | 📈 Inflación                 | S2 Abr19–25     | 10         | ✅ Completa     | KAN-10..12  |
| 4         | 📊 Tasa Retención            | S3 Abr26–May6   | 14         | 🟡 En curso     | KAN-13..16  |
| 5         | 🎓 Estudiantes & Docentes    | S4 May7–16      | 13         | ❌ Pendiente    | KAN-17..19  |
| 6         | 💰 Sueldos & Planta Central  | S5 May17–26     | 14         | ❌ Pendiente    | KAN-20..23  |
| 7         | 🏢 Recursos & Depreciación   | S6 May27–Jun2   | 13         | ❌ Pendiente    | KAN-24..27  |
| 8         | 🔧 Mantenimiento & Inversión | S6 Jun3–8       | 10         | ❌ Pendiente    | KAN-28..31  |
| 9         | 📉 Demanda e Ingresos        | S7 Jun9–16      | 11         | ❌ Pendiente    | KAN-32..35  |
| 10        | 📋 Costos & Gastos           | S7 Jun17–20     | 7          | ❌ Pendiente    | KAN-36..37  |
| 11        | 📐 Análisis Financiero       | S8-9 Jun21–Jul5 | 30         | ❌ Pendiente    | KAN-38..44  |
| 12        | 🏦 Financiamiento & Balance  | S9 Jul6–9       | 8          | ❌ Pendiente    | KAN-45..46  |
| 13        | 📄 Reportes & CES            | S9 Jul10–16     | 13         | ❌ Pendiente    | KAN-47..49  |
| 14        | ✅ Cierre & Validación       | Buffer Jul17–25 | 14         | ❌ Pendiente    | KAN-50..53  |
| **TOTAL** |                              |                 | **175 SP** | **3/14 épicas** |             |

---

## ✅ Lo que está correcto

- **Schema completo**: Las 42 tablas existen en BD y coinciden con diagramas DC-01 a DC-04.
- **RBAC funcional**: Roles (Admin/Analista/Visualizador), 18 permisos, asignaciones coherentes con el código.
- **Permisos canonicalizados** (KAN-11 SQL): Sin duplicados por acentos/mayúsculas; extensión `unaccent` disponible.
- **inflacion_anual / inflacion_proyectada**: Timestamps `timestamptz` correctos; 14 registros BCE (2012–2025) + 6 proyecciones lineales hasta 2031 funcionando.
- **PK compuestas**: `rol_permiso`, `usuario_rol`, `usuario_permiso_override` correctamente definidas.
- **Auditoría desacoplada**: Falla de auditoría no revierte operación principal — patrón correcto.
- **Épicas 1–3 completas**: Login, CRUD usuarios, 11 use cases de inflación, RBAC granular — código y BD alineados.
- **BD adelantada al código**: Todas las tablas de épicas 4–13 (28 tablas) ya existen en producción.
- **No hay triggers**: La auditoría es 100% application-level, evita lógica oculta en BD.
- **KAN-14 validado**: la simulación de retención y graduación ya corre en `feature/KAN-14-simulacion-cohorte`; el siguiente paso real es KAN-15/KAN-16.

---

## ⚠️ Inconsistencias detectadas

### Hallazgo 1 — TEXT timestamps en 24 tablas (bloqueante para épicas 4–13)

- **Evidencia**: Bloque D2 — columnas `creado_en`, `actualizado_en`, `eliminado_en` con `data_type: "text"`.
- **Justificación**: EF Core mapea `DateTime` a `timestamptz`. Con `text`, cualquier `INSERT`/`SELECT` con filtro de fecha falla en runtime o produce resultados incorrectos.
- **Impacto**: 🔴 ALTO — bloquea el inicio de KAN-13.
- **Recomendación**: Ejecutar `fix_timestamps.sql` antes de KAN-13.

**Tablas afectadas:**

```
usuario, sesion_usuario (emitido_en/expira_en/revocado_en), rol, permiso,
carrera, escenario_proyeccion, cargo_facultad, cargo_planta_central,
configuracion_arancel, configuracion_carga_docente, configuracion_retencion,
criterio_referencia_retencion, detalle_proyeccion_estudiantes,
detalle_simulacion_retencion, item_material_insumo, periodo_academico,
presupuesto_institucional, proyeccion_cargo_facultad, proyeccion_cargo_planta_central,
proyeccion_estudiantes, proyeccion_material_insumo, proyeccion_requerimiento_docente,
resumen_proyeccion_financiera, simulacion_retencion
```

```sql
-- fix_timestamps.sql — patrón por tabla
ALTER TABLE {tabla}
  ALTER COLUMN creado_en    TYPE timestamptz USING creado_en::timestamptz,
  ALTER COLUMN actualizado_en TYPE timestamptz USING NULLIF(actualizado_en,'')::timestamptz,
  ALTER COLUMN eliminado_en TYPE timestamptz USING NULLIF(eliminado_en,'')::timestamptz;
-- Para sesion_usuario:
ALTER TABLE sesion_usuario
  ALTER COLUMN emitido_en  TYPE timestamptz USING emitido_en::timestamptz,
  ALTER COLUMN expira_en   TYPE timestamptz USING expira_en::timestamptz,
  ALTER COLUMN revocado_en TYPE timestamptz USING NULLIF(revocado_en,'')::timestamptz;
```

---

### Hallazgo 2 — `auditoria_log.evento_en` sin zona horaria

- **Evidencia**: Bloque D2 → `timestamp without time zone` (KAN-09 SQL convirtió de `text` a `timestamp` pero no a `timestamptz`).
- **Justificación**: Supabase opera en UTC; EF Core con `DateTimeKind.Utc` puede producir offsets incorrectos al leer.
- **Impacto**: 🟡 MEDIO — logs muestran hora incorrecta fuera de UTC.
- **Recomendación**:

```sql
ALTER TABLE auditoria_log
  ALTER COLUMN evento_en TYPE timestamptz USING evento_en AT TIME ZONE 'UTC';
```

---

### Hallazgo 3 — `esta_activo` como `integer` en 5 tablas (herencia SQLite)

- **Evidencia**: Bloque A1 → `cargo_facultad`, `cargo_planta_central`, `carrera`, `configuracion_arancel`, `configuracion_carga_docente` con `esta_activo: int4`.
- **Justificación**: SQLite no tiene `boolean`; tablas migradas sin conversión. EF Core mapea `bool` → `boolean` PG; el tipo `int4` puede causar errores de comparación.
- **Impacto**: 🟡 MEDIO — error en runtime al filtrar por `esta_activo` en esas entidades.
- **Recomendación**:

```sql
ALTER TABLE cargo_facultad        ALTER COLUMN esta_activo TYPE boolean USING esta_activo::boolean;
ALTER TABLE cargo_planta_central  ALTER COLUMN esta_activo TYPE boolean USING esta_activo::boolean;
ALTER TABLE carrera               ALTER COLUMN esta_activo TYPE boolean USING esta_activo::boolean;
ALTER TABLE configuracion_arancel ALTER COLUMN esta_activo TYPE boolean USING esta_activo::boolean;
ALTER TABLE configuracion_carga_docente ALTER COLUMN esta_activo TYPE boolean USING esta_activo::boolean;
```

---

### Hallazgo 4 — `balance_proyectado.cuadra_balance` es `integer`

- **Evidencia**: Bloque A1 → `cuadra_balance: int4`; diagramas DC-04 y secuencia 11 lo tratan como flag binario.
- **Justificación**: Si la entidad C# declara `bool`, el ORM falla al leer.
- **Impacto**: 🟡 MEDIO — bloquea KAN-46.
- **Recomendación**:

```sql
ALTER TABLE balance_proyectado
  ALTER COLUMN cuadra_balance TYPE boolean USING cuadra_balance::boolean;
```

---

### Hallazgo 5 — 3 usuarios con `estado='Activo'` pero `esta_activo=false`

- **Evidencia**: Bloque B3 → ids 3, 4, 9 con `estado:"Activo"`, `esta_activo:false`.
- **Justificación**: `EliminarUsuarioUseCase` setea `esta_activo=false` (eliminación lógica) pero no cambia `estado` a `"Inactivo"`. Si el código de login chequea solo `estado`, estos usuarios pueden autenticarse.
- **Impacto**: 🟡 MEDIO — bypass potencial de autenticación.
- **Recomendación**:
  1. En `EliminarUsuarioUseCase`: añadir `estado = "Inactivo"` junto a `esta_activo = false`.
  2. Corregir registros existentes:

```sql
UPDATE usuario SET estado = 'Inactivo' WHERE id IN (3, 4, 9) AND esta_activo = false;
```

---

### Hallazgo 6 — `INF.ED` viola convención de nombres de permisos

- **Evidencia**: Bloque B1 → código `INF.ED`; resto del sistema usa `*.EDITAR` (`US.EDITAR`, `CA.EDITAR`, `CFG.EDITAR`).
- **Justificación**: Si el código C# verifica `"INF.EDITAR"`, el permiso nunca coincide → Analista siempre denegado en edición de inflación.
- **Impacto**: 🟡 MEDIO — bug silencioso en RBAC para módulo Inflación.
- **Recomendación**:

```sql
UPDATE permiso SET codigo = 'INF.EDITAR' WHERE codigo = 'INF.ED';
```

Verificar constante en código: buscar `"INF.ED"` y reemplazar por `"INF.EDITAR"`.

---

### Hallazgo 7 — Administrador sin permisos `CFG.*` y `REP.*`

- **Evidencia**: Bloque B2 → Admin tiene 14 permisos; ausentes: `CFG.VER`, `CFG.EDITAR`, `REP.VER`, `REP.EXPORTAR`. Paradójicamente, Visualizador sí tiene `CFG.VER`.
- **Impacto**: 🟡 MEDIO — Admin no puede acceder a Configuración ni exportar reportes.
- **Recomendación**:

```sql
INSERT INTO rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM rol r, permiso p
WHERE r.nombre = 'Administrador'
  AND p.codigo IN ('CFG.VER','CFG.EDITAR','REP.VER','REP.EXPORTAR')
  AND NOT EXISTS (
    SELECT 1 FROM rol_permiso rp WHERE rp.rol_id = r.id AND rp.permiso_id = p.id
  );
```

---

### Hallazgo 8 — `CA.ELIMINAR` no existe como permiso

- **Evidencia**: Bloque B1 → Carreras solo tiene `CA.VER`, `CA.CREAR`, `CA.EDITAR`.
- **Justificación**: Cuando se implemente CRUD completo de Carreras (KAN-13), el use case de eliminar no puede verificar permiso.
- **Impacto**: 🟠 BAJO-MEDIO — afecta cuando se implemente KAN-13.
- **Recomendación**:

```sql
INSERT INTO permiso (codigo, modulo_nombre, accion_nombre, esta_activo)
VALUES ('CA.ELIMINAR', 'Carreras', 'eliminar', true);

INSERT INTO rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id FROM rol r, permiso p
WHERE r.nombre = 'Administrador' AND p.codigo = 'CA.ELIMINAR';
```

---

### Hallazgo 9 — RLS deshabilitado en 40/42 tablas

- **Evidencia**: Bloque C4 → solo `__EFMigrationsHistory` e `item_material_insumo` tienen RLS activo.
- **Justificación**: Supabase expone tablas vía PostgREST. Sin RLS, cualquier cliente con `anon key` accede directamente a todas las tablas, bypasseando el RBAC de la app.
- **Impacto**: 🔴 ALTO (seguridad) — si el backend usa `service_role`, el riesgo es bajo; si usa `anon`/`authenticated`, es crítico.
- **Recomendación**: Confirmar que el backend usa `service_role` exclusivamente. Si no, habilitar RLS con `deny all` para `anon` y políticas permisivas para el rol del backend en tablas sensibles (`usuario`, `sesion_usuario`, `auditoria_log`).

---

### Hallazgo 10 — `item_material_insumo` con política `deny_all`

- **Evidencia**: Bloque C3 → `deny_all_item_material_insumo` con `qual: false`; RLS habilitado.
- **Justificación**: La tabla tiene 15 columnas y es parte del módulo Materiales (KAN-34). Si el backend no usa `service_role`, será completamente inaccesible al implementar KAN-34.
- **Impacto**: 🟡 MEDIO — bloquea KAN-34 si backend no usa `service_role`.
- **Recomendación**: Verificar con `SHOW session_replication_role;` si el cliente de Supabase usa `service_role`. Si es así, la política no aplica.

---

## ❌ Gaps: código vs BD vs épicas

| Módulo                   | Épica | KAN        | Tablas BD                                                                            | Use Cases .cs                                                                                                                                                                                                              | Diagramas   |
| ------------------------ | ----- | ---------- | ------------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------- |
| Carreras (CRUD)          | 1/4   | KAN-13     | ✅ `carrera`                                                                         | ❌                                                                                                                                                                                                                         | ✅          |
| Escenarios (CRUD)        | 4     | KAN-13     | ✅ `escenario_proyeccion`                                                            | ❌                                                                                                                                                                                                                         | ✅          |
| Retención (simulación)   | 4     | KAN-13..16 | ✅ 4 tablas                                                                          | ✅ `CrearSimulacionRetencionUseCase`, `ActualizarSimulacionRetencionUseCase`, `ObtenerSimulacionRetencionUseCase`, `EliminarSimulacionRetencionUseCase`, `LimpiarSimulacionesRetencionUseCase`, `MotorSimulacionRetencion` | ✅ DS-04    |
| Estudiantes / Docentes   | 5     | KAN-17..19 | ✅ 4 tablas                                                                          | ❌ `ProyectarEstudiantesUseCase`                                                                                                                                                                                           | ✅ DS-05    |
| Sueldos Facultad         | 6     | KAN-20..21 | ✅ `cargo_facultad`, `proyeccion_cargo_facultad`                                     | ❌                                                                                                                                                                                                                         | ✅ DS-06    |
| Sueldos Planta Central   | 6     | KAN-22..23 | ✅ `cargo_planta_central`, `proyeccion_cargo_planta_central`                         | ❌                                                                                                                                                                                                                         | ✅ DS-06    |
| Activos Fijos            | 7     | KAN-24..26 | ✅ `activo_fijo`, `categoria_activo`                                                 | ❌ `GestionActivosUseCase`                                                                                                                                                                                                 | ✅ DS-07    |
| Activos Diferidos        | 8     | KAN-31     | ✅ `activo_diferido`                                                                 | ❌                                                                                                                                                                                                                         | ✅ DC-03    |
| Mantenimiento / Capital  | 8     | KAN-28..30 | ✅ `servicio_basico_mantenimiento`, `configuracion_capital_trabajo`                  | ❌                                                                                                                                                                                                                         | ✅ DC-03    |
| Materiales / Presupuesto | 9     | KAN-34..35 | ✅ `item_material_insumo`, `proyeccion_material_insumo`, `presupuesto_institucional` | ❌                                                                                                                                                                                                                         | ✅ DC-03    |
| Config Arancel           | 9     | KAN-32     | ✅ `configuracion_arancel`                                                           | ❌                                                                                                                                                                                                                         | ✅ DC-04    |
| Costos y Gastos          | 10    | KAN-36..37 | ✅ `costo_gasto_periodo`                                                             | ❌ `ConsolidarCostosUseCase`                                                                                                                                                                                               | ✅ DS-08    |
| Análisis Financiero      | 11    | KAN-38..44 | ✅ `configuracion_tmr`, `resultado_analisis_financiero`                              | ❌ `CalcularIndicadoresUseCase`, `AjustarArancelAutomaticoUseCase`                                                                                                                                                         | ✅ DS-09/10 |
| Financiamiento           | 12    | KAN-45     | ✅ `configuracion_financiamiento`, `amortizacion_cuota`                              | ❌                                                                                                                                                                                                                         | ✅ DC-04    |
| Balance Proyectado       | 12    | KAN-46     | ✅ `balance_proyectado`, `resumen_proyeccion_financiera`                             | ❌ `GenerarBalanceUseCase`                                                                                                                                                                                                 | ✅ DS-11    |
| Reportes / CES           | 13    | KAN-47..49 | ✅ (datos en tablas existentes)                                                      | ❌ `GenerarReporteCESUseCase`                                                                                                                                                                                              | ✅ DS-11    |

> Los gaps son **esperados** — BD está adelantada al código. No hay inconsistencia de diseño, solo implementación pendiente. En retención, la base funcional de KAN-14 ya está cerrada; faltan KAN-15/KAN-16.

---

## 🚨 Riesgos técnicos

| #   | Riesgo                                                             | Probabilidad              | Impacto | Bloquea                 |
| --- | ------------------------------------------------------------------ | ------------------------- | ------- | ----------------------- |
| R1  | Runtime error timestamps TEXT en 24 tablas                         | Alta                      | Alto    | KAN-13 (inicio Épica 4) |
| R2  | Bypass RBAC vía PostgREST sin RLS                                  | Media                     | Alto    | Seguridad global        |
| R3  | `INF.ED` ≠ `INF.EDITAR` en código → Analista denegado en Inflación | Alta                      | Medio   | RF-IN-02 activo         |
| R4  | Usuarios eliminados pasan validación de login (`estado=Activo`)    | Media                     | Medio   | RF-US-02                |
| R5  | `item_material_insumo` deny_all bloquea KAN-34                     | Alta (si no service_role) | Medio   | KAN-34                  |
| R6  | `esta_activo int4` en 5 tablas → error ORM al leer                 | Alta                      | Medio   | KAN-13..23              |

---

## 🛠️ Plan de acción

### 🔴 Prioridad Alta — Ejecutar ANTES de KAN-13

| Acción                              | Script / Archivo                              | Descripción                                  |
| ----------------------------------- | --------------------------------------------- | -------------------------------------------- |
| Fix timestamps                      | `sql/FIX_timestamps_text_to_timestamptz.sql`  | 24 tablas con TEXT→timestamptz               |
| Fix booleanos                       | `sql/FIX_boolean_esta_activo.sql`             | 5 tablas SQLite heredadas                    |
| Fix `cuadra_balance`                | incluir en FIX_boolean                        | `balance_proyectado.cuadra_balance` int→bool |
| Fix `auditoria_log`                 | `sql/FIX_auditoria_evento_en_timestamptz.sql` | timestamp→timestamptz                        |
| Fix `INF.ED`                        | `sql/FIX_permiso_inf_editar.sql`              | Renombrar código + verificar código C#       |
| Fix usuarios inconsistentes         | `sql/FIX_usuarios_estado_inactivo.sql`        | ids 3,4,9: estado→Inactivo                   |
| Fix código `EliminarUsuarioUseCase` | `EliminarUsuarioUseCase.cs`                   | Setear `estado = "Inactivo"` al eliminar     |

### 🟡 Prioridad Media — Esta semana

| Acción                              | Descripción                                                                  |
| ----------------------------------- | ---------------------------------------------------------------------------- |
| Completar permisos Admin            | Insertar `CFG.VER`, `CFG.EDITAR`, `REP.VER`, `REP.EXPORTAR` en `rol_permiso` |
| Crear `CA.ELIMINAR`                 | Nuevo permiso + asignación a Administrador                                   |
| Verificar `service_role` en backend | Confirmar que la cadena de conexión usa `service_role` no `anon`             |

### 🟢 Prioridad Baja — Antes de Go-Live

| Acción                                  | Descripción                                                           |
| --------------------------------------- | --------------------------------------------------------------------- |
| Habilitar RLS en tablas sensibles       | `usuario`, `sesion_usuario`, `auditoria_log` con deny-all para `anon` |
| Revisar `item_material_insumo` deny_all | Alinear política con rol real del backend                             |
| Implementar épicas 4–13                 | Según roadmap — BD ya está lista                                      |

---

## 📅 Kanban Completo

### Épica 1 — 🏗️ Setup & Arquitectura · S1 Mar27–Abr5 · 14SP · ✅ COMPLETA

| KAN    | Descripción                                               | Req | Tier    | P   | SP  | Estado |
| ------ | --------------------------------------------------------- | --- | ------- | --- | --- | ------ |
| KAN-01 | Crear solución WPF + 4 proyectos Clean Arch               | —   | backend | P1  | 3   | ✅     |
| KAN-02 | Instalar NuGet (EF Core SQLite BCrypt ClosedXML MVVM)     | —   | backend | P1  | 1   | ✅     |
| KAN-03 | Diseñar todas tablas DB + migrations EF Core              | —   | db      | P1  | 5   | ✅     |
| KAN-04 | Domain Entities (Usuario Rol Carrera Inflación Retención) | —   | backend | P1  | 3   | ✅     |
| KAN-05 | DbContext + Repository genérico                           | —   | db      | P1  | 2   | ✅     |

### Épica 2 — 👤 Usuarios & RBAC · S1-2 Abr6–18 · 14SP · ✅ COMPLETA

| KAN    | Descripción                      | Req      | Tier             | P   | SP  | Estado |
| ------ | -------------------------------- | -------- | ---------------- | --- | --- | ------ |
| KAN-06 | CRUD Usuarios                    | RF-US-01 | backend+frontend | P1  | 5   | ✅     |
| KAN-07 | Login + BCrypt + sesión por rol  | RF-US-02 | backend          | P1  | 3   | ✅     |
| KAN-08 | Menú dinámico por rol + permisos | RF-US-03 | frontend         | P1  | 4   | ✅     |
| KAN-09 | AuditLog acciones críticas       | RF-US-04 | db               | P1  | 2   | ✅     |

### Épica 3 — 📈 Inflación · S2 Abr19–25 · 10SP · ✅ COMPLETA

| KAN    | Descripción                       | Req         | Tier             | P   | SP  | Estado |
| ------ | --------------------------------- | ----------- | ---------------- | --- | --- | ------ |
| KAN-10 | CRUD inflación + validación       | RF-IN-01/02 | backend+frontend | P1  | 3   | ✅     |
| KAN-11 | Regresión lineal + gráfico        | RF-IN-04    | backend          | P1  | 5   | ✅     |
| KAN-12 | Solo lectura módulos dependientes | RF-IN-03    | backend          | P1  | 2   | ✅     |

### Épica 4 — 📊 Tasa Retención · S3 Abr26–May6 · 14SP · ⏳ PRÓXIMA

> ⚠️ **Bloqueada** por Hallazgos 1, 3, 6 — ejecutar scripts de prioridad alta primero.

| KAN    | Descripción                      | Req      | Tier             | P   | SP  | Estado |
| ------ | -------------------------------- | -------- | ---------------- | --- | --- | ------ |
| KAN-13 | Config carrera y cohorte         | RF-TR-01 | backend+frontend | P1  | 4   | ⏳     |
| KAN-14 | Simulación cohorte ciclo a ciclo | RF-TR-02 | backend          | P1  | 5   | ⏳     |
| KAN-15 | Indicadores Ret% y Titu%         | RF-TR-02 | frontend         | P1  | 3   | ⏳     |
| KAN-16 | Edición con trazabilidad         | RF-TR-03 | db               | P1  | 2   | ⏳     |

### Épica 5 — 🎓 Estudiantes · S4 May7–16 · 13SP · ❌ PENDIENTE

| KAN    | Descripción                        | Req      | Tier             | P   | SP  | Estado |
| ------ | ---------------------------------- | -------- | ---------------- | --- | --- | ------ |
| KAN-17 | Proyección estudiantes por período | RF-ES-01 | backend          | P2  | 5   | ❌     |
| KAN-18 | Horas docencia por paralelos       | RF-ES-02 | backend          | P2  | 4   | ❌     |
| KAN-19 | Desglose docentes por tipo         | RF-ES-03 | backend+frontend | P2  | 4   | ❌     |

### Épica 6 — 💰 Sueldos & Planta Central · S5 May17–26 · 14SP · ❌ PENDIENTE

| KAN    | Descripción                   | Req      | Tier     | P   | SP  | Estado |
| ------ | ----------------------------- | -------- | -------- | --- | --- | ------ |
| KAN-20 | Cargos facultad + beneficios  | RF-SP-01 | backend  | P2  | 5   | ❌     |
| KAN-21 | Peso proporcional + inflación | RF-SP-02 | backend  | P2  | 4   | ❌     |
| KAN-22 | Planta central distribución   | RF-SP-03 | backend  | P2  | 3   | ❌     |
| KAN-23 | Consolidado sueldos           | RF-SP-04 | frontend | P2  | 2   | ❌     |

### Épica 7 — 🏢 Recursos & Depreciación · S6 May27–Jun2 · 13SP · ❌ PENDIENTE

| KAN    | Descripción         | Req      | Tier     | P   | SP  | Estado |
| ------ | ------------------- | -------- | -------- | --- | --- | ------ |
| KAN-24 | CRUD activos fijos  | RF-RD-01 | backend  | P3  | 4   | ❌     |
| KAN-25 | Inversiones futuras | RF-RD-02 | backend  | P3  | 3   | ❌     |
| KAN-26 | Depreciación lineal | RF-RD-03 | backend  | P3  | 4   | ❌     |
| KAN-27 | Vista depreciación  | RF-RD-04 | frontend | P3  | 2   | ❌     |

### Épica 8 — 🔧 Mantenimiento & Inversión · S6 Jun3–8 · 10SP · ❌ PENDIENTE

| KAN    | Descripción                    | Req      | Tier    | P   | SP  | Estado |
| ------ | ------------------------------ | -------- | ------- | --- | --- | ------ |
| KAN-28 | Servicios + mantenimiento      | RF-MI-01 | backend | P3  | 3   | ❌     |
| KAN-29 | Capital trabajo                | RF-MI-02 | backend | P3  | 2   | ❌     |
| KAN-30 | Inversión inicial total        | RF-MI-03 | backend | P3  | 3   | ❌     |
| KAN-31 | Activos diferidos amortización | RF-MI-04 | backend | P3  | 2   | ❌     |

### Épica 9 — 📉 Demanda e Ingresos · S7 Jun9–16 · 11SP · ❌ PENDIENTE

| KAN    | Descripción              | Req         | Tier    | P   | SP  | Estado |
| ------ | ------------------------ | ----------- | ------- | --- | --- | ------ |
| KAN-32 | Config arancel           | RF-DI-01    | backend | P3  | 3   | ❌     |
| KAN-33 | Ingresos totales         | RF-DI-02    | backend | P3  | 3   | ❌     |
| KAN-34 | Materiales con inflación | RF-DI-03    | backend | P3  | 3   | ❌     |
| KAN-35 | Presupuestos varios      | RF-DI-04/05 | backend | P3  | 2   | ❌     |

### Épica 10 — 📋 Costos & Gastos · S7 Jun17–20 · 7SP · ❌ PENDIENTE

| KAN    | Descripción              | Req      | Tier    | P   | SP  | Estado |
| ------ | ------------------------ | -------- | ------- | --- | --- | ------ |
| KAN-36 | Tabla consolidada costos | RF-CG-01 | backend | P3  | 5   | ❌     |
| KAN-37 | Recepción arancel óptimo | RF-CG-02 | backend | P3  | 2   | ❌     |

### Épica 11 — 📐 Análisis Financiero · S8-9 Jun21–Jul5 · 30SP · ❌ PENDIENTE

> KAN-41 es **blocker** — desbloquea KAN-36, KAN-37, KAN-46.

| KAN    | Descripción                    | Req      | Tier                | P   | SP  | Estado |
| ------ | ------------------------------ | -------- | ------------------- | --- | --- | ------ |
| KAN-38 | Estado P&G                     | RF-AF-01 | backend             | P4  | 4   | ❌     |
| KAN-39 | Flujo fondos neto              | RF-AF-02 | backend             | P4  | 4   | ❌     |
| KAN-40 | TIR VAN TMR                    | RF-AF-03 | backend             | P4  | 5   | ❌     |
| KAN-41 | **Arancel óptimo (bisección)** | RF-AF-04 | backend **blocker** | P4  | 8   | ❌     |
| KAN-42 | Punto equilibrio               | RF-AF-05 | backend             | P4  | 3   | ❌     |
| KAN-43 | Periodo recuperación           | RF-AF-06 | backend             | P4  | 2   | ❌     |
| KAN-44 | Dashboard financiero           | RF-AF-07 | frontend            | P4  | 4   | ❌     |

### Épica 12 — 🏦 Financiamiento & Balance · S9 Jul6–9 · 8SP · ❌ PENDIENTE

| KAN    | Descripción                   | Req      | Tier    | P   | SP  | Estado |
| ------ | ----------------------------- | -------- | ------- | --- | --- | ------ |
| KAN-45 | Financiamiento + amortización | RF-FB-01 | backend | P4  | 4   | ❌     |
| KAN-46 | Balance proyectado            | RF-FB-02 | backend | P4  | 4   | ❌     |

### Épica 13 — 📄 Reportes & CES · S9 Jul10–16 · 13SP · ❌ PENDIENTE

| KAN    | Descripción      | Req      | Tier     | P   | SP  | Estado |
| ------ | ---------------- | -------- | -------- | --- | --- | ------ |
| KAN-47 | Informe CES      | RF-FB-03 | frontend | P5  | 5   | ❌     |
| KAN-48 | Exportación XLSX | RF-FB-04 | backend  | P5  | 4   | ❌     |
| KAN-49 | Exportación PDF  | RF-FB-04 | backend  | P5  | 4   | ❌     |

### Épica 14 — ✅ Cierre & Validación · Buffer Jul17–25 · 14SP · ❌ PENDIENTE

| KAN    | Descripción                       | Bloqueante | P   | SP  | Estado |
| ------ | --------------------------------- | ---------- | --- | --- | ------ |
| KAN-50 | Validación vs Excel institucional | blocker    | P5  | 5   | ❌     |
| KAN-51 | Pruebas funcionales               | —          | P5  | 3   | ❌     |
| KAN-52 | Correcciones finales              | blocker    | P5  | 3   | ❌     |
| KAN-53 | Documentación técnica             | —          | P5  | 3   | ❌     |

---

## 🔗 Mapa de dependencias críticas

```
fix_timestamps.sql ──────────────────────────────┐
fix_booleans.sql ────────────────────────────────┤
fix_INF.EDITAR ──────────────────────────────────┤
                                                  ▼
                                             KAN-13 (Épica 4)
                                                  │
                                    ┌─────────────┴─────────────┐
                                    ▼                           ▼
                               KAN-17..19                  KAN-20..23
                               (Estudiantes)               (Sueldos)
                                    │                           │
                                    └──────────┬────────────────┘
                                               ▼
                                          KAN-24..35
                                    (Activos, Mant, Demanda)
                                               │
                                               ▼
                                          KAN-36..37
                                        (Costos y Gastos)
                                               │
                                               ▼
                                        KAN-41 [BLOCKER]
                                     Arancel óptimo (bisección)
                                               │
                              ┌────────────────┼────────────────┐
                              ▼                ▼                ▼
                         KAN-38..40        KAN-42..44      KAN-45..46
                         (TIR/VAN/TMR)   (PE, Recup,       (Financ,
                                          Dashboard)        Balance)
                                               │
                                               ▼
                                         KAN-47..49
                                      (Reportes & CES)
                                               │
                                               ▼
                                         KAN-50..53
                                        (Cierre & Val)
```

---

_Última actualización: 2026-04-14 | Rama: `feature/KAN-11-rbac-sesion`_
