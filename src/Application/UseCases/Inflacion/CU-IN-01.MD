# Caso de Uso: Registrar datos históricos de inflación anual

## Identificacion

| Campo            | Valor                                                     |
| ---------------- | --------------------------------------------------------- |
| Identificador    | CU-IN-01                                                  |
| Nombre           | Registrar datos históricos de inflación anual             |
| RF asociado      | RF-IN-01                                                  |
| Actor primario   | Administrador                                             |
| Actor secundario | Sistema (validaciones automáticas, registro de auditoría) |

## Descripcion

Permite al Administrador registrar manualmente datos de inflación anual del Ecuador, incluyendo el año, el porcentaje de inflación y opcionalmente la fuente del dato (ej. BCE, Statista, Estimación), garantizando que no existan años duplicados y que los valores estén dentro de un rango razonable.

---

## Flujo Principal - Registrar inflación manualmente

1. El Administrador accede al módulo de Inflación.
2. El sistema muestra la tabla histórica de inflación con los datos registrados: año, porcentaje de inflación, fuente y tipo (histórico/proyectado).
3. El Administrador selecciona la opción "Registrar nuevo dato".
4. El sistema muestra el formulario con los campos: año, porcentaje de inflación (%) y fuente del dato (opcional).
5. El Administrador completa los campos y confirma la operación.
6. El sistema valida que el año no esté registrado previamente.
7. El sistema valida que el porcentaje de inflación esté dentro del rango −10% a +20%.
8. El sistema guarda el registro con fecha/hora de creación, usuario que lo registró y tipo "Histórico".
9. El sistema registra la acción en el log de auditoría (CU-US-04).
10. El sistema muestra confirmación de registro exitoso y actualiza la tabla histórica.

### Flujos alternos - Registrar inflación

**6a. Año duplicado:**

- El sistema detecta que el año ya existe.
- Muestra mensaje "El año ingresado ya tiene un dato registrado. ¿Desea editarlo?" con opción de redirigir a edición (CU-IN-02).
- El flujo retorna al paso 4.

**7a. Porcentaje fuera de rango:**

- El sistema muestra mensaje "El valor de inflación debe estar entre −10% y +20%".
- El flujo retorna al paso 4 con los datos previamente ingresados.

**5a. Campos obligatorios incompletos:**

- El sistema resalta los campos faltantes (año y porcentaje son obligatorios).
- No permite confirmar hasta que estén completos.

**5b. Año con formato inválido:**

- El sistema valida que el año sea un número entero de 4 dígitos.
- Si no lo es, muestra mensaje de error y retorna al paso 4.

---

## Flujo alterno - Importar datos desde archivo

1. El Administrador selecciona la opción "Importar desde archivo".
2. El sistema muestra un diálogo de selección de archivo (formatos aceptados: .xlsx, .csv).
3. El Administrador selecciona el archivo.
4. El sistema lee el archivo y muestra una vista previa de los datos encontrados: año y porcentaje de inflación por fila.
5. El sistema resalta en rojo las filas con errores (años duplicados, valores fuera de rango, formatos inválidos).
6. El Administrador revisa la vista previa y confirma la importación.
7. El sistema importa únicamente las filas válidas, descartando las que tienen errores.
8. El sistema registra la acción en el log de auditoría (CU-US-04).
9. El sistema muestra resumen: X registros importados, Y registros descartados con detalle de errores.

### Flujos alternos - Importación

**3a. Archivo con formato no soportado:**

- El sistema muestra mensaje "Formato de archivo no soportado. Use .xlsx o .csv".
- El flujo retorna al paso 2.

**4a. Archivo vacío o sin datos válidos:**

- El sistema muestra mensaje "El archivo no contiene datos válidos de inflación".
- El flujo retorna al paso 2.

**6a. El Administrador cancela:**

- El sistema descarta la vista previa y no importa ningún dato.

---

## Precondiciones

- El Administrador ha iniciado sesión en el sistema (CU-US-02).
- El Administrador tiene el rol con permiso de escritura sobre el módulo Inflación.

## Postcondiciones

- Los datos de inflación quedan registrados en el sistema con su fuente, tipo y trazabilidad de quién y cuándo los registró.
- Los porcentajes de inflación registrados quedan disponibles como datos de solo lectura para los módulos consumidores (Sueldos, Demanda, Mantenimiento).
- Se registra un log de auditoría con la acción ejecutada (CU-US-04).

## Reglas de Negocio

- **RN-21:** Cada año puede tener un único registro de inflación (no puede haber dos valores para el mismo año).
- **RN-22:** El porcentaje de inflación debe estar en el rango de −10% a +20%, con precisión de al menos 2 decimales.
- **RN-23:** La inflación puede tomar valores negativos (deflación).
- **RN-24:** La fuente del dato es opcional pero recomendada para trazabilidad.
- **RN-25:** Los datos registrados en este módulo son de solo lectura para los módulos consumidores.
