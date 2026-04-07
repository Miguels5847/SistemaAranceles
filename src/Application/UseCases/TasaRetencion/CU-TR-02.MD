# Caso de Uso: Generar simulación por ciclo y calcular indicadores

## Identificacion

| Campo            | Valor                                                                                                                                          |
| ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| Identificador    | CU-TR-02                                                                                                                                       |
| Nombre           | Generar simulación por ciclo y calcular indicadores                                                                                            |
| RF asociado      | RF-US-03                                                                                                                                       |
| Actor primario   | Planificador Académico                                                                                                                         |
| Actor secundario | Sistema (cálculo de simulación por cohorte, generación de tabla, cálculo de indicadores, comparación con criterios), Consultor (visualización) |

## Descripcion

Permite al sistema calcular automáticamente el número de estudiantes por ciclo y período aplicando la tasa de retención en los ciclos 1 a N/2 y la tasa de graduación en los ciclos N/2+1 a N, manteniendo precisión decimal en todos los cálculos internos. Calcula los indicadores de retención acumulada Ret. y titulación acumulada Titu., los compara informativamente con los criterios de referencia configurados, y expone los resultados para el módulo Estudiantes.

---

## Flujo Principal - Generar simulación automática

1. El Planificador Académico accede al módulo de Tasa de Retención.
2. El sistema muestra las carreras con configuración registrada y la carrera actualmente seleccionada.
3. El Planificador Académico selecciona la carrera y elige la opción Generar Simulación.
4. El sistema recupera la configuración guardada: tasas, número de ciclos N, paralelos y estudiantes iniciales por período.
5. El sistema aplica la simulación ciclo a ciclo.
6. El sistema calcula los indicadores.
7. El sistema compara los indicadores obtenidos con los criterios de referencia configurados en CU-TR-01.
8. El sistema muestra la tabla de simulación con columnas Ciclo, Período 1 y Período 2, con los indicadores Ret. y Titu. al pie de la tabla.
9. El sistema muestra la comparación informativa, con indicadores visuales de color verde si cumple el criterio y amarillo/rojo si está por debajo.
10. El sistema registra la acción en el log de auditoría.

### Flujos alternos

**3a. No existe configuración guardada para la carrera seleccionada:**

- El sistema muestra mensaje: “No existe configuración registrada para esta carrera. Registre primero las tasas antes de generar la simulación”.
- El flujo retorna al paso 2.

**6a. El indicador Ret. queda por debajo del criterio de referencia de retención:**

- El sistema muestra nota informativa con color amarillo/rojo.
- El flujo continúa; es una advertencia informativa y los datos pueden guardarse y usarse en el módulo Estudiantes igualmente.

**6b. El indicador Titu. queda por debajo del criterio de referencia de titulación:**

- El sistema muestra nota informativa con color amarillo/rojo.
- La advertencia no bloquea el proceso.

**4a. Error al recuperar la configuración guardada:**

- El sistema muestra mensaje: “No se pudo recuperar la configuración de la carrera. Intente nuevamente o contacte al administrador”.
- El flujo retorna al paso 2.

---

## Flujo alterno - Consultar simulación existente

1. El Consultor accede al módulo de Tasa de Retención.
2. El sistema muestra la tabla de simulación ya generada para la carrera seleccionada con sus indicadores.
3. El Consultor visualiza los resultados.
4. No tiene opción de generar ni modificar la simulación.

---

## Precondiciones

- El usuario ha iniciado sesión en el sistema.
- Existe configuración guardada para la carrera seleccionada: tasas, ciclos, paralelos y estudiantes iniciales registrados.
- Los criterios de referencia para acreditación están configurados en el sistema por defecto o modificados por el Administrador.

## Postcondiciones

- La tabla de simulación con valores decimales por ciclo y período queda calculada y visible.
- Los indicadores Ret. y Titu. quedan calculados y disponibles.
- Los valores simulados quedan expuestos al módulo Estudiantes con precisión decimal completa.
- Se registra un log de auditoría con la acción ejecutada.

## Reglas de Negocio

- **RN-44:** La simulación mantiene precisión decimal en todos los cálculos internos; no se aplica redondeo en ninguna etapa del cálculo.
- **RN-45:** La zona de retención abarca los ciclos 1 a N/2; la zona de graduación abarca los ciclos N/2+1 a N.
- **RN-46:** Ret. = EstCicloN/2P1 / EstCiclo1P1 _ 100; Titu. = EstCicloNP1 / EstCiclo1P1 _ 100.
- **RN-47:** La comparación de los indicadores con los criterios de referencia es informativa y no bloqueante.
- **RN-48:** Las fórmulas de Ret. y Titu. están sujetas a revisión y podrán ajustarse conforme a los lineamientos del CACES.
- **RN-49:** Los valores simulados se exponen al módulo Estudiantes con la totalidad de sus decimales.
- **RN-50:** El sistema recalcula automáticamente la simulación cada vez que la configuración de la carrera es modificada.
