# Caso de Uso: Consolidar costos totales de personal por período

## Identificacion

| Campo            | Valor                                                                                     |
| ---------------- | ----------------------------------------------------------------------------------------- |
| Identificador    | CU-SP-04                                                                                  |
| Nombre           | Consolidar costos totales de personal por período                                         |
| RF asociado      | RF-SP-04                                                                                  |
| Actor primario   | Sistema de consolidación automática                                                       |
| Actor secundario | Planificador Académico (visualización y aprobación del resumen), Consultor (solo lectura) |

## Descripcion

El sistema consolida automáticamente los costos de sueldos de la facultad calculados en CU-SP-02 y los costos de planta central calculados en CU-SP-03, generando un resumen único por período con el total de costos de personal atribuibles a la carrera. Además, desglosa los costos según su destino contable para el módulo Costos y Gastos.

---

## Flujo Principal - Consolidar y desplegar costos totales de personal

1. El Planificador Académico accede al módulo de Sueldos y Planta Central y selecciona la opción Ver Consolidado.
2. El sistema verifica que existan los resultados de CU-SP-02 y CU-SP-03 para la carrera.
3. El sistema calcula el total de personal por período.
4. El sistema desglosa el consolidado según el destino contable para el módulo Costos y Gastos.
5. El sistema expone el consolidado y el desglose al módulo Costos y Gastos y al módulo Capital de Trabajo.
6. El sistema registra la acción en el log de auditoría.
7. El sistema muestra el resumen consolidado por período con el desglose por destino contable y el total disponible para los módulos consumidores.

### Flujos alternos

**2a. No existe cálculo de sueldos de la facultad:**

- El sistema muestra mensaje: “No se encontraron sueldos de la facultad calculados para esta carrera. Complete el cálculo de sueldos CU-SP-02 antes de consolidar”.
- El flujo retorna al paso 1.

**2b. No existe cálculo de planta central:**

- El sistema muestra advertencia informativa.
- El consolidado se genera únicamente con los sueldos de la facultad.
- Registre la planta central CU-SP-03 para un cálculo completo.
- El consolidado se genera parcialmente sin bloqueo.

**3a. Error aritmético en la consolidación:**

- El sistema muestra mensaje: “Se detectó un valor nulo en el período Pi durante la consolidación. Verifique los cálculos de sueldos o planta central para ese período”.
- El consolidado se genera para los períodos sin error.

---

## Flujo alterno - Recalcular consolidado ante cambios en módulos anteriores

1. El sistema detecta un cambio en los datos fuente: sueldos de facultad, planta central, docentes o inflación.
2. El sistema recalcula automáticamente el consolidado de costos de personal.
3. El sistema actualiza el desglose por destino contable y el total expuesto a los módulos Costos y Gastos y Capital de Trabajo.
4. El sistema registra la acción en el log de auditoría.

---

## Precondiciones

- El usuario ha iniciado sesión en el sistema.
- Existe al menos el cálculo de sueldos de la facultad para la carrera.
- Los costos de planta central son opcionales pero recomendados para un consolidado completo.

## Postcondiciones

- El resumen consolidado de costos totales de personal por período queda calculado y almacenado.
- El desglose por destino contable Costos por Servicios y Gastos de Administración queda disponible para el módulo Costos y Gastos.
- El total de personal queda disponible para el módulo Capital de Trabajo.
- Se registra un log de auditoría con la acción ejecutada.

## Reglas de Negocio

- **RN-89:** Totalpersonalperiodo = Sueldosfacultadperiodo + Plantacentralperiodo; la consolidación no aplica ningún factor adicional, solo suma los dos componentes calculados previamente.
- **RN-90:** Los sueldos de los cuatro tipos de docentes se clasifican como Costos por Servicios en el módulo Costos y Gastos.
- **RN-91:** Los sueldos de los cargos administrativos de la facultad y el costo de planta central se clasifican como Gastos de Administración en el módulo Costos y Gastos.
- **RN-92:** El consolidado se recalcula automáticamente ante cualquier cambio en los módulos fuente Sueldos facultad, Planta central, Estudiantes o Inflación, garantizando consistencia en tiempo real.
- **RN-93:** Si los costos de planta central no están registrados, el consolidado se genera solo con sueldos de facultad, mostrando advertencia informativa pero sin bloquear el flujo hacia los módulos consumidores.
