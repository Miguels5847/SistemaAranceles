# Caso de Uso: Configurar cargos, sueldos y beneficios sociales de la facultad

## Identificacion

| Campo            | Valor                                                                                                                                                                                                                   |
| ---------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Identificador    | CU-SP-01                                                                                                                                                                                                                |
| Nombre           | Configurar cargos, sueldos y beneficios sociales de la facultad                                                                                                                                                         |
| RF asociado      | RF-SP-01                                                                                                                                                                                                                |
| Actor primario   | Planificador Académico                                                                                                                                                                                                  |
| Actor secundario | Administrador configura valor del Décimo Cuarto, Sistema (cálculo automático de beneficios sociales y peso proporcional, lectura de docentes desde módulo Estudiantes, registro de auditoría), Consultor (solo lectura) |

## Descripcion

Permite al Planificador Académico registrar o editar los cargos de la facultad con sus sueldos base y número de personas, además de calcular automáticamente los beneficios sociales y el peso proporcional de cada cargo. El Administrador puede ajustar el valor del Décimo Cuarto según cambios normativos. El sistema guarda la configuración y deja los valores listos como entrada para los módulos posteriores.

---

## Flujo Principal - Registrar cargos de la facultad

1. El Planificador Académico accede al módulo de Sueldos y Planta Central y selecciona la carrera.
2. El sistema muestra la tabla de cargos de la facultad con los valores actuales, si existen, y los campos disponibles para edición.
3. El Planificador Académico ingresa o edita por cada cargo:
   - Nombre del cargo.
   - Sueldo base mensual.
   - Número de personas.
4. El Planificador Académico ingresa el parámetro de estudiantes de la Unidad Académica.
5. El Planificador Académico confirma el registro.
6. El sistema calcula automáticamente los beneficios sociales por cargo.
7. El sistema calcula el peso proporcional de cada cargo.
8. El sistema guarda la configuración y registra la acción en el log de auditoría.
9. El sistema muestra la tabla de cargos actualizada con sueldos, beneficios calculados y peso configurado por cargo.

### Flujos alternos

**3a. El Planificador Académico ingresa un sueldo base igual a 0 o negativo:**

- El sistema muestra mensaje: “El sueldo base debe ser mayor a 0”.
- El flujo retorna al paso 3 con los demás datos conservados.

**3b. El Planificador Académico ingresa un N de personas igual a 0 o negativo para un cargo administrativo:**

- El sistema muestra mensaje: “El número de personas debe ser mayor a 0”.
- El flujo retorna al paso 3.

**4a. El Planificador Académico ingresa un valor de estudiantes UA igual a 0 o negativo:**

- El sistema muestra mensaje: “El número de estudiantes de la Unidad Académica debe ser mayor a 0”.
- El flujo retorna al paso 4.

**3c. El número de docentes del módulo Estudiantes aún no ha sido calculado:**

- El sistema muestra advertencia informativa.
- Se muestra 0 como valor provisional.
- El registro puede guardarse sin bloqueo.

**5a. El Planificador Académico cancela antes de confirmar:**

- El sistema descarta los cambios y restaura los valores previos sin guardar.

---

## Flujo alterno - Editar valor del Décimo Cuarto

1. El Administrador accede a los parámetros globales del módulo.
2. El sistema muestra el valor actual del Décimo Cuarto por defecto.
3. El Administrador modifica el valor según la actualización legal vigente y confirma.
4. El sistema valida que el nuevo valor sea mayor a 0.
5. El sistema guarda el parámetro y recalcula automáticamente todos los beneficios afectados.
6. El sistema registra la acción en el log de auditoría.

### Flujos alternos de edición

**4a. El valor ingresado es 0 o negativo:**

- El sistema muestra mensaje: “El valor del Décimo Cuarto debe ser mayor a 0”.
- Se mantiene el valor anterior.
- El flujo retorna al paso 3.

---

## Precondiciones

- El usuario ha iniciado sesión en el sistema.
- La carrera seleccionada existe en el catálogo del sistema.
- Para leer docentes de forma automática, el módulo Estudiantes tiene calculados los docentes requeridos por tipo; si no existe, se acepta valor provisional 0.

## Postcondiciones

- Los cargos de la facultad con sueldos, número de personas, beneficios sociales calculados y peso proporcional quedan guardados por carrera.
- Los valores quedan disponibles como entrada para CU-SP-02.
- Se registra un log de auditoría con la acción ejecutada.

## Reglas de Negocio

- **RN-74:** El número de docentes PhD, Mgs., Parcial y Técnico proviene del módulo Estudiantes y no es editable en este módulo; solo se visualiza como referencia.
- **RN-75:** El peso proporcional de los cargos administrativos de la facultad es estcarreraperiodo / estUA; el Director de Carrera y todos los docentes tienen peso fijo 1.0.
- **RN-76:** Los beneficios sociales se calculan automáticamente al registrar o cambiar el sueldo base: DXIII = Sueldo / 12, DXIV = 450 / 12, Vacaciones = Sueldo / 24, Fondo Reserva = Sueldo / 8.33, Aporte Patronal = Sueldo / 11.15.
- **RN-77:** El valor del Décimo Cuarto, 450 anual o 37.50 mensual, corresponde al valor legal vigente en Ecuador y es configurable únicamente por el Administrador ante cambios normativos.
- **RN-78:** Los estudiantes UA representan únicamente a estudiantes de otras carreras de la misma facultad; la carrera proyectada no se incluye en ese denominador.
