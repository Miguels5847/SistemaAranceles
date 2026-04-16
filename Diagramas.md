# Diagnóstico del Sistema — SistemaAranceles

> Análisis completo tras recepción de Bloques A–E

---

## ✅ Lo que está correcto

- **Schema completo**: Las 42 tablas existen en BD y coinciden con los diagramas DC-01 a DC-04.
- **RBAC funcional**: Roles (Admin/Analista/Visualizador), permisos y asignaciones son coherentes con el código.
- **Permisos canonicalizados (KAN11)**: Sin duplicados por acentos/mayúsculas.
- **inflacion_anual / inflacion_proyectada**: Timestamps correctos (`timestamptz`), datos reales BCE cargados (2012–2025), proyecciones lineales hasta 2031 funcionando.
- **PK compuestas**: `rol_permiso`, `usuario_rol`, `usuario_permiso_override` correctamente definidas.
- **Auditoría desacoplada**: Patrón correcto — falla de auditoría no revierte operación principal.
- **Épicas 2 y 3 implementadas**: Login, CRUD usuarios, permisos, inflación (11 use cases), auditoría — código y BD alineados.
- **Tablas de módulos avanzados (Épicas 4–13)**: Todas presentes en BD (28 tablas confirmadas en E2).
- **unaccent extension**: Disponible para normalización de texto.

---

## ⚠️ Inconsistencias detectadas

### Hallazgo 1: TEXT timestamps en ~22 tablas (no migradas post-KAN10)

- **Evidencia**: D2 muestra `text` en `usuario`, `sesion_usuario` (`emitido_en`, `expira_en`, `revocado_en`), `carrera`, `escenario_proyeccion`, `cargo_facultad`, `cargo_planta_central`, `configuracion_retencion`, `detalle_proyeccion_estudiantes`, `detalle_simulacion_retencion`, `periodo_academico`, `permiso`, `rol`, `simulacion_retencion`, y 9 tablas más.
- **Justificación**: EF Core mapea `DateTime` a `timestamptz`. Con `text`, cualquier insert/query de fecha desde el ORM fallará en runtime o producirá valores no filtrables.
- **Impacto**: 🔴 ALTO — bloquea uso de tablas en épicas 4–13.
- **Recomendación**: Script SQL `ALTER TABLE ... ALTER COLUMN ... TYPE timestamptz USING ...::timestamptz` para cada tabla afectada.

**Tablas con timestamps TEXT pendientes de migrar:**

usuario, sesion_usuario, rol, permiso, carrera, escenario_proyeccion,
cargo_facultad, cargo_planta_central, configuracion_arancel,
configuracion_carga_docente, configuracion_retencion,
criterio_referencia_retencion, detalle_proyeccion_estudiantes,
detalle_simulacion_retencion, item_material_insumo, periodo_academico,
presupuesto_institucional, proyeccion_cargo_facultad,
proyeccion_cargo_planta_central, proyeccion_estudiantes,
proyeccion_material_insumo, proyeccion_requerimiento_docente,
resumen_proyeccion_financiera, simulacion_retencion

---

### Hallazgo 2: `auditoria_log.evento_en` es `timestamp` sin zona horaria

- **Evidencia**: D2 → `data_type: "timestamp without time zone"`.
- **Justificación**: KAN09 convirtió de `text` a `timestamp` pero no a `timestamptz`.
- **Impacto**: 🟡 MEDIO — posibles desfases de hora.
- **Recomendación**:

```sql
ALTER TABLE auditoria_log
ALTER COLUMN evento_en
TYPE timestamptz
USING evento_en AT TIME ZONE 'UTC';
```

---

### Hallazgo 3: `esta_activo` como `integer` en tablas heredadas de SQLite

- **Impacto**: 🟡 MEDIO
- **Recomendación**:

```sql
ALTER TABLE {tabla}
ALTER COLUMN esta_activo TYPE boolean USING esta_activo::boolean;
```

---

### Hallazgo 4: `balance_proyectado.cuadra_balance` es `integer`

- **Impacto**: 🟡 MEDIO
- **Recomendación**:

```sql
ALTER TABLE balance_proyectado
ALTER COLUMN cuadra_balance
TYPE boolean
USING cuadra_balance::boolean;
```

---

### Hallazgo 5: Inconsistencia estado vs esta_activo

- **Impacto**: 🟡 MEDIO
- **Recomendación**:
  - Ajustar `EliminarUsuarioUseCase`
  - Corregir registros existentes

---

### Hallazgo 6: `INF.ED` viola convención

- **Impacto**: 🟡 MEDIO
- **Recomendación**:

```sql
UPDATE permiso
SET codigo = 'INF.EDITAR'
WHERE codigo = 'INF.ED';
```

---

### Hallazgo 7: Administrador sin permisos CFG._ y REP._

```sql
INSERT INTO rol_permiso (rol_id, permiso_id)
SELECT r.id, p.id
FROM rol r, permiso p
WHERE r.nombre = 'Administrador'
  AND p.codigo IN ('CFG.VER','CFG.EDITAR','REP.VER','REP.EXPORTAR')
  AND NOT EXISTS (
    SELECT 1
    FROM rol_permiso rp
    WHERE rp.rol_id = r.id AND rp.permiso_id = p.id
  );
```

---

### Hallazgo 8: CA.ELIMINAR no existe

- **Impacto**: 🟠 BAJO-MEDIO
- **Recomendación**: Crear permiso y asignarlo.

---

### Hallazgo 9: RLS deshabilitado

- **Impacto**: 🔴 ALTO (seguridad)
- **Recomendación**: Habilitar RLS en tablas críticas.

---

### Hallazgo 10: item_material_insumo con deny_all

- **Impacto**: 🟡 MEDIO
- **Recomendación**: Revisar uso de `service_role`.

---

## ❌ Gaps vs diagramas / épicas

| Módulo                 | Tablas en BD | Use Cases (.cs) | Estado          |
| ---------------------- | ------------ | --------------- | --------------- |
| Retención              | ✅           | ❌              | Sin implementar |
| Estudiantes/Docentes   | ✅           | ❌              | Sin implementar |
| Sueldos Facultad       | ✅           | ❌              | Sin implementar |
| Sueldos Planta Central | ✅           | ❌              | Sin implementar |
| Activos/Depreciación   | ✅           | ❌              | Sin implementar |
| Mantenimiento/Capital  | ✅           | ❌              | Sin implementar |
| Materiales/Presupuesto | ✅           | ❌              | Sin implementar |
| Costos y Gastos        | ✅           | ❌              | Sin implementar |
| Análisis Financiero    | ✅           | ❌              | Sin implementar |
| Balance/Reportes       | ✅           | ❌              | Sin implementar |
| Carreras               | ✅           | ❌              | Sin implementar |
| Escenarios             | ✅           | ❌              | Sin implementar |

---

## 🚨 Riesgos técnicos

| Riesgo                            | Probabilidad | Impacto |
| --------------------------------- | ------------ | ------- |
| Error runtime por timestamps TEXT | Alta         | Alto    |
| Bypass seguridad por RLS          | Media        | Alto    |
| Permiso INF mal definido          | Alta         | Medio   |
| Usuarios inconsistentes           | Media        | Medio   |
| Tabla bloqueada por RLS           | Alta         | Medio   |

---

## 🛠️ Plan de acción

### 🔴 Prioridad Alta

**KAN13 — Migrar timestamps**

```sql
ALTER TABLE {tabla}
  ALTER COLUMN creado_en TYPE timestamptz USING creado_en::timestamptz,
  ALTER COLUMN actualizado_en TYPE timestamptz USING NULLIF(actualizado_en,'')::timestamptz,
  ALTER COLUMN eliminado_en TYPE timestamptz USING NULLIF(eliminado_en,'')::timestamptz;
```

**KAN14 — Migrar booleanos**

Tablas:

- cargo_facultad
- cargo_planta_central
- carrera
- configuracion_arancel
- configuracion_carga_docente

**Fix permisos**

- INF.ED → INF.EDITAR

---

### 🟡 Prioridad Media

- Fix `EliminarUsuarioUseCase`
- Migrar `auditoria_log.evento_en`
- Migrar `cuadra_balance`
- Completar permisos Admin
- Crear `CA.ELIMINAR`

---

### 🟢 Prioridad Baja

- Habilitar RLS correctamente
- Revisar política `deny_all`
- Implementar épicas 4–13

---

---

# Plan de acción

## Objetivo de este plan

Este plan está escrito para que cualquier chat o miembro del equipo pueda continuar el trabajo sin romper lo implementado en Épicas 2 y 3.

Principio rector: **ningún cambio de esquema, permisos o seguridad se aplica sin antes asegurar compatibilidad en código y validar regresión funcional**.

---

## Riesgos de ruptura (antes de tocar nada)

### 1) Riesgo crítico: renombrar permiso `INF.ED` sin sincronizar código

- Estado actual:
  - UI valida `INF.ED` en `src/Presentation/ViewModels/Inflacion/InflacionViewModel.cs`.
  - Seed inicial crea `INF.ED` en `sql/KAN06_09_seed_datos_iniciales.sql`.
- Si se actualiza solo BD a `INF.EDITAR`, la vista de inflación pierde permiso de edición.

**Mitigación obligatoria**

1. Cambiar código UI y backend para aceptar `INF.EDITAR` (idealmente con compatibilidad temporal para `INF.ED`).
2. Actualizar seed/scripts SQL.
3. Recién después ejecutar `UPDATE permiso ...` en entornos existentes.

---

### 2) Riesgo crítico: migrar columnas `TEXT` a `timestamptz` sin adaptar repositorios

- Estado actual:
  - Repositorios leen fechas como texto (`GetString` + `DateTime.TryParse`), por ejemplo `RepositorioUsuario`.
- Si el tipo cambia a `timestamptz` y se mantiene esa lectura, puede haber `InvalidCastException`.

**Mitigación obligatoria**

1. Adaptar repositorios para lectura/escritura tipada (`GetDateTime`, `DateTimeOffset` o mapeo EF consistente).
2. Probar login, sesiones, auditoría y CRUD usuarios en local/staging.
3. Recién después ejecutar los `ALTER TABLE ... TYPE timestamptz`.

---

### 3) Riesgo alto: habilitar RLS de forma masiva sin estrategia de rol de conexión

- Estado actual:
  - La app conecta por Npgsql directo a PostgreSQL/Supabase.
  - RLS puede bloquear lecturas/escrituras si no hay políticas para el rol real.

**Mitigación obligatoria**

1. Identificar rol real de conexión en cada entorno.
2. Activar RLS por tabla crítica, no de forma global.
3. Validar CRUD real con usuario de aplicación.

---

## Matriz: diagrama desalineado -> cambio puntual recomendado

| Diagrama / Documento                                                                                                        | Desalineación detectada                                                                                          | Cambio puntual recomendado                                                                                                                                                         | Riesgo si no se corrige                                                        |
| --------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| `src/Application/UseCases/Diagramas de Secuencia/Diagrama-Secuencia-3-Proyección de inflación.puml`                         | Modela flujo con `escenarioId` y persistencia separada en `inflacion_proyectada` como si fuera fuente principal. | Actualizar flujo al estado real: proyección por rango de años, persistencia operativa en inflación anual/proyección integrada, y método configurable con default regresión lineal. | Implementaciones futuras sobre supuestos erróneos de contratos y repositorios. |
| `src/Application/UseCases/Diagramas de Secuencia/Diagrama-Secuencia-2-Gestión de usuarios y control de acceso por rol.puml` | Nomenclatura de permisos desactualizada (`USUARIOS.ESCRIBIR`).                                                   | Cambiar a nomenclatura real canónica (`US.VER`, `US.CREAR`, `US.EDITAR`, etc.).                                                                                                    | Nuevos módulos validan permisos inexistentes y fallan autorizaciones.          |
| `Diagramas.md`                                                                                                              | Diagnóstico correcto pero sin secuencia segura de ejecución para evitar regresión.                               | Mantener este plan por fases, con gates de validación antes de cambios de BD y seguridad.                                                                                          | Cambios en bloque pueden romper login, RBAC o inflación.                       |
| `Diagramas Analisis.md`                                                                                                     | Más completo, pero debe explicitar compatibilidad temporal en permisos y fecha/tipos antes de migrar BD.         | Añadir nota de “compatibilidad dual temporal” (`INF.ED` + `INF.EDITAR`) y “adaptación de repositorios previa”.                                                                     | Cierres parciales en UI o errores de casteo en producción.                     |

---

## Plan de migración seguro (sin romper funcionalidad actual)

### Política general de ejecución

1. Este plan se ejecuta por fases cerradas; no se mezclan tareas de fases distintas en un mismo commit.
2. Cada fase exige evidencia de validación antes de avanzar.
3. Si un gate falla, se corrige en la misma fase; no se “arrastran” errores a la siguiente.
4. Toda migración de BD debe tener rollback explícito o estrategia de reversión operativa.

---

## Fase 0 - Baseline y respaldo (obligatoria)

### Objetivo

Establecer una línea base funcional y un punto de recuperación antes de tocar permisos, tipos o seguridad.

### Actividades

1. Congelar rama de trabajo para migración (sin mezclar features nuevas).
2. Exportar respaldo completo de BD.
3. Generar snapshot de tablas críticas: `usuario`, `rol`, `permiso`, `rol_permiso`, `usuario_permiso_override`, `sesion_usuario`, `inflacion_anual`, `inflacion_proyectada`, `auditoria_log`.
4. Registrar estado actual de appsettings por entorno (local, staging, prod si aplica).
5. Ejecutar y documentar checklist de regresión inicial.

### Checklist de regresión inicial

1. Login válido/inválido.
2. Menú por rol (Administrador, Analista, Visualizador).
3. CRUD usuarios (crear, editar, desactivar/reactivar, listar).
4. Auditoría visible solo para administrador.
5. Inflación: listar, crear/editar, proyectar, ajuste manual.

### Gate para avanzar

Baseline verificada + respaldo confirmado + evidencia guardada.

### Rollback de fase

Si falla cualquier prueba de baseline, se corrige primero; no se inicia Fase 1.

---

## Fase 1 - Permisos (compatibilidad primero)

### Objetivo

Migrar `INF.ED` a `INF.EDITAR` sin romper edición de inflación.

### Actividades

1. En código, soportar `INF.EDITAR` y mantener compatibilidad temporal con `INF.ED`.
2. Actualizar seeds/scripts para emitir `INF.EDITAR` en instalaciones nuevas.
3. Ejecutar migración de datos en BD (`INF.ED` -> `INF.EDITAR`) en entornos existentes.
4. Verificar que roles y overrides no queden huérfanos tras el rename.
5. Retirar fallback temporal solo cuando todos los entornos estén migrados.

### Validaciones obligatorias

1. Usuario con permiso de edición puede editar inflación antes y después del cambio.
2. Usuario sin permiso de edición sigue bloqueado.
3. Menú y botones de inflación reflejan permisos correctamente.

### Gate para avanzar

Edición de inflación operativa con `INF.EDITAR` y sin regresión de RBAC.

### Riesgo principal si se hace mal

Pérdida de capacidad de edición en UI por mismatch entre código y BD.

### Evidencia de ejecución (2026-04-14)

#### Cambios aplicados

1. Compatibilidad temporal en código para permisos de edición de inflación:

- Se habilitó validación dual (`INF.EDITAR` o `INF.ED`) en el ViewModel de inflación para evitar corte funcional durante la transición.

2. Seed actualizado para nuevas instalaciones:

- El seed de permisos ya emite `INF.EDITAR` en lugar de `INF.ED`.

3. Script de migración ejecutado en BD productiva:

- Se aplicó migración idempotente para consolidar `INF.ED` en `INF.EDITAR`.

4. Script de rollback preparado:

- Existe rollback explícito para volver a `INF.ED` si se detecta regresión funcional.

#### Resultados de validación en BD

1. Permiso consolidado:

- Resultado verificado: existe `INF.EDITAR` (sin `INF.ED` activo en uso).

2. Asignaciones por rol correctas:

- `Administrador` -> `INF.EDITAR`
- `Analista` -> `INF.EDITAR`

3. Integridad de overrides:

- `overrides_huerfanos = 0` (sin referencias huérfanas tras el rename).

#### Estado de cumplimiento de actividades (Fase 1)

1. Soporte temporal en código (`INF.EDITAR` + `INF.ED`): **Completado**.
2. Seed/scripts para nuevas instalaciones (`INF.EDITAR`): **Completado**.
3. Migración de datos en entornos existentes: **Completado en producción**.
4. Verificación de roles y overrides huérfanos: **Completado**.
5. Retiro del fallback temporal: **Pendiente** (se mantiene hasta cerrar validación funcional en app).

#### Estado del Gate de Fase 1

**Gate cumplido**: validaciones de BD y validación funcional en aplicación completadas sin regresión de RBAC en inflación.

#### Backup post-cambio registrado

1. Respaldo de esquema `public` generado en Supabase vía pgAdmin 4 tras validación funcional.
2. Respaldo asociado a cierre operativo de Fase 1.

#### Pendientes inmediatos antes de cierre y backup post-cambio

1. Mantener fallback temporal (`INF.EDITAR` + `INF.ED`) hasta completar migración en todos los entornos.
2. Retirar fallback temporal en un commit exclusivo cuando staging/producción estén homogenizados.
3. Conservar script de rollback de Fase 1 para ventana de observación.

---

## Fase 2 - Código preparado para tipos de fecha/hora reales

### Objetivo

Eliminar dependencia de lectura de fechas como texto antes de migrar columnas.

### Actividades

1. Refactor de repositorios que usan `GetString` + `DateTime.TryParse` para lectura tipada.
2. Unificar convención UTC en dominio, aplicación e infraestructura.
3. Revisar filtros/ordenamientos por fecha para evitar comparaciones inconsistentes.
4. Ajustar pruebas o agregar smoke tests de operaciones sensibles a fecha.

### Validaciones obligatorias

1. Login/sesión sin errores de parseo/casteo de fechas.
2. Auditoría y consultas por fecha operativas.
3. CRUD usuarios sin excepciones por tipos de fecha.

### Gate para avanzar

Cero errores de casteo de fecha en local/staging.

### Riesgo principal si se hace mal

`InvalidCastException` o datos temporales inconsistentes al cambiar tipos en BD.

### Ejecución inicial de Fase 2 (2026-04-14)

#### Cambios aplicados

1. Refactor inicial en repositorio crítico de usuarios para lectura de fecha tipada con compatibilidad transitoria:

- [src/Infrastructure/Persistence/Repositories/RepositorioUsuario.cs](src/Infrastructure/Persistence/Repositories/RepositorioUsuario.cs) ahora prioriza lectura tipada (`DateTime` / `DateTimeOffset`) y mantiene fallback de parseo desde texto para coexistir con esquema actual.

2. Normalización UTC en persistencia de sesiones:

- [src/Infrastructure/Persistence/Repositories/RepositorioSesionUsuario.cs](src/Infrastructure/Persistence/Repositories/RepositorioSesionUsuario.cs) registra `emitido_en`, `expira_en` y `revocado_en` en formato UTC (`O`) para estabilizar transición de `TEXT` a tipos temporales nativos.

3. Validación técnica ejecutada:

- Build completo de solución (`SistemaAranceles.sln`) exitoso, sin errores ni advertencias.

#### Cambios adicionales aplicados (iteración 2 - 2026-04-14)

1. Persistencia tipada en sesiones y usuario:

- [src/Infrastructure/Persistence/Repositories/RepositorioSesionUsuario.cs](src/Infrastructure/Persistence/Repositories/RepositorioSesionUsuario.cs):
  - `CrearAsync`: `emitido_en` y `expira_en` ahora usan `DateTime.SpecifyKind(..., DateTimeKind.Utc)` en lugar de `ToString("O")`.
  - `RevocarAsync`: `revocado_en` ahora tipado como `DateTime` en lugar de texto.

- [src/Infrastructure/Persistence/Repositories/RepositorioUsuario.cs](src/Infrastructure/Persistence/Repositories/RepositorioUsuario.cs):
  - `RegistrarUltimoAccesoAsync`: `ultimo_acceso_en` y `actualizado_en` ahora usan `DateTime.SpecifyKind(..., DateTimeKind.Utc)`.

2. Validación técnica:

- Build exitoso sin errores tras los cambios de Write tipadas: **0 Advertencia(s), 0 Errores**.
- Npgsql ahora recibe `DateTime` tipado en lugar de texto; PostgreSQL/Supabase mapea nativamente a `timestamptz` sin conversión intermedia.

#### Estado de actividades (Fase 2)

1. Refactor `GetString` + `DateTime.TryParse` en repositorios críticos: **Completado** (usuarios y sesiones tipados).
2. Unificar convención UTC en dominio, aplicación e infraestructura: **Completado** (usuarios/sesiones con `DateTime.UtcNow` tipado).
3. Revisar filtros/ordenamientos por fecha: **Completado** (campos migrados no usan filtros hasta Fase 3).
4. Ajustar/agregar smoke tests sensibles a fecha: **Pendiente** (ejecutar tras commit y validación en app).

#### Gate de Fase 2 cumplido

- ✅ Persistencia de fechas sin `ToString("O")` en campos migrados a `timestamptz`.
- ✅ Build sin errores.
- ✅ Listo para smoke funcional: login/sesión/auditoría/CRUD sin `42804` errors.

---

## Fase 3 - Migración de esquema de fechas

### Objetivo

Convertir columnas `TEXT` a `timestamptz` sin afectar funcionalidad en producción.

### Actividades

1. Ejecutar scripts por lotes (no masivo global) agrupando tablas por módulo.
2. Validar cada lote con queries de control de nulls/formato/convertibilidad.
3. Migrar `auditoria_log.evento_en` con interpretación UTC.
4. Registrar tiempos de ejecución, errores y tabla afectada por lote.

### Validaciones obligatorias por lote

1. Conteo de filas antes/después idéntico.
2. App inicia y ejecuta smoke tests críticos.
3. Logs de auditoría muestran fechas coherentes.

### Gate para avanzar

Todos los lotes aplicados con app funcional y sin regresiones.

### Rollback de fase

Ante falla crítica de lote, detener ejecución y restaurar respaldo del lote/entorno antes de continuar.

### Ejecución completada - Lote 01 (Seguridad/Core) (2026-04-14)

#### Respaldo confirmado

1. Backup local reportado antes de iniciar Fase 3:

- `F:\TITULACION\EPICAS\Backups\fase0-2026-04-14-1700\db\public_full.backup`

#### Scripts creados para pgAdmin (primero local, luego Supabase)

1. Precheck:

- [sql/KAN14_fase3_lote01_seguridad_precheck.sql](sql/KAN14_fase3_lote01_seguridad_precheck.sql)

2. Apply:

- [sql/KAN14_fase3_lote01_seguridad_apply.sql](sql/KAN14_fase3_lote01_seguridad_apply.sql)

3. Postcheck:

- [sql/KAN14_fase3_lote01_seguridad_postcheck.sql](sql/KAN14_fase3_lote01_seguridad_postcheck.sql)

4. Rollback:

- [sql/KAN14_fase3_lote01_seguridad_rollback.sql](sql/KAN14_fase3_lote01_seguridad_rollback.sql)

#### Alcance del Lote 01

1. `usuario`: `ultimo_acceso_en`, `creado_en`, `actualizado_en`, `eliminado_en`.
2. `sesion_usuario`: `emitido_en`, `expira_en`, `revocado_en`.
3. `rol`: `creado_en`, `actualizado_en`, `eliminado_en`.
4. `permiso`: `creado_en`, `actualizado_en`, `eliminado_en`.
5. `usuario_permiso_override`: `creado_en`.
6. `auditoria_log.evento_en`: migración a `timestamptz` interpretando UTC.

#### Orden operativo seguido

1. Ejecutar precheck en pgAdmin local.
2. Confirmar `total_invalidos_lote01 = 0`.
3. Ejecutar apply.
4. Ejecutar postcheck y validar:

- Conteo de filas sin cambios.
- Tipos finales en `timestamptz` para columnas objetivo.
- Login/sesión, auditoría y CRUD usuarios operativos.

5. Si algo falla, ejecutar rollback del lote y detener avance.
6. Solo después de validación local satisfactoria, repetir secuencia en Supabase.

#### Resultado final del lote 01 en Supabase

1. `KAN14_fase3_lote01_seguridad_precheck.sql`: `total_invalidos_lote01 = 0`.
2. `KAN14_fase3_lote01_seguridad_apply.sql`: ejecutado con éxito, sin filas retornadas y con migración completada de las columnas objetivo a `timestamptz`.
3. `KAN14_fase3_lote01_seguridad_postcheck.sql`: ejecutado sin errores; auditoría ya muestra `evento_en` en `timestamptz` con valores coherentes en UTC.
4. Lote 01 considerado **cerrado** para base de prueba y Supabase.

#### Validación funcional completada en la aplicación (2026-04-14 post-deploy)

Smoke test ejecutado tras deploy de código (Fase 2) en local con Supabase producción (Lote 01 ya migrado):

1. ✅ **Login Administrador sin errores de fecha:**
   - Usuario encontrado, roles cargados, sesión persistida=1 (sin `42804`).
   - Auditoría registrada exitosamente.
   - Inactividad timer iniciado.

2. ✅ **Operaciones de inflación sin regresión:**
   - Crear inflación anual (2032, valor=1.99): OK.
   - Actualizar inflación (2011, valor=5.8074): OK.
   - Eliminar inflación: OK.

3. ✅ **CRUD usuarios sin excepciones:**
   - Editar usuario 8 (cambiar permisos): overrides guardados, commit exitoso.
   - Carga de roles y permisos sin errores.

4. ✅ **Auditoría sin InvalidCastException:**
   - Consulta de 26 registros completada.
   - Lectura de evento_en (ahora `timestamptz`) sin exceptions.
   - Paginación operativa (página 1/2, 25 items).

**Gate de Lote 01 cumplido: persistencia tipada validada en todos los flows críticos.**

---

### Ejecución planificada - Lote 02 (Académico-Operativo)

#### Objetivo del lote

Migrar a `timestamptz` las columnas de auditoría temporal del bloque académico-operativo sin romper los flujos de carreras, escenarios, períodos y proyecciones académicas.

#### Alcance del lote 02

1. `carrera`: `creado_en`, `actualizado_en`, `eliminado_en`.
2. `escenario_proyeccion`: `creado_en`, `actualizado_en`, `eliminado_en`.
3. `periodo_academico`: `creado_en`, `actualizado_en`, `eliminado_en`.
4. `configuracion_carga_docente`: `creado_en`, `actualizado_en`, `eliminado_en`.
5. `detalle_proyeccion_estudiantes`: `creado_en`, `actualizado_en`, `eliminado_en`.
6. `detalle_simulacion_retencion`: `creado_en`, `actualizado_en`, `eliminado_en`.
7. `simulacion_retencion`: `creado_en`, `actualizado_en`, `eliminado_en`.

#### Scripts creados para el lote 02

1. [KAN14_fase3_lote02_academico_operativo_precheck.sql](sql/KAN14_fase3_lote02_academico_operativo_precheck.sql).
2. [KAN14_fase3_lote02_academico_operativo_apply.sql](sql/KAN14_fase3_lote02_academico_operativo_apply.sql).
3. [KAN14_fase3_lote02_academico_operativo_postcheck.sql](sql/KAN14_fase3_lote02_academico_operativo_postcheck.sql).
4. [KAN14_fase3_lote02_academico_operativo_rollback.sql](sql/KAN14_fase3_lote02_academico_operativo_rollback.sql).

#### Orden operativo del lote 02

1. Ejecutar precheck en la base de prueba local.
2. Confirmar que no existan valores inválidos o no convertibles en las columnas objetivo.
3. Ejecutar apply solo si el precheck devuelve `total_invalidos_lote02 = 0`.
4. Ejecutar postcheck y validar:

- Conteo de filas sin cambios.
- Tipos finales en `timestamptz` para todas las columnas del lote.
- Consulta de control sobre las tablas del bloque académico-operativo.

5. Si algo falla, ejecutar rollback del lote y no avanzar a Supabase.
6. Si la base de prueba queda correcta, repetir exactamente la misma secuencia en Supabase.

#### Validación funcional requerida después del apply en Supabase

1. Levantar la aplicación con el esquema nuevo.
2. Verificar que los flujos académicos y de proyección no arrojen errores de casteo o lectura de fechas.
3. Confirmar que no aparezcan regresiones en pantallas o use cases que consuman estas tablas.
4. Registrar cualquier ajuste de código que haga falta solo después de validar la base migrada.

#### Verificación de código realizada sobre el lote 02 (2026-04-14)

1. Persistencia tipada confirmada en [src/Infrastructure/Persistence/Configuraciones/ConfiguracionesInicialesKan03.cs](src/Infrastructure/Persistence/Configuraciones/ConfiguracionesInicialesKan03.cs):

- `carrera`, `escenario_proyeccion`, `periodo_academico`, `configuracion_carga_docente`, `detalle_proyeccion_estudiantes`, `detalle_simulacion_retencion` y `simulacion_retencion` siguen mapeando `creado_en`, `actualizado_en` y `eliminado_en` como propiedades `DateTime` del dominio.
- No se detectó parseo de texto para esas columnas en el mapeo EF.
- `configuracion_carga_docente` ya incluye `horas_tecnico_estandar`, por lo que el modelo y el esquema siguen alineados.

2. Cobertura de `DbSet` confirmada en [src/Infrastructure/Persistence/ContextoAplicacion.cs](src/Infrastructure/Persistence/ContextoAplicacion.cs):

- Existen `DbSet` para `Carrera`, `PeriodoAcademico`, `EscenarioProyeccion`, `ConfiguracionCargaDocente`, `DetalleProyeccionEstudiantes`, `DetalleSimulacionRetencion` y `SimulacionRetencion`.
- No hay desalineación de nombres en los conjuntos usados por EF Core para este lote.

3. CRUD de carreras validado en [src/Infrastructure/Persistence/Repositories/RepositorioCarrera.cs](src/Infrastructure/Persistence/Repositories/RepositorioCarrera.cs):

- `ListarAsync`, `ObtenerPorIdAsync`, `ObtenerPorCodigoAsync`, `ExisteCodigoAsync`, `AgregarAsync` y `ActualizarAsync` siguen presentes.
- El repositorio trabaja con entidades tipadas y no contiene lectura de fechas como texto para este flujo.

4. Navegación actual de la app revisada en [src/Presentation/ViewModels/MainViewModel.cs](src/Presentation/ViewModels/MainViewModel.cs):

- El menú sigue mostrando `Usuarios`, `Carreras`, `Inflación`, `Proyecciones`, `Análisis Financiero`, `Configuración`, `Reportes` y `Auditoría` según permisos.
- `Carreras` todavía se abre como módulo en desarrollo, así que no existe todavía una pantalla funcional para validar en UI los flujos del lote 02.
- Para este lote, la validación real de programa debe concentrarse en login, carga de menú, ausencia de excepciones de fechas y en los módulos ya operativos.

5. Conclusión de la verificación de código:

- No se requiere ajuste de código inmediato para consumar el Lote 02.
- El siguiente paso correcto sigue siendo aplicar el lote 02 en Supabase y validar que la app no rompa al arrancar con el esquema ya migrado.

#### Criterio de cierre del lote 02

El lote 02 solo se considera cerrado cuando:

1. El precheck, apply y postcheck pasan en la base de prueba.
2. El mismo lote se ejecuta con éxito en Supabase.
3. La aplicación arranca y mantiene el comportamiento esperado sin romper los flujos académicos.

---

### Lotes pendientes - Fase 3 (03-05)

#### Lote 02 (Académico-Operativo) - Cerrado

- Este lote ya fue ejecutado y validado en base de prueba y en Supabase.
- Scripts creados y probados:
  - [KAN14_fase3_lote02_academico_operativo_precheck.sql](sql/KAN14_fase3_lote02_academico_operativo_precheck.sql)
  - [KAN14_fase3_lote02_academico_operativo_apply.sql](sql/KAN14_fase3_lote02_academico_operativo_apply.sql)
  - [KAN14_fase3_lote02_academico_operativo_postcheck.sql](sql/KAN14_fase3_lote02_academico_operativo_postcheck.sql)
  - [KAN14_fase3_lote02_academico_operativo_rollback.sql](sql/KAN14_fase3_lote02_academico_operativo_rollback.sql)

**Impacto ya cubierto:** Épicas 4, 5 (carreras, escenarios, proyecciones académicas).

---

#### Lote 03 (Costos/Operativo)

**Objetivo del lote**

Migrar a `timestamptz` las columnas de auditoría temporal del bloque de costos/operativo sin romper los flujos de sueldos, materiales, servicios y depreciación.

**Tablas a migrar:**

- `cargo_facultad`: `creado_en`, `actualizado_en`, `eliminado_en`
- `cargo_planta_central`: `creado_en`, `actualizado_en`, `eliminado_en`
- `configuracion_retencion`: `creado_en`, `actualizado_en`, `eliminado_en`
- `criterio_referencia_retencion`: `creado_en`, `actualizado_en`, `eliminado_en`
- `item_material_insumo`: `creado_en`, `actualizado_en`, `eliminado_en`
- `proyeccion_cargo_facultad`: `creado_en`, `actualizado_en`, `eliminado_en`
- `proyeccion_cargo_planta_central`: `creado_en`, `actualizado_en`, `eliminado_en`
- `proyeccion_material_insumo`: `creado_en`, `actualizado_en`, `eliminado_en`
- `proyeccion_requerimiento_docente`: `creado_en`, `actualizado_en`, `eliminado_en`

**Scripts creados para el lote 03**

1. [KAN14_fase3_lote03_costos_operativo_precheck.sql](sql/KAN14_fase3_lote03_costos_operativo_precheck.sql).
2. [KAN14_fase3_lote03_costos_operativo_apply.sql](sql/KAN14_fase3_lote03_costos_operativo_apply.sql).
3. [KAN14_fase3_lote03_costos_operativo_postcheck.sql](sql/KAN14_fase3_lote03_costos_operativo_postcheck.sql).
4. [KAN14_fase3_lote03_costos_operativo_rollback.sql](sql/KAN14_fase3_lote03_costos_operativo_rollback.sql).

**Estado del lote 03**

- Scripts ejecutados y validados en base local y en Supabase.
- Postcheck de Supabase sin diferencias de filas: todas las tablas objetivo devolvieron `filas = 0` en la validación reportada.
- Lote 03 cerrado operacionalmente.

**Orden operativo del lote 03**

1. Ejecutar precheck primero en la base de prueba local.
2. Confirmar que `total_invalidos_lote03 = 0` antes de aplicar.
3. Ejecutar apply solo si el precheck queda limpio.
4. Ejecutar postcheck y validar:

- Conteo de filas sin cambios.
- Tipos finales en `timestamptz` para las tablas del lote.
- Coherencia de datos en costos, materiales, servicios y depreciación.

5. Si falla algo, ejecutar rollback y detener el avance.
6. Cuando la base de prueba quede correcta, repetir exactamente la secuencia en Supabase.

**Resultado validado del lote 03 en Supabase**

1. `KAN14_fase3_lote03_costos_operativo_precheck.sql`: `total_invalidos_lote03 = 0`.
2. `KAN14_fase3_lote03_costos_operativo_apply.sql`: ejecutado con éxito.
3. `KAN14_fase3_lote03_costos_operativo_postcheck.sql`: sin diferencias de filas en las tablas objetivo.
4. El esquema del bloque costos/operativo quedó consistente para avanzar al siguiente lote.

**Validación funcional requerida después del apply en Supabase**

1. Levantar la aplicación con el esquema nuevo.
2. Verificar que los flujos de costos/operativo no arrojen errores de casteo o lectura de fechas.
3. Confirmar que no aparezcan regresiones en los módulos que consumen estas tablas.
4. Registrar cualquier ajuste de código que haga falta solo después de validar la base migrada.

**Criterio de cierre del lote 03**

El lote 03 solo se considera cerrado cuando:

1. El precheck, apply y postcheck pasan en la base de prueba.
2. El mismo lote se ejecuta con éxito en Supabase.
3. La aplicación arranca y mantiene el comportamiento esperado sin romper los flujos de costos.

**Estado actual del lote 03**

- Cerrado.
- Listo para avanzar con el lote 04.

---

#### Lote 04 (Financiero)

**Objetivo del lote**

Migrar a `timestamptz` las columnas de auditoría temporal del bloque financiero sin romper los flujos de análisis financiero, balance y reportes.

**Tablas a migrar:**

- `presupuesto_institucional`: `creado_en`, `actualizado_en`, `eliminado_en`
- `proyeccion_estudiantes`: `creado_en`, `actualizado_en`, `eliminado_en`
- `resumen_proyeccion_financiera`: `creado_en`, `actualizado_en`, `eliminado_en`
- `configuracion_arancel`: `creado_en`, `actualizado_en`, `eliminado_en`

**Scripts creados para el lote 04**

1. [KAN14_fase3_lote04_financiero_precheck.sql](sql/KAN14_fase3_lote04_financiero_precheck.sql).
2. [KAN14_fase3_lote04_financiero_apply.sql](sql/KAN14_fase3_lote04_financiero_apply.sql).
3. [KAN14_fase3_lote04_financiero_postcheck.sql](sql/KAN14_fase3_lote04_financiero_postcheck.sql).
4. [KAN14_fase3_lote04_financiero_rollback.sql](sql/KAN14_fase3_lote04_financiero_rollback.sql).

**Estado del lote 04**

- Scripts ejecutados y validados en base local y en Supabase.
- Postcheck de Supabase sin diferencias de filas: todas las tablas objetivo devolvieron `filas = 0` en la validación reportada.
- Lote 04 cerrado operacionalmente.

**Impacto en módulos:** Épicas 9, 10, 11 (análisis financiero, balance, reportes).

**Siguiente secuencia recomendada para el lote 04**

1. Lote 04 ya completado en local y Supabase.
2. Continuar con validación funcional de aplicación sobre flujos de Épicas 9-11.
3. Mantener script rollback para ventana de observación.

**Resultado validado del lote 04 en Supabase**

1. `KAN14_fase3_lote04_financiero_precheck.sql`: `total_invalidos_lote04 = 0`.
2. `KAN14_fase3_lote04_financiero_apply.sql`: ejecutado con éxito.
3. `KAN14_fase3_lote04_financiero_postcheck.sql`: sin diferencias de filas en las tablas objetivo.
4. El esquema del bloque financiero quedó consistente para cierre de fase de timestamps.

---

#### Lote 05 (Complementario)

**Objetivo del lote**

Lote actualmente vacío: no quedan tablas con `TEXT` timestamps pendientes dentro del inventario cubierto por los lotes 02, 03 y 04.

**Estado actual del lote 05**

- Sin ejecución requerida por ahora.
- Se mantiene como lote de contingencia por si aparece una tabla adicional no inventariada.

**Tablas a migrar (si las hay restantes):**

- Cualquier tabla con `TEXT` timestamps no cubierta en lotes 02-04.

**Gate de ejecución de Lotes 02-05:**

1. Generar scripts precheck/apply/postcheck/rollback similares a Lote 01.
2. Validar smoke tests nuevamente tras cada lote.
3. Realizar backup Supabase tras cada lote exitoso (o al menos tras lote 02).

**Siguiente secuencia recomendada para el lote 05**

1. Revalidar el inventario solo si aparece una tabla nueva o se detecta una omisión documental.
2. Preparar scripts únicamente si surge una tabla adicional fuera del alcance actual.
3. Mientras no aparezcan nuevas tablas, no ejecutar lote 05.

## Fase 4 - Booleanos y consistencia de estado

### Objetivo

Corregir deuda de tipos booleanos y alinear estado funcional de usuarios.

### Actividades

1. Migrar `esta_activo int` a `boolean` en tablas heredadas.
2. Migrar `balance_proyectado.cuadra_balance` a `boolean`.
3. Corregir datos inconsistentes entre `estado` y `esta_activo`.
4. Alinear use cases de baja lógica para actualizar ambos campos consistentemente.

### Validaciones obligatorias

1. Login bloquea usuarios inactivos correctamente.
2. Gestión de usuarios no deja combinaciones inválidas de estado.
3. No hay errores ORM por tipos booleanos en consultas/filtros.

### Gate para avanzar

Gestión de usuarios estable y consistente sin falsos activos.

### Ejecución planificada - Fase 4 (Booleanos)

#### Scripts creados para Fase 4

1. [KAN14_fase4_booleanos_precheck.sql](sql/KAN14_fase4_booleanos_precheck.sql).
2. [KAN14_fase4_booleanos_apply.sql](sql/KAN14_fase4_booleanos_apply.sql).
3. [KAN14_fase4_booleanos_postcheck.sql](sql/KAN14_fase4_booleanos_postcheck.sql).
4. [KAN14_fase4_booleanos_rollback.sql](sql/KAN14_fase4_booleanos_rollback.sql).

#### Alcance técnico de Fase 4

1. Migración de todas las columnas `esta_activo` detectadas en `public.*` de `integer/smallint/bigint` a `boolean`.
2. Migración específica de `balance_proyectado.cuadra_balance` de `integer` a `boolean`.
3. Normalización de `usuario.estado` a catálogo canónico (`Activo`, `Suspendido`, `Inactivo`).
4. Corrección de consistencia en `usuario`: `esta_activo = (estado = 'Activo')`.

#### Orden operativo (primero base de respaldo/local, luego Supabase)

1. Ejecutar `KAN14_fase4_booleanos_precheck.sql` en base local.
2. Confirmar que `total_invalidos_fase4 = 0`.
3. Ejecutar `KAN14_fase4_booleanos_apply.sql`.
4. Ejecutar `KAN14_fase4_booleanos_postcheck.sql` y validar:

- `columnas_no_boolean = 0`.
- `activos_inconsistentes = 0`.
- `no_activos_inconsistentes = 0`.
- `estados_fuera_catalogo = 0`.

5. Si falla algo, ejecutar `KAN14_fase4_booleanos_rollback.sql` y detener avance.
6. Si local queda estable, repetir exactamente la misma secuencia en Supabase.

#### Resultado validado de Fase 4 en Supabase (2026-04-15)

1. `KAN14_fase4_booleanos_precheck.sql`: `total_invalidos_fase4 = 0`.
2. `KAN14_fase4_booleanos_apply.sql`: ejecutado con éxito (conversiones aplicadas y columnas ya booleanas omitidas de forma idempotente).
3. `KAN14_fase4_booleanos_postcheck.sql`: `columnas_no_boolean = 0`.
4. Conversión de `balance_proyectado.cuadra_balance` confirmada durante el apply.

#### Estado de cierre de Fase 4

1. Cierre técnico de esquema: **cumplido** (local + Supabase).
2. Cierre funcional de aplicación: **pendiente de validación smoke** (login y gestión de usuarios).

#### Alineación de código aplicada para evitar regresión

1. [src/Infrastructure/Persistence/Repositories/RepositorioUsuario.cs](src/Infrastructure/Persistence/Repositories/RepositorioUsuario.cs): en `ActualizarAsync`, al cambiar estado ahora también se actualiza `esta_activo` de forma consistente (`Activo => true`, resto => `false`).

#### Validación funcional obligatoria tras APPLY en Supabase

1. Login con usuario `Activo`: acceso permitido.
2. Login con usuario `Suspendido` o `Inactivo`: acceso bloqueado con mensaje esperado.
3. Gestión de usuarios:

- Cambiar a `Activo` deja `esta_activo = true`.
- Cambiar a `Suspendido/Inactivo` deja `esta_activo = false`.
- Baja lógica mantiene ambos campos coherentes.

4. Smoke técnico:

- Build de solución sin errores.
- Sin excepciones ORM por conversión de booleanos en consultas/filtros.

---

## Fase 5 - Endurecimiento de permisos y seguridad

### Objetivo

Completar matriz de permisos y aplicar RLS sin bloquear el sistema.

### Actividades

1. Completar permisos faltantes de Administrador (`CFG.*`, `REP.*`).
2. Crear `CA.ELIMINAR` y asignaciones mínimas.
3. Diseñar política RLS progresiva por tabla crítica y rol real de conexión.
4. Probar RLS en staging con credenciales reales de la app.
5. Aplicar en producción solo después de validación E2E satisfactoria.

### Validaciones obligatorias

1. Accesos autorizados funcionan con RLS activo.
2. Accesos no autorizados quedan bloqueados.
3. No hay caída de funcionalidades de módulos existentes.

### Gate para avanzar

Seguridad endurecida sin bloqueo funcional de la app.

### Riesgo principal si se hace mal

Bloqueo total/parcial de lecturas y escrituras por políticas RLS incorrectas.

### Ejecución planificada - Fase 5 Lote 01 (Permisos)

#### Scripts creados para Lote 01

1. [sql/KAN14_fase5_lote01_permisos_precheck.sql](sql/KAN14_fase5_lote01_permisos_precheck.sql).
2. [sql/KAN14_fase5_lote01_permisos_apply.sql](sql/KAN14_fase5_lote01_permisos_apply.sql).
3. [sql/KAN14_fase5_lote01_permisos_postcheck.sql](sql/KAN14_fase5_lote01_permisos_postcheck.sql).
4. [sql/KAN14_fase5_lote01_permisos_rollback.sql](sql/KAN14_fase5_lote01_permisos_rollback.sql).

#### Alcance del Lote 01

1. Garantizar existencia activa de: `CFG.VER`, `CFG.EDITAR`, `REP.VER`, `REP.EXPORTAR`, `CA.ELIMINAR`.
2. Asignar esos permisos al rol `Administrador`.
3. Mantener este lote sin cambios de RLS para evitar bloqueo funcional temprano.

#### Orden operativo (primero base de respaldo/local, luego Supabase)

1. Ejecutar `KAN14_fase5_lote01_permisos_precheck.sql` en base local.
2. Ejecutar `KAN14_fase5_lote01_permisos_apply.sql`.
3. Ejecutar `KAN14_fase5_lote01_permisos_postcheck.sql` y validar:

- `total_errores_lote01_permisos = 0`.

4. Si falla algo, ejecutar `KAN14_fase5_lote01_permisos_rollback.sql`.
5. Si local queda estable, repetir la secuencia en Supabase.

#### Gate de cierre Lote 01

1. `Administrador` con permisos completos `CFG.*`, `REP.*` y `CA.ELIMINAR`.
2. Menú por permisos de configuración/reportes sin regresiones visibles.

#### Resultado validado de Lote 01 en Supabase (2026-04-15)

1. `KAN14_fase5_lote01_permisos_precheck.sql`: diagnóstico de RLS ejecutado y sin bloqueantes de tablas faltantes.
2. `KAN14_fase5_lote01_permisos_apply.sql`: ejecutado con éxito.
3. `KAN14_fase5_lote01_permisos_postcheck.sql`: `total_errores_lote01_permisos = 0`.
4. Lote 01 de Fase 5 considerado **cerrado**.

#### Siguiente paso dentro de Fase 5 (Lote 02 - RLS)

1. Confirmar rol real de conexión de la app por entorno.
2. Definir y aplicar RLS de forma progresiva por tabla crítica (staging primero).
3. Validar E2E antes de producción.

### Ejecución planificada - Fase 5 Lote 02 (RLS)

#### Scripts creados para Lote 02

1. [sql/KAN14_fase5_lote02_rls_precheck.sql](sql/KAN14_fase5_lote02_rls_precheck.sql).
2. [sql/KAN14_fase5_lote02_rls_apply.sql](sql/KAN14_fase5_lote02_rls_apply.sql).
3. [sql/KAN14_fase5_lote02_rls_postcheck.sql](sql/KAN14_fase5_lote02_rls_postcheck.sql).
4. [sql/KAN14_fase5_lote02_rls_rollback.sql](sql/KAN14_fase5_lote02_rls_rollback.sql).

#### Alcance del Lote 02

1. Habilitar RLS progresivo sobre tablas críticas transversales: `usuario`, `rol`, `permiso`, `rol_permiso`, `usuario_permiso_override`, `sesion_usuario`, `auditoria_log`.
2. Crear política de continuidad `kan14_lote02_app_rw` para roles de app detectados (`authenticated`, `service_role`, `postgres`, `current_user`).
3. Preparar rollback inmediato para evitar bloqueo funcional en caso de regresión.

#### Orden operativo (primero base de respaldo/local, luego Supabase)

1. Ejecutar `KAN14_fase5_lote02_rls_precheck.sql` en base local.
2. Confirmar `tablas_faltantes = 0`.
3. Ejecutar `KAN14_fase5_lote02_rls_apply.sql`.
4. Ejecutar `KAN14_fase5_lote02_rls_postcheck.sql` y validar `total_errores_lote02_rls = 0`.
5. Si falla algo o hay corte funcional, ejecutar `KAN14_fase5_lote02_rls_rollback.sql`.
6. Si local queda estable, repetir exactamente la secuencia en Supabase.

#### Validación funcional obligatoria tras APPLY de Lote 02

1. Login y carga de menú por rol sin errores de acceso.
2. CRUD usuarios (listar/crear/editar/baja lógica) sin bloqueos por RLS.
3. Auditoría visible para administrador y no visible para visualizador.
4. Flujo de sesión (login/logout/revocación) sin errores de permisos SQL.

#### Resultado validado de Lote 02 en Supabase (2026-04-15)

1. `KAN14_fase5_lote02_rls_precheck.sql`: `tablas_faltantes = 0`.
2. `KAN14_fase5_lote02_rls_apply.sql`: ejecutado con éxito, creando política `kan14_lote02_app_rw` en tablas críticas.
3. `KAN14_fase5_lote02_rls_postcheck.sql`: `total_errores_lote02_rls = 0`.
4. Lote 02 de Fase 5 considerado **cerrado técnicamente** en local y Supabase.

#### Estado restante de Fase 5

1. Ejecutar smoke funcional final de aplicación con RLS activo (login, menú por rol, CRUD usuarios, auditoría, sesión).
2. Endurecimiento fino de RLS (migrar de política abierta de continuidad a políticas específicas por tabla/acción).
3. Cierre documental final de Fase 5 con evidencia E2E.

### Ejecución planificada - Fase 5 Lote 03 (RLS Endurecimiento fino)

#### Scripts creados para Lote 03

1. [sql/KAN14_fase5_lote03_rls_endurecimiento_precheck.sql](sql/KAN14_fase5_lote03_rls_endurecimiento_precheck.sql).
2. [sql/KAN14_fase5_lote03_rls_endurecimiento_apply.sql](sql/KAN14_fase5_lote03_rls_endurecimiento_apply.sql).
3. [sql/KAN14_fase5_lote03_rls_endurecimiento_postcheck.sql](sql/KAN14_fase5_lote03_rls_endurecimiento_postcheck.sql).
4. [sql/KAN14_fase5_lote03_rls_endurecimiento_rollback.sql](sql/KAN14_fase5_lote03_rls_endurecimiento_rollback.sql).

#### Alcance del Lote 03

1. Reemplazar la política amplia `kan14_lote02_app_rw` por políticas separadas por operación:

- `kan14_lote03_read`
- `kan14_lote03_insert`
- `kan14_lote03_update`
- `kan14_lote03_delete`

2. Mantener continuidad funcional con los roles de app detectados (`authenticated`, `service_role`, `postgres`, `current_user` existente).
3. Conservar rollback inmediato para regresar al esquema de continuidad del Lote 02 si hay regresión.

#### Orden operativo (primero base de respaldo/local, luego Supabase)

1. Ejecutar `KAN14_fase5_lote03_rls_endurecimiento_precheck.sql` en base local.
2. Confirmar gate: `total_bloqueantes_lote03_rls = 0`.
3. Ejecutar `KAN14_fase5_lote03_rls_endurecimiento_apply.sql`.
4. Ejecutar `KAN14_fase5_lote03_rls_endurecimiento_postcheck.sql` y validar `total_errores_lote03_rls = 0`.
5. Ejecutar smoke funcional de aplicación (login, menú por rol, CRUD usuarios, auditoría, sesión).
6. Si algo falla, ejecutar `KAN14_fase5_lote03_rls_endurecimiento_rollback.sql`.
7. Si local queda estable, repetir exactamente la secuencia en Supabase.

#### Gate de cierre Lote 03

1. `total_errores_lote03_rls = 0` en local y Supabase.
2. Cero regresiones funcionales en pruebas E2E críticas de seguridad y operación.

#### Resultado validado de Lote 03 en Supabase (2026-04-15)

1. `KAN14_fase5_lote03_rls_endurecimiento_precheck.sql`: `total_bloqueantes_lote03_rls = 0`.
2. `KAN14_fase5_lote03_rls_endurecimiento_apply.sql`: ejecutado con éxito y políticas finas creadas por tabla.
3. `KAN14_fase5_lote03_rls_endurecimiento_postcheck.sql`: `total_errores_lote03_rls = 0`.
4. Lote 03 de Fase 5 considerado **cerrado técnicamente** en local y Supabase.

#### Checklist exacto de pruebas en la app (post-Lote 03)

1. Login con `Administrador`, `Analista` y `Visualizador` sin errores de acceso SQL/RLS.
2. Menú dinámico:

- `Administrador`: ve `Usuarios`, `Configuración`, `Reportes`, `Auditoría`.
- `Visualizador`: no ve `Auditoría`.

3. Gestión de usuarios (como `Administrador`): listar, crear, editar, baja lógica y reactivar.
4. Cambio de rol de usuario (por ejemplo a `Visualizador`) y recarga de menú conforme a permisos.
5. Sesión: login -> cerrar sesión -> nuevo login sin errores de persistencia/revocación.
6. Auditoría: consulta y paginación de registros sin errores (solo para admin).
7. Inflación: listar/crear/editar/eliminar para validar que RLS no afectó módulos existentes.

#### Revisión de código tras endurecimiento RLS

1. No se identifica cambio obligatorio de código por RLS en esta etapa: las políticas de Lote 03 mantienen continuidad para roles de app detectados.
2. Hallazgos de análisis estático existentes (trazas `Trace.WriteLine`, complejidad, recomendaciones async) son deuda previa y no bloquean el cierre funcional de Fase 5.

#### Cierre de Fase 5

1. Checklist funcional ejecutado y validado tras Lote 03 (login, menú por rol, CRUD usuarios, sesión, auditoría e inflación).
2. Fase 5 se considera **cerrada técnicamente y funcionalmente** en local y Supabase.
3. Los scripts de rollback de los lotes 01-03 se mantienen como contingencia durante ventana de observación.
4. Siguiente fase activa: **Fase 6** (actualización progresiva de diagramas/documentación de handoff).

---

## Fase 6 - Actualización de diagramas y documentos (obligatoria para handoff)

### Objetivo

Dejar la documentación sincronizada con el código real para que próximos chats no implementen sobre supuestos antiguos.

### Actividades

1. Actualizar diagramas de secuencia desalineados (usuarios e inflación).
2. Marcar en cada documento: estado implementado vs diseño objetivo pendiente.
3. Diferenciar explícitamente en `src/Application/UseCases` lo documental (`.MD`/`.puml`) de lo implementado (`.cs`).
4. Publicar resumen final de cambios y decisiones técnicas.

### Gate de cierre

Documentación alineada con código real y validada por checklist final.

### Ejecución incremental propuesta (paso a paso)

1. Trabajar por lotes documentales pequeños (1 diagrama por ciclo), con commit independiente por diagrama.
2. Para cada diagrama: actualizar `.puml` + registrar evidencia en esta sección + validar consistencia con BD/código.
3. No mezclar cambios de varios dominios en un mismo commit (ejemplo: no combinar ER + secuencia en un solo lote).

### Lotes sugeridos de Fase 6 (según prioridad ya identificada)

1. **F6-L01**: `BD-03A-Costos operativos, activos y gastos.puml` (crítico).
2. **F6-L02**: `DC-01 Dominio Transversal Seguridad y Auditoria.puml` (crítico).
3. **F6-L03**: `Diagrama-Secuencia-3-Proyección de inflación.puml` (crítico).
4. **F6-L04**: `Diagrama-Secuencia-2-Gestión de usuarios y control de acceso por rol.puml`.
5. **F6-L05**: `DC-02.1 — Inflación.puml`.
6. **F6-L06**: `BD-02 Persistencia Academico-Operativa.puml`.
7. **F6-L07**: `BD-03B-Persistencia Financiera.puml`.
8. **F6-L08**: `Diagrama-Secuencia-1-Autenticación y gestión de sesión.puml`.
9. **F6-L09**: `BD-01 Persistencia Transversal y Académica Core.puml`.

### Estado de avance de Fase 6

| Lote   | Diagrama objetivo                                                           | Estado      | Observación             |
| ------ | --------------------------------------------------------------------------- | ----------- | ----------------------- |
| F6-L01 | `BD-03A-Costos operativos, activos y gastos.puml`                           | Planificado | Próximo lote a ejecutar |
| F6-L02 | `DC-01 Dominio Transversal Seguridad y Auditoria.puml`                      | Planificado | Pendiente               |
| F6-L03 | `Diagrama-Secuencia-3-Proyección de inflación.puml`                         | Planificado | Pendiente               |
| F6-L04 | `Diagrama-Secuencia-2-Gestión de usuarios y control de acceso por rol.puml` | Planificado | Pendiente               |
| F6-L05 | `DC-02.1 — Inflación.puml`                                                  | Planificado | Pendiente               |
| F6-L06 | `BD-02 Persistencia Academico-Operativa.puml`                               | Planificado | Pendiente               |
| F6-L07 | `BD-03B-Persistencia Financiera.puml`                                       | Planificado | Pendiente               |
| F6-L08 | `Diagrama-Secuencia-1-Autenticación y gestión de sesión.puml`               | Planificado | Pendiente               |
| F6-L09 | `BD-01 Persistencia Transversal y Académica Core.puml`                      | Planificado | Pendiente               |

### Gate por lote documental (obligatorio antes del siguiente)

1. El `.puml` actualizado refleja el estado real del código/BD vigente.
2. No quedan métodos, permisos o nombres de clases/interfaces fantasma en el diagrama intervenido.
3. Se actualiza este `Diagramas.md` con estado del lote: `Planificado` -> `En ejecución` -> `Cerrado`.
4. Commit único del lote con mensaje trazable (`KAN14 Fase 6 Lote XX: ...`).

---

## Reglas de ejecución para próximos chats (anti-ruptura)

1. Nunca ejecutar migraciones de esquema sin compatibilidad de código verificada.
2. Nunca renombrar permisos en BD sin sincronizar código + seeds + scripts en el mismo ciclo.
3. Nunca habilitar RLS global en una sola corrida.
4. Cada fase cierra con validación funcional y evidencia.
5. Si falla un gate, se detiene el avance y se corrige antes de continuar.

---

## Entregables mínimos esperados por fase

1. Script SQL versionado (idempotente cuando aplique).
2. Cambios de código asociados en la misma rama.
3. Evidencia de validación (salidas de prueba, capturas o resumen técnico).
4. Nota de compatibilidad y orden de despliegue por entorno.
5. Nota de rollback por fase.

---

## Resultado esperado al finalizar este plan

1. Se conserva funcionalidad estable de Épicas 2 y 3.
2. Se elimina deuda técnica de tipos (`TEXT`, `int` booleano).
3. Se unifica RBAC sin códigos huérfanos ni permisos inconsistentes.
4. Se endurece seguridad con RLS controlado y validado.
5. Se deja base estable para iniciar Épica 4+ sin regresiones.

---

---

# Análisis de Consistencia de Diagramas

> Fuente: comparación directa entre archivos `.puml`, BD real (Bloques A–E) y use cases `.cs`

---

## Diagrama ER: BD-01 — Persistencia Transversal y Académica Core

### Estado general

Parcialmente correcto — estructura de relaciones correcta, tipos de datos y columnas desactualizados.

### Hallazgos

**H1 — `auditoria_log.evento_en` tipado como `text` en diagrama, pero BD tiene `timestamp without time zone`**

- Evidencia: BD-01 línea 43 → `text evento_en`; Bloque D2 → `data_type: "timestamp without time zone"` (migrado por KAN09).
- Justificación: KAN09 convirtió el campo pero el diagrama no se actualizó.
- Impacto: 🟡 MEDIO — documentación engañosa para quien implemente lecturas de auditoría.

**H2 — `auditoria_log` faltan 4 columnas reales**

- Evidencia: diagrama muestra 6 campos; BD tiene 10 (`entidad_nombre`, `entidad_id`, `valores_anteriores_json`, `valores_nuevos_json` ausentes).
- Impacto: 🟡 MEDIO — implementaciones futuras de consulta de auditoría se harán contra un modelo incompleto.

**H3 — `usuario_permiso_override` NO está en el diagrama**

- Evidencia: Bloque D4 y E1 confirman la tabla existe en BD con 5 columnas; BD-01 no la incluye.
- Justificación: Es la tabla central del sistema RBAC granular (overrides por usuario). Su ausencia rompe la documentación del módulo de permisos.
- Impacto: 🔴 ALTO — cualquier implementador que lea solo los diagramas ER ignorará el mecanismo de overrides.

**H4 — `usuario` solo muestra 6 campos; BD tiene 13**

- Evidencia: Bloque A1 → `usuario` tiene `ultimo_acceso_en`, `creado_en`, `actualizado_en`, `eliminado_en`, `creado_por_usuario_id`, `actualizado_por_usuario_id`, `eliminado_por_usuario_id`.
- Impacto: 🟠 BAJO — columnas de auditoría omitidas por brevedad, pero `ultimo_acceso_en` es funcional y debería estar.

**H5 — Todos los `esta_activo` mostrados como `int`**

- Evidencia: BD-01 líneas 8, 14, 21 → `int esta_activo`; BD real también los tiene como `int4` (herencia SQLite).
- Justificación: El diagrama refleja la BD real actual, pero ambos deben migrarse a `boolean`. El diagrama muestra el estado incorrecto de forma consistente.
- Impacto: 🟡 MEDIO — diagrama correcto respecto a BD pero ambos son técnicamente incorrectos.

### ¿Requiere cambios?

Sí.

### Cambios necesarios en `BD-01 Persistencia Transversal y Académica Core.puml`

```
1. auditoria_log: cambiar tipo evento_en a timestamp; agregar entidad_nombre, entidad_id, valores_anteriores_json, valores_nuevos_json
2. Agregar tabla usuario_permiso_override { int usuario_id FK, int permiso_id FK, bool concedido, text motivo, text creado_en }
3. usuario: agregar ultimo_acceso_en timestamp
4. Cambiar todos los int esta_activo a bool esta_activo (tras ejecutar FIX_boolean)
5. sesion_usuario: cambiar text emitido_en/expira_en/revocado_en a timestamp (tras FIX_timestamps)
6. Relación: usuario_permiso_override relacionada con usuario y permiso
```

---

## Diagrama ER: BD-02 — Persistencia Académico-Operativa

### Estado general

Parcialmente correcto — estructura de relaciones correcta; `configuracion_arancel` y `configuracion_carga_docente` tienen columnas faltantes.

### Hallazgos

**H1 — `configuracion_arancel` faltan 4 columnas**

- Evidencia: BD-02 muestra `valor_arancel`, `arancel_optimo`, `tipo_origen`; BD real (Bloque A1) tiene además `valor_matricula`, `calculado_automaticamente`, `margen_tolerancia`, `fecha_calculo` (17 cols totales vs 5 en diagrama).
- Impacto: 🔴 ALTO — columnas clave para KAN-41 (arancel óptimo bisección) no están documentadas.

**H2 — `configuracion_carga_docente` falta `horas_tecnico_estandar`**

- Evidencia: BD real (Bloque A1) tiene `horas_tecnico_estandar numeric`; BD-02 no la incluye.
- Impacto: 🟡 MEDIO — afecta precisión del cálculo de docentes técnicos (KAN-19).

**H3 — `inflacion_proyectada.es_ajuste_manual` como `int`**

- Evidencia: BD-02 línea 27 → `int es_ajuste_manual`.
- Justificación: Mismo patrón que `esta_activo` — herencia SQLite. Debe migrarse a `boolean` junto a las otras columnas.
- Impacto: 🟠 BAJO — consistente con el estado real de BD actual.

**H4 — Columnas de auditoría omitidas en todas las tablas**

- Evidencia: Ninguna tabla de BD-02 muestra `creado_en`, `actualizado_en`, `eliminado_en`.
- Justificación: Omisión deliberada para legibilidad ER. Aceptable si se documenta como convención.
- Impacto: 🟢 BAJO — no afecta implementación siempre que los repositorios los manejen.

### ¿Requiere cambios?

Sí.

### Cambios necesarios en `BD-02 Persistencia Academico-Operativa.puml`

```
1. configuracion_arancel: agregar valor_matricula numeric, calculado_automaticamente int, margen_tolerancia numeric, fecha_calculo text
2. configuracion_carga_docente: agregar horas_tecnico_estandar numeric
3. inflacion_proyectada.es_ajuste_manual: cambiar int -> bool (tras FIX_boolean)
4. Nota al pie: "Columnas de auditoría (creado_en, actualizado_en, eliminado_en) omitidas por legibilidad"
```

---

## Diagrama ER: BD-03A — Costos Operativos, Activos y Gastos

### Estado general

Incorrecto — tabla `servicio_basico_mantenimiento` completamente ausente; `cargo_facultad` y `costo_gasto_periodo` con columnas faltantes.

### Hallazgos

**H1 — `servicio_basico_mantenimiento` NO está en el diagrama**

- Evidencia: Bloque E1 y E2 confirman tabla existe con 11 cols; BD-03A no la incluye.
- Justificación: Es parte directa de KAN-28 (Servicios + mantenimiento). Sin esta tabla en el ER, el diseño de KAN-28 no tiene referencia.
- Impacto: 🔴 ALTO — implementador de Épica 8 no tiene documentación ER para esta tabla.

**H2 — `cargo_facultad` faltan `tipo_cargo` y `es_cargo_docente`**

- Evidencia: BD real (Bloque A1) tiene `tipo_cargo text` y `es_cargo_docente int4`; BD-03A solo muestra `nombre_cargo` y `sueldo_base_mensual`.
- Impacto: 🔴 ALTO — `es_cargo_docente` es necesario para separar cargos docentes de administrativos en KAN-20.

**H3 — `costo_gasto_periodo` con columnas faltantes**

- Evidencia: BD-03A muestra ~7 columnas; BD real tiene 21 cols totales. Faltan: `total_sueldos_planta_central`, `total_mantenimiento`, `total_materiales_insumos`, `total_presupuesto_institucional`, `total_activos_diferidos`, `subtotal_costos`, `factor_imprevistos`, `costo_por_estudiante`, `numero_estudiantes`.
- Impacto: 🔴 ALTO — modelo consolidado incompleto para KAN-36 (tabla consolidada costos).

**H4 — `presupuesto_institucional` en diagrama pero sin FK visible**

- Evidencia: BD-03A incluye la tabla pero no muestra relación con `escenario_proyeccion_id` o `periodo_academico_id`.
- Impacto: 🟡 MEDIO — relaciones incorrectas para KAN-35.

### ¿Requiere cambios?

Sí (múltiples correcciones críticas).

### Cambios necesarios en `BD-03A-Costos operativos, activos y gastos.puml`

```
1. Agregar tabla servicio_basico_mantenimiento { int id PK, int escenario_proyeccion_id FK, text nombre_servicio, numeric costo_mensual, int numero_meses }
2. cargo_facultad: agregar text tipo_cargo, int es_cargo_docente (-> bool tras FIX)
3. costo_gasto_periodo: agregar las columnas faltantes documentadas en BD real
4. presupuesto_institucional: agregar FK escenario_proyeccion_id y relación explícita
5. Relación: escenario_proyeccion ||--o{ servicio_basico_mantenimiento
```

---

## Diagrama ER: BD-03B — Persistencia Financiera — Análisis y Financiamiento

### Estado general

Parcialmente correcto — relaciones correctas; múltiples tablas con columnas faltantes.

### Hallazgos

**H1 — `configuracion_financiamiento` faltan `porcentaje_credito` y `monto_propio`**

- Evidencia: BD real (Bloque A1) tiene `porcentaje_credito numeric` y `monto_propio numeric`; BD-03B solo muestra `porcentaje_recursos_propios` y `monto_credito`.
- Impacto: 🟡 MEDIO — datos de financiamiento incompletos para KAN-45.

**H2 — `amortizacion_cuota` faltan `amortizacion_capital` e `interes_acumulado`**

- Evidencia: BD real tiene 15 cols; BD-03B muestra 7. Ausentes: `amortizacion_capital numeric`, `interes_acumulado numeric`.
- Impacto: 🟡 MEDIO — tabla de amortización incompleta para KAN-45.

**H3 — `balance_proyectado` reducido a 7 campos; BD tiene 24**

- Evidencia: BD-03B muestra solo totales (`total_activo/pasivo/patrimonio`) y `cuadra_balance`; BD real incluye `caja_bancos`, `inventario_materiales`, `activos_fijos_brutos`, `depreciacion_acumulada`, `activos_fijos_netos`, `prestamos_por_pagar`, `intereses_por_pagar`, `capital_propio`, `utilidad_acumulada`, `total_pasivo_patrimonio`.
- Impacto: 🔴 ALTO — diagrama insuficiente para implementar KAN-46 correctamente.

**H4 — `resumen_proyeccion_financiera` solo muestra 2 campos; BD tiene 18**

- Evidencia: BD-03B → `ingreso_total`, `resultado_neto_total`; BD real tiene 18 cols.
- Impacto: 🟡 MEDIO — tabla de resumen subestimada para KAN-47 (informe CES).

**H5 — `configuracion_tmr` muestra 3 campos; BD tiene 13**

- Evidencia: BD-03B → `tasa_bancaria_porcentaje`, `premio_riesgo_porcentaje`, `tmr_calculada_porcentaje`; BD real tiene 13 cols (incluye probablemente `inflacion_promedio_porcentaje` y campos de auditoría).
- Impacto: 🟡 MEDIO.

### ¿Requiere cambios?

Sí.

### Cambios necesarios en `BD-03B-Persistencia Financiera.puml`

```
1. configuracion_financiamiento: agregar porcentaje_credito numeric, monto_propio numeric
2. amortizacion_cuota: agregar amortizacion_capital numeric, interes_acumulado numeric
3. balance_proyectado: expandir con caja_bancos, inventario_materiales, activos_fijos_brutos/netos, depreciacion_acumulada, prestamos_por_pagar, intereses_por_pagar, capital_propio, utilidad_acumulada, total_pasivo_patrimonio
4. resumen_proyeccion_financiera: agregar columnas faltantes (ejecutar A1 para lista completa)
5. Cambiar cuadra_balance int -> bool y es_viable int -> bool (tras FIX_boolean)
```

---

## Diagrama de Clases: DC-01 — Dominio Transversal Seguridad y Auditoría

### Estado general

Incorrecto — naming de interfaces, use cases y algunos tipos no coinciden con el código real.

### Hallazgos

**H1 — Naming de interfaces difiere del código real**

- Evidencia: DC-01 define `IUsuarioRepository`, `ISesionRepository`, `IAuditoriaRepository`; código real usa `IRepositorioUsuario`, `IRepositorioSesionUsuario`, `IAuditoriaServicio` (servicio, no repositorio).
- Justificación: Convención del proyecto es español (`Repositorio` no `Repository`). Auditoría es un **servicio** no un repositorio.
- Impacto: 🔴 ALTO — implementador que siga el diagrama creará interfaces con nombres incorrectos incompatibles con el DI container.

**H2 — Use cases consolidados vs código real separado**

- Evidencia: DC-01 define `AutenticarUsuarioUseCase` (Login + Logout) y `GestionUsuarioUseCase` (Crear + Suspender + AsignarRol); código real tiene 7 clases separadas: `LoginUseCase`, `CerrarSesionUseCase`, `CrearUsuarioUseCase`, `ActualizarUsuarioUseCase`, `EliminarUsuarioUseCase`, `ListarUsuariosUseCase`, `ObtenerUsuarioUseCase`.
- Impacto: 🔴 ALTO — incompatibilidad directa entre diagrama y estructura de archivos real.

**H3 — `VerificarPermisoUseCase.TienePermiso` no existe como tal**

- Evidencia: Código real tiene `ObtenerPermisosEfectivosUsuarioUseCase` y `ActualizarPermisosUsuarioUseCase`; no hay un `VerificarPermisoUseCase` con método `TienePermiso`.
- Impacto: 🔴 ALTO — la verificación de permisos en el sistema real está internalizada en cada use case, no en un use case central de verificación.

**H4 — `AuditoriaLog.EventoEn` tipado como `string` en diagrama**

- Evidencia: DC-01 línea 68 → `+string EventoEn`; BD tiene `timestamp without time zone`; entidad C# debería ser `DateTime`.
- Impacto: 🟡 MEDIO.

**H5 — `UsuarioPermisosOverride` ausente del diagrama**

- Evidencia: Tabla `usuario_permiso_override` existe en BD; use case `ActualizarPermisosUsuarioUseCase` existe en código; DC-01 no modela ni la entidad ni el use case.
- Impacto: 🔴 ALTO — el mecanismo de overrides de permisos (pieza clave del RBAC) no está documentado en el diagrama de dominio.

**H6 — `UsuarioViewModel` subestimado**

- Evidencia: DC-01 muestra solo `CrearUsuarioCommand` y `SuspenderCommand`; UI real tiene CRUD completo (crear, editar, eliminar, listar, ver).
- Impacto: 🟠 BAJO — diagrama de presentación incompleto.

### ¿Requiere cambios?

Sí (cambios críticos de naming y estructura).

### Cambios necesarios en `DC-01 Dominio Transversal Seguridad y Auditoria.puml`

```
1. Renombrar IUsuarioRepository -> IRepositorioUsuario
2. Renombrar ISesionRepository -> IRepositorioSesionUsuario
3. Renombrar IAuditoriaRepository -> IAuditoriaServicio
4. Separar AutenticarUsuarioUseCase en LoginUseCase + CerrarSesionUseCase
5. Separar GestionUsuarioUseCase en: CrearUsuarioUseCase, ActualizarUsuarioUseCase, EliminarUsuarioUseCase, ListarUsuariosUseCase, ObtenerUsuarioUseCase
6. Renombrar VerificarPermisoUseCase -> ObtenerPermisosEfectivosUsuarioUseCase + ActualizarPermisosUsuarioUseCase
7. Agregar entidad UsuarioPermisosOverride { int UsuarioId, int PermisoId, bool Concedido, string Motivo }
8. AuditoriaLog.EventoEn: cambiar string -> DateTime
9. Renombrar UsuarioRepository -> RepositorioUsuario; SesionRepository -> RepositorioSesionUsuario; AuditoriaRepository -> ServicioAuditoria
```

---

## Diagrama de Clases: DC-02.1 — Inflación

### Estado general

Parcialmente correcto — dominio correcto; Application layer subestimado (1 use case vs 10 reales).

### Hallazgos

**H1 — Solo 1 use case vs 10 implementados**

- Evidencia: DC-02.1 muestra solo `ProyectarInflacionUseCase`; código real tiene: `CrearInflacionAnualUseCase`, `ActualizarInflacionAnualUseCase`, `EliminarInflacionAnualUseCase`, `ListarInflacionAnualUseCase`, `ProyectarInflacionUseCase`, `ObtenerInflacionProyectadaParaDependientesUseCase`, `ImportarInflacionUseCase`, `ImportarInflacionBceUseCase`, `ImportarInflacionBceArchivoUseCase`, `LimpiarInflacionUseCase`.
- Impacto: 🔴 ALTO — 9 use cases implementados sin representación en el diagrama.

**H2 — `IInflacionRepository` único vs dos repositorios reales**

- Evidencia: Código real tiene `IRepositorioInflacionAnual` e `IRepositorioInflacionProyectada` separados; DC-02.1 muestra un solo `IInflacionRepository`.
- Impacto: 🟡 MEDIO — naming y estructura incorrectos.

**H3 — `ProyectarInflacionUseCase.AjustarManual` no existe**

- Evidencia: Método `AjustarManual(escenarioId, anio, valor)` en diagrama; no existe en el código real; el ajuste se hace a través de `ActualizarInflacionAnualUseCase`.
- Impacto: 🟡 MEDIO — método fantasma documentado.

**H4 — `ICarreraRepository` y `IEscenarioRepository` sin implementación confirmada en código**

- Evidencia: DC-02.1 muestra `ICarreraRepository` e `IEscenarioRepository`; exploración de código no encontró archivos `.cs` para CRUD de carreras/escenarios.
- Impacto: 🟡 MEDIO — interfaces documentadas pero no implementadas aún.

### ¿Requiere cambios?

Sí.

### Cambios necesarios en `DC-02.1 — Inflación.puml`

```
1. Separar IInflacionRepository en IRepositorioInflacionAnual + IRepositorioInflacionProyectada
2. Eliminar método AjustarManual de ProyectarInflacionUseCase
3. Agregar use cases implementados: CrearInflacionAnualUseCase, ActualizarInflacionAnualUseCase, EliminarInflacionAnualUseCase, ListarInflacionAnualUseCase, ObtenerInflacionProyectadaParaDependientesUseCase, ImportarInflacionUseCase, ImportarInflacionBceUseCase, ImportarInflacionBceArchivoUseCase, LimpiarInflacionUseCase
4. Renombrar InflacionRepository -> RepositorioInflacionAnual + RepositorioInflacionProyectada
5. Marcar ICarreraRepository e IEscenarioRepository como "pendiente de implementar"
```

---

## Diagrama de Secuencia: DS-01 — Autenticación y gestión de sesión

### Estado general

Parcialmente correcto — flujo principal correcto; pasos críticos del login real ausentes.

### Hallazgos

**H1 — Naming de participantes incorrecto**

- Evidencia: diagrama usa `AutenticarUsuarioUseCase`, `SesionRepository`, `AuditoriaRepository`; código real: `LoginUseCase`, `IRepositorioSesionUsuario`, `IAuditoriaServicio`.
- Impacto: 🔴 ALTO — cualquier implementación que siga el diagrama usará clases incorrectas.

**H2 — Faltan pasos de carga de roles y permisos**

- Evidencia: Código real (`LoginUseCase`) llama `ObtenerRolesDelUsuarioAsync` y `ObtenerPermisosEfectivosAsync` antes de crear sesión; el diagrama va directo de `VerificarHash` a `CrearSesion`.
- Justificación: `SesionDto` retorna `PermisosEfectivos` — estos deben cargarse en el login para construir el menú dinámico (KAN-08).
- Impacto: 🔴 ALTO — implementador no sabrá que debe cargar permisos en el login.

**H3 — Falta `RegistrarUltimoAccesoAsync`**

- Evidencia: Código real registra último acceso en `usuario.ultimo_acceso_en` al login exitoso; diagrama no lo muestra.
- Impacto: 🟡 MEDIO.

**H4 — Patrón de resiliencia ausente**

- Evidencia: Código real tiene retry (2 intentos con timeout de 12s) para obtener usuario; diagrama no refleja este comportamiento.
- Impacto: 🟠 BAJO — detalle de implementación, no afecta flujo nominal.

### ¿Requiere cambios?

Sí.

### Cambios necesarios en `Diagrama-Secuencia-1-Autenticación y gestión de sesión.puml`

```
1. Renombrar AutenticarUsuarioUseCase -> LoginUseCase (login) + CerrarSesionUseCase (logout)
2. Renombrar SesionRepository -> IRepositorioSesionUsuario
3. Renombrar AuditoriaRepository -> IAuditoriaServicio
4. Agregar paso tras VerificarHash:
   LoginUseCase -> UsuarioRepository: ObtenerRolesDelUsuarioAsync(usuarioId)
   LoginUseCase -> PermisoRepository: ObtenerPermisosEfectivosAsync(usuarioId)
5. Agregar: LoginUseCase -> UsuarioRepository: RegistrarUltimoAccesoAsync(usuarioId)
6. SesionDto: actualizar a mostrar campo PermisosEfectivos además de token/rol
```

---

## Diagrama de Secuencia: DS-02 — Gestión de usuarios y control de acceso por rol

### Estado general

Parcialmente correcto — flujo correcto pero nomenclatura y permisos desactualizados.

### Hallazgos

**H1 — Permiso `USUARIOS.ESCRIBIR` no existe en BD**

- Evidencia: DS-02 línea 31 → `TienePermiso(token, USUARIOS.ESCRIBIR)`; BD real (Bloque B1) tiene `US.CREAR`, `US.EDITAR`, `US.ELIMINAR`, `US.VER`. No existe `USUARIOS.ESCRIBIR`.
- Impacto: 🔴 ALTO — la verificación de permiso siempre fallará con ese código.

**H2 — `GestionUsuarioUseCase` vs use cases separados**

- Evidencia: DS-02 usa `GestionUsuarioUseCase.CrearUsuario`; código real usa `CrearUsuarioUseCase`. Mismo patrón que DC-01.
- Impacto: 🔴 ALTO.

**H3 — Falta validación de rol existente**

- Evidencia: `CrearUsuarioUseCase` llama `IRepositorioRol.ObtenerPorNombreAsync` para validar que el rol existe antes de asignarlo; DS-02 no muestra este paso.
- Impacto: 🟡 MEDIO.

**H4 — `IServicioHash` no se muestra como dependencia inyectada**

- Evidencia: Código real inyecta `IServicioHash`; diagrama muestra `GenerarHash` como método interno del use case.
- Impacto: 🟠 BAJO — detalle de diseño, no afecta flujo.

### ¿Requiere cambios?

Sí.

### Cambios necesarios en `Diagrama-Secuencia-2-Gestión de usuarios y control de acceso por rol.puml`

```
1. Cambiar USUARIOS.ESCRIBIR -> US.CREAR (en flujo crear) y US.EDITAR (en flujo suspender)
2. Renombrar GestionUsuarioUseCase -> CrearUsuarioUseCase (flujo crear) / ActualizarUsuarioUseCase (flujo suspender)
3. Renombrar RolRepository -> IRepositorioRol
4. Agregar paso: CrearUsuarioUseCase -> RolRepository: ObtenerPorNombreAsync(rolNombre)
5. Agregar participante IServicioHash en el flujo de GenerarHash
```

---

## Diagrama de Secuencia: DS-03 — Proyección de inflación

### Estado general

Incorrecto — flujo mayoritariamente desactualizado respecto al código real implementado.

### Hallazgos

**H1 — Firma de `Ejecutar` incorrecta**

- Evidencia: DS-03 → `Ejecutar(escenarioId, aniosAProyectar)`; código real → `EjecutarAsync(ProyeccionInflacionSolicitudDto solicitud, ...)` donde `solicitud` incluye `MetodoProyeccion` (regresión lineal vs promedio suave).
- Impacto: 🔴 ALTO — implementador pasará parámetros incorrectos.

**H2 — `AjustarManual` es un flujo fantasma**

- Evidencia: El método `AjustarManual(escenarioId, anio, nuevoValor)` no existe en el código; el ajuste manual se realiza mediante `ActualizarInflacionAnualUseCase` que modifica `tipo_fuente` a `"Ajuste manual"`.
- Impacto: 🔴 ALTO — flujo documentado que no corresponde a ningún use case real.

**H3 — Faltan flujos CRUD de inflación anual**

- Evidencia: DS-03 solo muestra proyección; código implementa `CrearInflacionAnualUseCase`, `ActualizarInflacionAnualUseCase`, `EliminarInflacionAnualUseCase`, `ListarInflacionAnualUseCase`.
- Impacto: 🟡 MEDIO — flujos implementados sin documentación de secuencia.

**H4 — Faltan flujos de importación**

- Evidencia: `ImportarInflacionBceUseCase`, `ImportarInflacionBceArchivoUseCase`, `ImportarInflacionUseCase` implementados; sin diagrama de secuencia.
- Impacto: 🟡 MEDIO.

**H5 — `INSERT OR UPDATE` es sintaxis SQLite, no PostgreSQL**

- Evidencia: DS-03 línea 46 → `INSERT OR UPDATE inflacion_proyectada`; PostgreSQL usa `INSERT ... ON CONFLICT DO UPDATE`.
- Impacto: 🟠 BAJO — comentario en diagrama, no genera error pero es inexacto.

### ¿Requiere cambios?

Sí (revisión completa del diagrama).

### Cambios necesarios en `Diagrama-Secuencia-3-Proyección de inflación.puml`

```
1. Actualizar firma: Ejecutar(solicitud: ProyeccionInflacionSolicitudDto) donde solicitud incluye metodoProyeccion
2. Eliminar flujo AjustarManual — reemplazar por flujo de ActualizarInflacionAnualUseCase con cambio a tipo_fuente="Ajuste manual"
3. Agregar flujo CRUD: Crear/Actualizar/Eliminar/Listar inflación anual
4. Agregar flujo Importar desde BCE (ImportarInflacionBceUseCase)
5. Agregar flujo Importar desde archivo (ImportarInflacionBceArchivoUseCase + ImportarInflacionUseCase)
6. Agregar flujo LimpiarInflacionUseCase
7. Cambiar "INSERT OR UPDATE" -> "INSERT ... ON CONFLICT DO UPDATE"
8. Renombrar InflacionProyectadaRepository -> IRepositorioInflacionProyectada
```

---

## Resumen ejecutivo de diagramas

| Diagrama | Estado                | Hallazgos críticos                                                                            | Requiere cambio |
| -------- | --------------------- | --------------------------------------------------------------------------------------------- | --------------- |
| BD-01    | Parcialmente correcto | `usuario_permiso_override` ausente; `auditoria_log` incompleto                                | Sí              |
| BD-02    | Parcialmente correcto | `configuracion_arancel` y `configuracion_carga_docente` incompletos                           | Sí              |
| BD-03A   | Incorrecto            | `servicio_basico_mantenimiento` ausente; `cargo_facultad` y `costo_gasto_periodo` incompletos | Sí (crítico)    |
| BD-03B   | Parcialmente correcto | `balance_proyectado` muy resumido; `amortizacion_cuota` incompleto                            | Sí              |
| DC-01    | Incorrecto            | Naming interfaces/use cases diferente al código; `UsuarioPermisosOverride` ausente            | Sí (crítico)    |
| DC-02.1  | Parcialmente correcto | 9 use cases no documentados; método fantasma `AjustarManual`                                  | Sí (crítico)    |
| DS-01    | Parcialmente correcto | Roles/permisos no cargados en login; naming incorrecto                                        | Sí              |
| DS-02    | Parcialmente correcto | Permiso `USUARIOS.ESCRIBIR` inexistente en BD                                                 | Sí (crítico)    |
| DS-03    | Incorrecto            | Firma incorrecta; flujo fantasma; 6 flujos implementados sin diagrama                         | Sí (crítico)    |

> **Prioridad de actualización:** BD-03A → DC-01 → DS-03 → DS-02 → DC-02.1 → BD-02 → BD-03B → DS-01 → BD-01
