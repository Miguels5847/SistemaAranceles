# Caso de Uso: Editar datos de inflación registrados

## Identificacion

| Campo            | Valor                                                                   |
| ---------------- | ----------------------------------------------------------------------- |
| Identificador    | CU-IN-02                                                                |
| Nombre           | Editar datos de inflación registrados                                   |
| RF asociado      | RF-IN-02                                                                |
| Actor primario   | Administrador / Planificador Académico                                  |
| Actor secundario | Sistema (validaciones automáticas, registro de auditoría, trazabilidad) |

## Descripcion

Permite editar el porcentaje de inflación y la fuente de cualquier año previamente registrado, ya sea un dato histórico o un valor proyectado por regresión. Cuando un Planificador Académico sobrescribe un valor proyectado, la fuente cambia automáticamente a "Ajuste manual" para mantener trazabilidad.

---

## Flujo Principal - Editar dato de inflación

1. El usuario accede al módulo de Inflación.
2. El sistema muestra la tabla histórica de inflación con todos los registros: año, porcentaje, fuente, tipo (histórico/proyectado) y fecha de última modificación.
3. El usuario selecciona un registro de la tabla y elige la opción "Editar".
4. El sistema muestra el formulario de edición con los valores actuales: porcentaje de inflación (%) y fuente del dato.
5. El usuario modifica los campos deseados y confirma.
6. El sistema valida que el nuevo porcentaje de inflación esté dentro del rango −10% a +20%.
7. El sistema guarda los cambios con fecha/hora de modificación y usuario que modificó.
8. Si el registro era de tipo "Proyectado" y fue editado, el sistema cambia la fuente a "Ajuste manual".
9. El sistema registra la acción en el log de auditoría (CU-US-04).
10. El sistema muestra confirmación de edición exitosa y actualiza la tabla histórica.

### Flujos alternos - Editar dato

**6a. Porcentaje fuera de rango:**

- El sistema muestra mensaje "El valor de inflación debe estar entre −10% y +20%".
- El flujo retorna al paso 4 con los datos previamente ingresados.

**5a. Sin cambios realizados:**

- El usuario confirma sin haber modificado ningún campo.
- El sistema muestra mensaje "No se detectaron cambios".
- No genera registro de auditoría.
- El flujo retorna al paso 2.

**3a. Intento de editar el año:**

- El sistema no permite modificar el año de un registro existente.
- El campo año se muestra como solo lectura.
- Si se necesita corregir el año, se debe eliminar el registro y crear uno nuevo.

---

## Precondiciones

- El usuario ha iniciado sesión en el sistema (CU-US-02).
- El usuario tiene permiso de escritura sobre el módulo Inflación (Administrador o Planificador Académico).
- Existe al menos un dato de inflación registrado en el sistema (CU-IN-01) o proyectado (CU-IN-03).

## Postcondiciones

- El dato de inflación queda actualizado con los nuevos valores.
- Se conserva la trazabilidad completa: quién modificó, cuándo y qué valor tenía antes.
- Si el dato era proyectado y fue editado manualmente, la fuente queda como "Ajuste manual".
- Los módulos consumidores (Sueldos, Demanda, Mantenimiento) utilizarán automáticamente el valor actualizado en sus próximos cálculos.
- Se registra un log de auditoría con la acción ejecutada (CU-US-04).

## Reglas de Negocio

- **RN-26:** El año de un registro de inflación no puede ser modificado; es inmutable una vez creado.
- **RN-27:** El porcentaje de inflación editado debe cumplir el mismo rango de validación que al registrar: −10% a +20%.
- **RN-28:** Cuando se edita un valor proyectado por regresión, la fuente cambia automáticamente a "Ajuste manual".
- **RN-29:** El sistema debe registrar trazabilidad de cada edición: usuario que modificó, fecha/hora y valor anterior.
- **RN-30:** La edición de un dato de inflación no recalcula automáticamente los módulos consumidores; estos se recalculan cuando el usuario accede a ellos o ejecuta un recálculo explícito.
