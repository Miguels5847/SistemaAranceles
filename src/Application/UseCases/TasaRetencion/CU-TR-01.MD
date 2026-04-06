# Caso de Uso: Configurar, registrar y validar tasas de retención y graduación

## Identificacion

| Campo            | Valor                                                                                                      |
| ---------------- | ---------------------------------------------------------------------------------------------------------- |
| Identificador    | CU-TR-01                                                                                                   |
| Nombre           | Configurar, registrar y validar tasas de retención y graduación                                            |
| RF asociado      | RF-TR-01                                                                                                   |
| Actor primario   | Planificador Académico                                                                                     |
| Actor secundario | Administrador configura criterios de referencia, Sistema (validaciones automáticas, registro de auditoría) |

## Descripcion

Permite al Planificador Académico registrar y configurar por carrera las tasas de retención y graduación, el número de ciclos, los paralelos por período y los estudiantes iniciales por paralelo. Permite al Administrador definir los criterios de referencia para acreditación. El sistema valida todos los datos ingresados, guarda la configuración y la expone como base para la simulación y el módulo Estudiantes.

---

## Flujo Principal - Registrar configuración de tasas

1. El Planificador Académico accede al módulo de Tasa de Retención.
2. El sistema muestra el formulario de configuración con todos los campos disponibles.
3. El Planificador Académico selecciona la carrera a configurar.
4. El Planificador Académico ingresa los siguientes datos:
   - Número de ciclos.
   - Número de paralelos por período.
   - Estudiantes iniciales por paralelo para Período 1 y Período 2.
   - Tasa de Retención.
   - Tasa de Graduación.
5. El Administrador configura o confirma los criterios de referencia para acreditación.
6. El Planificador Académico confirma el registro.
7. El sistema valida los datos ingresados.
8. El sistema guarda la configuración de la carrera.
9. El sistema registra la acción en el log de auditoría.
10. El sistema muestra confirmación de registro exitoso y deja los valores disponibles para simulación y consulta.

### Flujos alternos - Registrar configuración

**7a. Tasa de Retención o Graduación fuera del rango 0–100:**

- El sistema muestra mensaje: “La tasa ingresada debe estar entre 0 y 100”.
- El flujo retorna al paso 4 con los datos previamente ingresados.
- El guardado no queda bloqueado; la advertencia es informativa.

**7b. Número de ciclos inválido (menor a 2 o impar):**

- El sistema muestra mensaje: “El número de ciclos debe ser mayor o igual a 2 y debe ser un valor par”.
- El flujo retorna al paso 4.

**7c. Estudiantes iniciales igual a 0 o valor negativo:**

- El sistema muestra mensaje: “El número de estudiantes iniciales debe ser mayor a 0”.
- El flujo retorna al paso 4.

**7d. Número de paralelos menor a 1:**

- El sistema muestra mensaje: “El número de paralelos debe ser igual o mayor a 1 por período”.
- El flujo retorna al paso 4.

**3a. Carrera no registrada en el catálogo del sistema:**

- El sistema muestra mensaje: “La carrera seleccionada no existe en el catálogo. Verifique el registro de carreras”.
- El flujo retorna al paso 3.

**6a. El usuario cancela antes de confirmar:**

- El sistema descarta los datos ingresados y regresa al listado de carreras sin guardar ningún cambio.

---

## Flujo alterno - Configurar criterios de referencia

1. El Administrador accede a la sección de criterios de referencia dentro del módulo.
2. El sistema muestra los valores actuales de referencia.
3. El Administrador modifica los valores según la normativa o el ejercicio vigente.
4. El Administrador confirma el cambio.
5. El sistema guarda los nuevos criterios de referencia y los aplica a la comparación informativa en todas las simulaciones de la carrera.
6. El sistema registra la acción en el log de auditoría.

### Flujos alternos de configuración de criterios

**3a. Valores de criterio fuera del rango 0–100:**

- El sistema muestra mensaje: “Los criterios de referencia deben estar entre 0 y 100”.
- El flujo retorna al paso 3.

---

## Precondiciones

- El Planificador Académico o el Administrador han iniciado sesión en el sistema.
- El usuario tiene permiso de escritura sobre el módulo Tasa de Retención según su rol.
- La carrera a configurar existe en el catálogo del sistema.

## Postcondiciones

- La configuración de tasas, ciclos, paralelos, estudiantes iniciales y criterios de referencia queda guardada para la carrera seleccionada.
- Los valores quedan disponibles para la ejecución de la simulación y para el módulo Estudiantes.
- Se registra un log de auditoría con la acción ejecutada.

## Reglas de Negocio

- **RN-37:** Las tasas de retención y graduación deben estar comprendidas entre 0 y 100.
- **RN-38:** El número de ciclos debe ser 2 y par; valores impares o menores a 2 son inválidos.
- **RN-39:** Los estudiantes iniciales por paralelo deben ser mayores a 0; no se permiten valores nulos o negativos.
- **RN-40:** El número de paralelos por período debe ser 1; el CES puede autorizar apertura de paralelos adicionales según la demanda.
- **RN-41:** La tasa de retención aplica desde el ciclo 1 hasta el ciclo N/2; la tasa de graduación aplica desde el ciclo N/2+1 hasta el ciclo N.
- **RN-42:** Los criterios de referencia por defecto 65% retención y 100% titulación son informativos y no bloqueantes.
- **RN-43:** Solo el Administrador puede modificar los criterios de referencia para acreditación.
