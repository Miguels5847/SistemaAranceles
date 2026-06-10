# Caso de Uso: Proyectar inflación para años futuros

## Identificacion

| Campo            | Valor                                                           |
| ---------------- | --------------------------------------------------------------- |
| Identificador    | CU-IN-03                                                        |
| Nombre           | Proyectar inflación para años futuros                           |
| RF asociado      | RF-IN-03                                                        |
| Actor primario   | Administrador                                                   |
| Actor secundario | Sistema (cálculo de regresión lineal, generación de proyección) |

## Descripcion

Permite al sistema calcular automáticamente una proyección de inflación para los años del horizonte de la carrera que no tengan dato histórico registrado, utilizando regresión lineal (mínimos cuadrados) sobre los datos históricos disponibles, con la posibilidad de que el usuario ajuste manualmente cualquier valor proyectado.

---

## Flujo Principal - Generar proyección automática

1. El Administrador accede al módulo de Inflación.
2. El sistema muestra la tabla histórica de inflación con datos registrados y proyectados (si existen).
3. El Administrador selecciona la opción "Proyectar inflación".
4. El sistema solicita el rango de años a proyectar: año inicio y año fin del horizonte de la carrera (ej. 2023–2028).
5. El Administrador ingresa el rango y confirma.
6. El sistema identifica los años del horizonte que no tienen dato histórico registrado.
7. El sistema calcula la regresión lineal (mínimos cuadrados) sobre todos los datos históricos disponibles.
8. El sistema genera los valores proyectados para cada año faltante.
9. El sistema muestra la tabla actualizada con los valores proyectados marcados como tipo "Proyectado" y fuente "Regresión lineal", diferenciados visualmente de los datos históricos.
10. El sistema muestra el gráfico de tendencia con la línea de datos históricos y la línea de proyección superpuesta.
11. El sistema registra la acción en el log de auditoría (CU-US-04).

### Flujos alternos - Generar proyección

**5a. Todos los años del horizonte ya tienen dato histórico:**

- El sistema muestra mensaje "Todos los años del rango seleccionado ya tienen datos registrados. No se requiere proyección".
- El flujo retorna al paso 2.

**7a. Datos históricos insuficientes:**

- El sistema detecta que hay menos de 2 datos históricos.
- Muestra mensaje "Se requieren al menos 2 datos históricos para calcular la proyección".
- El flujo retorna al paso 2.

**5b. Rango inválido:**

- El año fin es menor o igual al año inicio.
- El sistema muestra mensaje "El año fin debe ser mayor al año inicio".
- El flujo retorna al paso 4.

**9a. Valores proyectados fuera de rango:**

- El sistema muestra advertencia "Algunos valores proyectados están fuera del rango esperado. Revise y ajuste manualmente si es necesario".
- Permite guardar igualmente, pero resalta los valores atípicos.

---

## Flujo alterno - Ajuste manual de proyección

1. El Administrador visualiza la tabla con los valores proyectados.
2. El Administrador selecciona un valor proyectado y elige la opción "Editar".
3. El sistema muestra el formulario de edición con el valor actual de la proyección.
4. El Administrador ingresa el nuevo valor y confirma.
5. El sistema valida que el nuevo valor esté dentro del rango −10% a +20%.
6. El sistema guarda el cambio, actualiza la fuente a "Ajuste manual" y conserva el tipo "Proyectado".
7. El sistema registra la acción en el log de auditoría (CU-US-04).
8. El sistema actualiza la tabla y el gráfico de tendencia.

### Flujos alternos - Ajuste manual

**5a. Valor fuera de rango:**

- El sistema muestra mensaje "El valor de inflación debe estar entre −10% y +20%".
- El flujo retorna al paso 3.

---

## Flujo alterno - Regenerar proyección

1. El Administrador selecciona la opción "Regenerar proyección".
2. El sistema solicita confirmación: "Regenerar sobrescribirá todos los valores proyectados actuales (incluyendo ajustes manuales). ¿Desea continuar?"
3. El Administrador confirma.
4. El sistema recalcula la regresión lineal con los datos históricos actuales y reemplaza todos los valores proyectados.
5. El sistema registra la acción en el log de auditoría (CU-US-04).
6. El sistema actualiza la tabla y el gráfico.

### Flujos alternos - Regeneración

**2a. El Administrador cancela:**

- El sistema mantiene los valores proyectados actuales sin cambios.

---

## Precondiciones

- El Administrador ha iniciado sesión en el sistema (CU-US-02).
- El Administrador tiene permiso de escritura sobre el módulo Inflación.
- Existen al menos 2 datos históricos de inflación registrados en el sistema (CU-IN-01).
- Se ha definido el año base y horizonte de proyección de la carrera.

## Postcondiciones

- Los años del horizonte que no tenían dato histórico quedan con un valor proyectado calculado por regresión lineal o ajustado manualmente.
- Cada valor proyectado queda marcado con tipo "Proyectado" y fuente "Regresión lineal" o "Ajuste manual" según corresponda.
- El gráfico de tendencia refleja tanto los datos históricos como la línea de proyección.
- Los módulos consumidores (Sueldos, Demanda, Mantenimiento) disponen de porcentajes de inflación para todo el horizonte de la carrera.
- Se registra un log de auditoría con la acción ejecutada (CU-US-04).

## Reglas de Negocio

- **RN-31:** La proyección utiliza regresión lineal (mínimos cuadrados) calculada sobre todos los datos históricos disponibles.
- **RN-32:** Se requieren al menos 2 datos históricos para calcular la regresión.
- **RN-33:** Los valores proyectados se distinguen visualmente de los datos históricos.
- **RN-34:** Cualquier valor proyectado puede ser ajustado manualmente; al hacerlo la fuente cambia a "Ajuste manual".
- **RN-35:** Regenerar la proyección sobrescribe todos los valores proyectados anteriores, incluyendo ajustes manuales.
- **RN-36:** La inflación se registra de forma anual y aplica el mismo valor para ambos períodos del año.
