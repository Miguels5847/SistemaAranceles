# Caso de Uso: Autenticar usuarios y gestionar sesiones

## Identificacion

| Campo            | Valor                                                                                |
| ---------------- | ------------------------------------------------------------------------------------ |
| Identificador    | CU-US-02                                                                             |
| Nombre           | Autenticar usuarios y gestionar sesiones                                             |
| RF asociado      | RF-US-02                                                                             |
| Actor primario   | Usuario (cualquier rol: Administrador, Planificador Academico o Consultor)           |
| Actor secundario | Sistema (validación de credenciales, gestión de sesión, temporizador de inactividad) |

## Descripcion

Permite a cualquier usuario del sistema autenticarse mediante correo electrónico y contraseña para acceder a las funcionalidades según su rol, cerrar sesión de forma voluntaria, y gestionar la expiración automática de sesión por inactividad configurable.

---

## Flujo Principal - Iniciar Sesion

1. El usuario accede al formulario de inicio de sesión del sistema.
2. El sistema muestra los campos: correo electrónico y contraseña.
3. El usuario ingresa su correo electrónico y contraseña, y confirma.
4. El sistema valida que ambos campos estén completos.
5. El sistema busca el correo en la base de datos.
6. El sistema compara la contraseña ingresada con el hash BCrypt almacenado.
7. El sistema verifica que la cuenta tenga estado "Activo".
8. El sistema crea la sesión autenticada.
9. El sistema carga el menú de navegación adaptado al rol del usuario (solo módulos permitidos).
10. El sistema redirige al panel principal del sistema.

### Flujos alternos - Iniciar Sesion

**5a. Correo no encontrado:**

- El sistema muestra mensaje genérico "Credenciales incorrectas" (sin revelar si el correo existe o no).
- El flujo retorna al paso 2.

**6a. Contrasena incorrecta:**

- El sistema muestra el mismo mensaje genérico "Credenciales incorrectas".
- El flujo retorna al paso 2.

**7a. Cuenta suspendida:**

- El sistema muestra el mensaje "Su cuenta se encuentra suspendida. Contacte al Administrador".
- El flujo retorna al paso 2.

**4a. Campos incompletos:**

- El sistema resalta los campos faltantes y no permite confirmar hasta que estén completos.

---

## Flujo - Cerrar Sesion Voluntaria

1. El usuario selecciona la opción "Cerrar sesión" disponible en cualquier pantalla del sistema.
2. El sistema solicita confirmación: "¿Desea cerrar la sesión actual?"
3. El usuario confirma.
4. El sistema invalida la sesión activa.
5. El sistema redirige al formulario de inicio de sesión.

---

## Flujo - Expiracion Automatica por Inactividad

1. El sistema monitorea la actividad del usuario durante la sesión.
2. El sistema detecta que no ha habido interacción durante el tiempo configurado (por defecto 30 minutos).
3. El sistema invalida la sesión automáticamente.
4. El sistema redirige al formulario de inicio de sesión con el mensaje "Su sesión ha expirado por inactividad".

---

## Precondiciones

- El sistema está operativo y la base de datos accesible.
- Existe al menos una cuenta de usuario registrada en el sistema (CU-US-01).

## Postcondiciones

- El usuario queda autenticado con sesión activa y acceso según su rol, o la sesión queda cerrada/expirada y el usuario es redirigido al formulario de inicio de sesión.

## Reglas de Negocio

- **RN-06:** El mensaje de error en autenticación debe ser genérico ("Credenciales incorrectas") para no revelar si el correo existe o si la contraseña es incorrecta.
- **RN-07:** Los usuarios con estado "Suspendido" no pueden iniciar sesión bajo ninguna circunstancia.
- **RN-08:** El tiempo de expiración de sesión por inactividad es configurable por el Administrador (valor por defecto: 30 minutos).
- **RN-09:** El cierre de sesión (voluntario o automático) debe invalidar completamente la sesión; no debe ser posible regresar con el botón "atrás" del navegador o la aplicación.
