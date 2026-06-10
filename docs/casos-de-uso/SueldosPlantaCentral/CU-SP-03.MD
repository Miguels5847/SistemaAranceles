# Caso de Uso: Calcular costos de planta central atribuidos a la carrera

## Identificacion

| Campo            | Valor                                                                                                                                                    |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Identificador    | CU-SP-03                                                                                                                                                 |
| Nombre           | Calcular costos de planta central atribuidos a la carrera                                                                                                |
| RF asociado      | RF-SP-03                                                                                                                                                 |
| Actor primario   | Administrador                                                                                                                                            |
| Actor secundario | Sistema (cálculo de peso por estudiantes, distribución proporcional, registro de auditoría), Planificador Académico (consulta), Consultor (solo lectura) |

## Descripcion

Permite al Administrador registrar los cargos de la planta central de la universidad con sus sueldos mensuales totales, y al sistema calcular la porción de esos costos que corresponde a la carrera, distribuida proporcionalmente según el número de estudiantes de la carrera respecto al total de estudiantes de la universidad. El peso de distribución cambia período a período conforme crecen los estudiantes proyectados.

---

## Flujo Principal - Registrar cargos de planta central y calcular costo atribuido

1. El Administrador accede al módulo de Sueldos y Planta Central, sección Planta Central.
2. El sistema muestra la tabla de cargos de planta central con los valores actuales, si existen.
3. El Administrador registra o edita por cada cargo de planta central:
   - Nombre del cargo.
   - Sueldo mensual total del cargo.
4. El Administrador ingresa o confirma el parámetro total de estudiantes de la universidad.
5. El Administrador confirma el registro.
6. El sistema lee desde el módulo Estudiantes los estudiantes proyectados por período.
7. El sistema calcula el costo de planta central atribuido a la carrera por período.
8. El peso de la carrera se recalcula automáticamente en cada período conforme varían los estudiantes proyectados.
9. Si el Administrador activa la opción de ajuste por inflación anual, el sistema aplica el factor de inflación correspondiente con protección anti-deflación.
10. El sistema guarda los resultados y registra la acción en el log de auditoría.
11. El sistema muestra la tabla de cargos de planta central con sueldo mensual, total semestral, peso por período y costo atribuido a la carrera por período.

### Flujos alternos

**3a. El Administrador ingresa un sueldo mensual igual a 0 o negativo para un cargo:**

- El sistema muestra mensaje: “El sueldo mensual del cargo debe ser mayor a 0”.
- El flujo retorna al paso 3.

**4a. El Administrador ingresa un total de estudiantes de la universidad igual a 0 o negativo:**

- El sistema muestra mensaje: “El total de estudiantes de la universidad debe ser mayor a 0”.
- El flujo retorna al paso 4.

**6a. No existen estudiantes proyectados para la carrera:**

- El sistema muestra advertencia.
- Se utilizará 0 como valor provisional.
- Genere primero la proyección de estudiantes CU-ES-01.
- El cálculo continúa sin bloqueo.

**3b. El Administrador decide importar la estructura salarial desde un archivo:**

- El sistema solicita el archivo en formato XLSX o CSV.
- El sistema valida el formato y los campos obligatorios: nombre cargo, sueldo mensual.
- Si el archivo es válido, importa y pobla la tabla automáticamente, retornando al paso 4.
- Si el archivo es inválido, muestra mensaje de error con el detalle del problema y retorna al paso 3.

**5a. El Administrador cancela antes de confirmar:**

- El sistema descarta los cambios y restaura los valores previos sin guardar.

---

## Flujo alterno - Consultar costos de planta central

1. El actor accede a la sección Planta Central del módulo.
2. El sistema muestra la tabla de cargos con sueldos, pesos por período y costos atribuidos a la carrera.
3. El actor visualiza los datos en modo solo lectura.
4. No tiene opción de registrar ni editar.

---

## Precondiciones

- El Administrador ha iniciado sesión en el sistema.
- Existen estudiantes proyectados por período para la carrera; si no existen, se usa 0 como provisional.
- El total de estudiantes de la universidad está configurado como parámetro editable por el Administrador.

## Postcondiciones

- Los cargos de planta central con sueldos mensuales quedan registrados en el sistema.
- El costo de planta central atribuido a la carrera por período/año queda calculado y disponible.
- Los valores quedan expuestos como entrada para CU-SP-04.
- Se registra un log de auditoría con la acción ejecutada.

## Reglas de Negocio

- **RN-84:** La fórmula de distribución de planta central es Costoperiodo = Totalmensualtodoscargos _ 6 _ estcarreraperiodo / estuniversidad.
- **RN-85:** El peso de distribución de la carrera es dinámico; se recalcula en cada período usando el estcarreraperiodo proyectado.
- **RN-86:** El total de estudiantes de la universidad, denominador del peso, es un parámetro editable únicamente por el Administrador.
- **RN-87:** La aplicación de inflación a los sueldos de planta central es opcional y configurable por el Administrador; si se activa, aplica las mismas reglas de protección anti-deflación definidas en RN-80.
- **RN-88:** El sistema acepta importación de estructura salarial desde archivo XLSX o CSV; en caso de conflicto entre valores importados y valores existentes, el Administrador debe confirmar la sobreescritura.
