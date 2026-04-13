# Caso de Uso: Consultar y editar configuración guardada

## Identificacion

| Campo            | Valor                                                                                                                              |
| ---------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| Identificador    | CU-TR-03                                                                                                                           |
| Nombre           | Consultar y editar configuración guardada                                                                                          |
| RF asociado      | RF-TR-03                                                                                                                           |
| Actor primario   | Planificador Académico                                                                                                             |
| Actor secundario | Sistema (recuperación de datos, validaciones, recálculo automático, trazabilidad, registro de auditoría), Consultor (solo lectura) |

## Descripcion

Permite al Planificador Académico consultar la configuración registrada de tasas, ciclos, paralelos, estudiantes iniciales y criterios de referencia por carrera, y editarla cuando sea necesario. Al guardar cualquier cambio, el sistema recalcula automáticamente la simulación y los indicadores, registra trazabilidad usuario, fecha y hora, y muestra de forma informativa si los nuevos indicadores están por debajo del criterio de referencia.

---

## Flujo Principal - Consultar configuración

1. El actor Planificador Académico o Consultor accede al módulo de Tasa de Retención.
2. El sistema muestra el listado de carreras con configuración registrada.
3. El actor selecciona una carrera.
4. El sistema recupera y presenta la configuración actual: tasas de retención y graduación, número de ciclos, paralelos por período, estudiantes iniciales, criterios de referencia, tabla de simulación generada e indicadores Ret. y Titu.

### Flujos alternos

**3a. La carrera seleccionada no tiene configuración registrada:**

- El sistema muestra mensaje: “Esta carrera no tiene configuración registrada. Utilice la opción de registro CU-TR-01 para ingresar los datos iniciales”.
- El flujo retorna al paso 2.

---

## Flujo alterno - Editar configuración

1. El Planificador Académico, desde la vista de consulta, selecciona la opción Editar.
2. El sistema habilita los campos del formulario para edición, mostrando los valores actuales de cada campo.
3. El Planificador Académico modifica uno o más campos y confirma.
4. El sistema valida los nuevos valores aplicando las mismas reglas definidas en RF-TR-01 / CU-TR-01.
5. El sistema guarda los cambios.
6. El sistema recalcula automáticamente la simulación completa e indicadores con los nuevos valores.
7. Si los indicadores recalculados quedan por debajo del criterio de referencia configurado, el sistema muestra nota informativa no bloqueante con indicador visual de color amarillo/rojo.
8. El sistema registra trazabilidad del cambio: nombre de usuario, fecha y hora de la modificación.
9. El sistema registra la acción en el log de auditoría.
10. El sistema muestra la vista actualizada con la configuración nueva, los indicadores recalculados, la confirmación de guardado y el registro de última modificación.

### Flujos alternos de edición

**4a. Validación fallida en uno o más campos editados:**

- El sistema aplica los mismos flujos alternos de validación definidos en CU-TR-01.
- El flujo retorna al paso 3.

**1a. Un Consultor intenta acceder a la opción de edición:**

- El sistema no muestra la opción Editar para usuarios con rol Consultor.
- Si el intento se realiza por otro medio, el sistema muestra: “No tiene permisos para editar esta configuración. Contacte al Planificador Académico o Administrador”.
- El Consultor continúa en modo solo lectura.

**3a. El Planificador Académico cancela la edición antes de confirmar:**

- El sistema descarta todos los cambios realizados y restaura los valores previos.
- La vista regresa al estado de consulta sin ningún cambio registrado.

**6a. Error en el recálculo automático de la simulación:**

- El sistema muestra mensaje: “No se pudo recalcular la simulación automáticamente. Los datos han sido guardados; regenere la simulación manualmente CU-TR-02”.
- El flujo continúa desde el paso 8 con la trazabilidad registrada.

---

## Flujo alterno - Regenerar simulación tras edición

1. El Planificador Académico, tras editar y guardar, selecciona la opción Recalcular Simulación.
2. El sistema solicita confirmación.
3. El Planificador Académico confirma.
4. El sistema ejecuta el recálculo completo según la lógica de CU-TR-02.
5. El sistema registra la acción en el log de auditoría.
6. El sistema actualiza la tabla y los indicadores en pantalla.

### Flujos alternos de regeneración

**3a. El Planificador Académico cancela:**

- El sistema mantiene la última simulación calculada sin cambios.

---

## Precondiciones

- El usuario ha iniciado sesión en el sistema.
- Existe al menos una carrera con configuración registrada en el módulo CU-TR-01.
- Para editar, el usuario tiene rol Planificador Académico o Administrador con permiso de escritura sobre el módulo Tasa de Retención.

## Postcondiciones

- La configuración de la carrera queda actualizada con los nuevos valores ingresados.
- La simulación y los indicadores Ret. y Titu. quedan recalculados automáticamente y visibles.
- Se registra trazabilidad de la modificación: usuario, fecha y hora del cambio.
- Los valores actualizados quedan disponibles para el módulo Estudiantes.
- Se registra un log de auditoría con la acción ejecutada.

## Reglas de Negocio

- **RN-51:** Solo los usuarios con rol Planificador Académico o Administrador pueden editar la configuración de tasas; el Consultor tiene acceso exclusivo de lectura.
- **RN-52:** Toda edición exitosa queda registrada con trazabilidad completa: nombre de usuario, fecha y hora de la última modificación.
- **RN-53:** Al guardar cualquier cambio en la configuración, el sistema recalcula automáticamente la simulación y los indicadores en menos de 2 segundos.
- **RN-54:** Las advertencias informativas sobre incumplimiento de criterios de referencia no bloquean el guardado ni la exportación de datos al módulo Estudiantes.
- **RN-55:** El historial de trazabilidad usuario-fecha-hora se conserva para auditoría y no puede ser eliminado por el Planificador Académico.
