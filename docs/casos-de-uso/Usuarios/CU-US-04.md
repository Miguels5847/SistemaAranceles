# Caso de Uso: Registrar auditoria basica de actividades

## Identificacion

| Campo            | Valor                                              |
| ---------------- | -------------------------------------------------- |
| Identificador    | CU-US-04                                           |
| Nombre           | Registrar auditoria basica de actividades          |
| RF asociado      | RF-US-04                                           |
| Actor primario   | Sistema (registro automático de acciones críticas) |
| Actor secundario | Administrador (consulta del log de auditoría)      |

## Descripcion

El sistema registra automáticamente un log de las acciones críticas realizadas por los usuarios, incluyendo creación, edición y suspensión de cuentas, cambios de roles y permisos, y modificaciones en datos de los módulos de cálculo. El Administrador puede consultar este log con filtros por usuario, fecha y módulo.

---

## Flujo Principal - Registro Automatico de Accion Critica

1. Un usuario autenticado ejecuta una acción crítica en cualquier módulo del sistema (crear, editar, suspender, cambiar rol, modificar datos de cálculo).
2. El sistema detecta que la acción corresponde a una operación auditable.
3. El sistema captura automáticamente: acción realizada, identificador del usuario que la ejecutó, fecha y hora exacta, y módulo afectado.
4. El sistema almacena el registro en la tabla de auditoría.

### Flujos alternos - Registro Automatico

**4a. Error al guardar registro de auditoria:**

- El sistema registra el error internamente.
- La operación original del usuario NO se interrumpe ni se revierte (la auditoría no debe bloquear la funcionalidad del sistema).
- El sistema reintenta el registro en segundo plano.

---

## Flujo - Consultar Log de Auditoria

1. El Administrador accede a la vista de log de auditoría.
2. El sistema muestra el listado de registros ordenados por fecha descendente (más recientes primero), con columnas: fecha y hora, usuario, acción realizada y módulo afectado.
3. El Administrador puede aplicar filtros por: usuario específico, rango de fechas (desde/hasta), módulo afectado.
4. El sistema actualiza el listado mostrando solo los registros que coinciden con los filtros aplicados.
5. El Administrador puede visualizar el detalle de un registro seleccionado.

### Flujos alternos - Consultar Log

**3a. Sin resultados:**

- El sistema muestra mensaje "No se encontraron registros con los filtros aplicados".
- Permite al Administrador modificar los filtros o limpiarlos.

**4a. Volumen grande de registros:**

- El sistema pagina los resultados mostrando un número configurable de registros por página (por defecto 50).

---

## Precondiciones

- Para el registro automático: un usuario ha ejecutado una acción crítica dentro de cualquier módulo.
- Para la consulta: el Administrador ha iniciado sesión (CU-US-02) y tiene permiso de lectura sobre el módulo Usuarios.

## Postcondiciones

- Cada acción crítica queda registrada de forma permanente e inmutable en la tabla de auditoría.
- Los registros de auditoría no pueden ser editados, eliminados ni alterados por ningún usuario del sistema, incluyendo el Administrador.

## Reglas de Negocio

- **RN-16:** Las acciones auditables incluyen como mínimo: creación de usuario, edición de usuario, suspensión/reactivación de usuario, creación de rol, edición de rol, activación/desactivación de rol, y toda modificación de datos en los módulos de cálculo (inflación, tasas, estudiantes, sueldos, recursos, costos, etc.).
- **RN-17:** Los registros de auditoría son inmutables — no pueden ser editados ni eliminados por ningún usuario del sistema, incluyendo el Administrador.
- **RN-18:** El registro de auditoría no debe afectar el rendimiento de la operación principal; se ejecuta de forma transparente.
- **RN-19:** La consulta del log de auditoría es exclusiva del Administrador; los demás roles no pueden acceder a esta vista.
- **RN-20:** Cada registro de auditoría contiene como mínimo: identificador único, acción realizada (texto descriptivo), usuario que ejecutó la acción, fecha y hora con precisión de segundos, y módulo afectado.
