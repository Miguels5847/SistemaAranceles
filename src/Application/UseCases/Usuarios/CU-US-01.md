# Caso de Uso: Gestionar cuentas de usuario

## Identificacion

| Campo            | Valor                                                              |
| ---------------- | ------------------------------------------------------------------ |
| Identificador    | CU-US-01                                                           |
| Nombre           | Gestionar cuentas de usuario                                       |
| RF asociado      | RF-US-01                                                           |
| Actor primario   | Administrador                                                      |
| Actor secundario | Sistema (validaciones automáticas, cifrado, registro de auditoría) |

## Descripcion

Permite al Administrador crear nuevas cuentas de usuario, consultar el listado de usuarios existentes, editar sus datos personales y de rol, y suspender o reactivar cuentas según sea necesario, garantizando que no existan correos duplicados y que las contraseñas se almacenen de forma segura.

---

## Flujo Principal - Crear Usuario

1. El Administrador accede al panel de administración de usuarios.
2. El Administrador selecciona la opción "Crear usuario".
3. El sistema muestra el formulario de creación con los campos: nombre completo, correo electrónico, contraseña inicial y rol a asignar.
4. El Administrador completa los campos y confirma la operación.
5. El sistema valida que el correo electrónico no esté registrado previamente.
6. El sistema cifra la contraseña con BCrypt.
7. El sistema crea la cuenta con estado "Activo".
8. El sistema registra la acción en el log de auditoría (CU-US-04).
9. El sistema muestra confirmación de creación exitosa y actualiza el listado de usuarios.

### Flujos alternos - Crear Usuario

**5a. Correo duplicado:**

- El sistema detecta que el correo ya existe.
- Muestra mensaje "El correo electrónico ya se encuentra registrado".
- El flujo retorna al paso 3 con los datos previamente ingresados.

**4a. Campos incompletos:**

- El Administrador no completa todos los campos obligatorios.
- El sistema resalta los campos faltantes y no permite confirmar hasta que estén completos.

**4b. Formato de correo invalido:**

- El sistema valida que el correo tenga formato válido.
- Si no lo tiene, muestra mensaje de error y retorna al paso 3.

---

## Flujo - Consultar Usuarios

1. El Administrador accede al panel de administración de usuarios.
2. El sistema muestra el listado de todas las cuentas con columnas: nombre completo, correo electrónico, rol asignado y estado (Activo/Suspendido).
3. El Administrador puede filtrar por rol o estado, o buscar por nombre o correo.

---

## Flujo - Editar Usuario

1. El Administrador selecciona un usuario del listado.
2. El Administrador selecciona la opción "Editar".
3. El sistema muestra el formulario con los datos actuales del usuario: nombre, correo y rol.
4. El Administrador modifica los campos deseados y confirma.
5. El sistema valida que si el correo fue modificado, el nuevo correo no esté duplicado.
6. El sistema guarda los cambios.
7. El sistema registra la acción en el log de auditoría (CU-US-04).
8. El sistema muestra confirmación y actualiza el listado.

### Flujos alternos - Editar Usuario

**5a. Correo duplicado:**

- Mismo tratamiento que en el flujo de creación paso 5a.

**1a. El Administrador intenta editar su propia cuenta y cambiar su rol:**

- El sistema impide que un Administrador se quite a sí mismo el rol de Administrador si es el único usuario con ese rol.

---

## Flujo - Suspender / Reactivar Usuario

1. El Administrador selecciona un usuario del listado.
2. El Administrador selecciona "Suspender" (si está Activo) o "Reactivar" (si está Suspendido).
3. El sistema solicita confirmación de la acción.
4. El Administrador confirma.
5. El sistema cambia el estado del usuario.
6. El sistema registra la acción en el log de auditoría (CU-US-04).
7. El sistema muestra confirmación y actualiza el listado.

### Flujos alternos - Suspender / Reactivar

**2a. El Administrador intenta suspender su propia cuenta:**

- El sistema impide la operación y muestra "No puede suspender su propia cuenta".

**2b. El Administrador intenta suspender al unico usuario con rol Administrador:**

- El sistema impide la operación y muestra "Debe existir al menos un Administrador activo en el sistema".

---

## Precondiciones

- El Administrador ha iniciado sesión en el sistema (CU-US-02).
- El Administrador tiene el rol con permiso de escritura sobre el módulo Usuarios.
- Existe al menos un rol activo en el sistema para poder asignarlo a nuevos usuarios (CU-US-03).

## Postcondiciones

- La cuenta de usuario queda creada, editada o con estado modificado según la operación realizada.
- La contraseña se almacena cifrada con BCrypt (nunca en texto plano).
- Se registra un log de auditoría con la acción ejecutada (CU-US-04).

## Reglas de Negocio

- **RN-01:** El correo electrónico es único en todo el sistema (no puede haber dos usuarios con el mismo correo).
- **RN-02:** Las contraseñas deben almacenarse cifradas con BCrypt; nunca en texto plano ni con cifrado reversible.
- **RN-03:** Debe existir al menos un usuario con rol Administrador activo en todo momento.
- **RN-04:** Los usuarios suspendidos no pueden iniciar sesión pero sus datos permanecen en el sistema (no se eliminan físicamente).
- **RN-05:** Toda operación de creación, edición o cambio de estado genera un registro de auditoría automáticamente.
