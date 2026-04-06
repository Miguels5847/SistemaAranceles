# Caso de Uso: Gestionar roles, permisos y restringir acceso

## Identificacion

| Campo            | Valor                                                                              |
| ---------------- | ---------------------------------------------------------------------------------- |
| Identificador    | CU-US-03                                                                           |
| Nombre           | Gestionar roles, permisos y restringir acceso                                      |
| RF asociado      | RF-US-03                                                                           |
| Actor primario   | Administrador                                                                      |
| Actor secundario | Sistema (verificación de permisos en cada solicitud, adaptación dinámica del menú) |

## Descripcion

Permite al Administrador crear, editar, activar y desactivar roles con permisos específicos por módulo y acción (leer, escribir, exportar), y garantiza que el sistema restrinja automáticamente el acceso a funcionalidades según el rol asignado a cada usuario, adaptando el menú de navegación para mostrar únicamente los módulos autorizados.

---

## Flujo Principal - Crear Rol

1. El Administrador accede al panel de gestión de roles y permisos.
2. El Administrador selecciona la opción "Crear rol".
3. El sistema muestra el formulario con los campos: nombre del rol, descripción, y lista de módulos con sus acciones disponibles (leer, escribir, exportar).
4. El Administrador ingresa el nombre, la descripción y selecciona los permisos deseados para cada módulo.
5. El sistema valida que el nombre del rol no esté duplicado.
6. El sistema guarda el rol con estado "Activo" y sus permisos asociados.
7. El sistema registra la acción en el log de auditoría (CU-US-04).
8. El sistema muestra confirmación de creación exitosa y actualiza el listado de roles.

### Flujos alternos - Crear Rol

**5a. Nombre duplicado:**

- El sistema detecta que ya existe un rol con el mismo nombre.
- Muestra mensaje "Ya existe un rol con ese nombre".
- El flujo retorna al paso 3 con los datos previamente ingresados.

**4a. Campos obligatorios incompletos:**

- El sistema resalta los campos faltantes (nombre es obligatorio) y no permite confirmar hasta que estén completos.

**4b. Sin permisos seleccionados:**

- El sistema muestra advertencia "Debe asignar al menos un permiso al rol".
- No permite guardar sin al menos un permiso.

---

## Flujo - Consultar Roles

1. El Administrador accede al panel de gestión de roles y permisos.
2. El sistema muestra el listado de todos los roles con columnas: nombre, descripción, estado (Activo/Inactivo) y cantidad de usuarios asignados.
3. El Administrador puede seleccionar un rol para ver el detalle de sus permisos por módulo.

---

## Flujo - Editar Rol

1. El Administrador selecciona un rol del listado.
2. El Administrador selecciona la opción "Editar".
3. El sistema muestra el formulario con los datos actuales del rol: nombre, descripción y permisos asignados.
4. El Administrador modifica los campos o permisos deseados y confirma.
5. El sistema valida que si el nombre fue modificado, el nuevo nombre no esté duplicado.
6. El sistema guarda los cambios.
7. El sistema registra la acción en el log de auditoría (CU-US-04).
8. El sistema muestra confirmación y actualiza el listado.

### Flujos alternos - Editar Rol

**5a. Nombre duplicado:**

- Mismo tratamiento que en el flujo de creación paso 5a.

**4a. El Administrador intenta quitar permisos criticos al rol Administrador:**

- El sistema impide la operación y muestra "El rol Administrador debe conservar al menos el permiso de escritura sobre el módulo Usuarios".

---

## Flujo - Activar / Desactivar Rol

1. El Administrador selecciona un rol del listado.
2. El Administrador selecciona "Desactivar" (si está Activo) o "Activar" (si está Inactivo).
3. El sistema solicita confirmación de la acción.
4. El Administrador confirma.
5. El sistema cambia el estado del rol.
6. El sistema registra la acción en el log de auditoría (CU-US-04).
7. El sistema muestra confirmación y actualiza el listado.

### Flujos alternos - Activar / Desactivar Rol

**2a. El Administrador intenta desactivar el rol "Administrador":**

- El sistema impide la operación y muestra "El rol Administrador no puede ser desactivado".

**2b. El Administrador intenta desactivar un rol con usuarios activos asignados:**

- El sistema muestra advertencia "Este rol tiene X usuario(s) activo(s) asignado(s). Reasigne sus roles antes de desactivar".
- No permite desactivar hasta que no haya usuarios activos con ese rol.

---

## Flujo - Restriccion Automatica de Acceso (verificacion continua)

1. Un usuario autenticado solicita acceder a un módulo o ejecutar una acción.
2. El sistema consulta el rol del usuario y sus permisos asociados.
3. El sistema verifica si el rol tiene el permiso requerido para el módulo y la acción solicitada.
4. Si tiene permiso, el sistema concede el acceso.
5. Si no tiene permiso, el sistema muestra mensaje "No tiene permisos para acceder a este recurso" y deniega la operación.

---

## Precondiciones

- El Administrador ha iniciado sesión en el sistema (CU-US-02).
- El Administrador tiene el rol con permiso de escritura sobre el módulo Usuarios.

## Postcondiciones

- El rol queda creado, editado o con estado modificado según la operación realizada.
- Los permisos del rol se aplican inmediatamente a todos los usuarios que lo tengan asignado.
- El menú de navegación de cada usuario refleja únicamente los módulos que su rol autoriza.
- Se registra un log de auditoría con la acción ejecutada (CU-US-04).

## Reglas de Negocio

- **RN-10:** El nombre del rol es único en todo el sistema.
- **RN-11:** Los roles se desactivan, nunca se eliminan físicamente del sistema.
- **RN-12:** Existen tres roles predefinidos sugeridos: Administrador, Planificador Academico y Consultor. El rol Administrador no puede ser desactivado ni despojado de sus permisos sobre el módulo Usuarios.
- **RN-13:** No se puede desactivar un rol que tenga usuarios activos asignados; primero se deben reasignar.
- **RN-14:** La verificación de permisos ocurre en cada solicitud del usuario y debe completarse en menos de 1 segundo, de forma transparente.
- **RN-15:** El menú de navegación se adapta dinámicamente al rol del usuario, mostrando solo los módulos a los que tiene acceso.
