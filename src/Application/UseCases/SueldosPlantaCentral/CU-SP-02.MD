# Caso de Uso: Calcular sueldos semestrales de la facultad con peso e inflación

## Identificacion

| Campo            | Valor                                                                                                                             |
| ---------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| Identificador    | CU-SP-02                                                                                                                          |
| Nombre           | Calcular sueldos semestrales de la facultad con peso e inflación                                                                  |
| RF asociado      | RF-SP-02                                                                                                                          |
| Actor primario   | Planificador Académico                                                                                                            |
| Actor secundario | Sistema (cálculo semestral por cargo con peso e inflación, recálculo automático, registro de auditoría), Consultor (solo lectura) |

## Descripcion

Permite al sistema calcular el costo semestral atribuible a la carrera por cada cargo de la facultad, aplicando el peso proporcional configurado en CU-SP-01, la fórmula de costo semestral completa y el factor de inflación acumulado proveniente del módulo Inflación. La regla de protección anti-deflación garantiza que los sueldos nunca disminuyan ante inflaciones negativas.

---

## Flujo Principal - Calcular sueldos semestrales con inflación

1. El Planificador Académico accede al módulo de Sueldos y Planta Central y selecciona la carrera.
2. El sistema verifica que existan cargos configurados con sueldos y beneficios.
3. El sistema lee desde el módulo Inflación el factor de inflación anual para cada año del horizonte, con protección anti-deflación.
4. El sistema lee desde el módulo Estudiantes los estudiantes proyectados por período para el peso proporcional de administrativos y el número de docentes requeridos por tipo.
5. El Planificador Académico selecciona la opción Calcular Sueldos Semestrales.
6. El sistema calcula por cada cargo y período el total semestral.
7. El sistema aplica el factor de inflación acumulado para el año correspondiente.
8. El peso de los cargos administrativos se recalcula período a período, ya que depende de estcarreraperiodo.
9. El sistema guarda los resultados y registra la acción en el log de auditoría.
10. El sistema muestra la tabla de costos semestrales por cargo y período, y el total de sueldos de la facultad por período.

### Flujos alternos

**2a. No existen cargos configurados para la carrera:**

- El sistema muestra mensaje: “No se encontraron cargos de facultad configurados para esta carrera. Registre primero los cargos y sueldos CU-SP-01”.
- El flujo retorna al paso 1.

**3a. No existe factor de inflación para algún año del horizonte:**

- El sistema muestra advertencia informativa.
- Se utilizará factor 1.0 sin ajuste para ese año.
- El cálculo continúa sin bloqueo.

**4a. El número de docentes requeridos aún no está calculado:**

- El sistema muestra advertencia.
- Se utilizará 0 para los tipos de docentes pendientes.
- El cálculo continúa con los datos disponibles.

**8a. Los estudiantes proyectados para un período son 0:**

- El sistema muestra advertencia informativa.
- El peso de los cargos administrativos será 0 para ese período, resultando en costo 0 para esos cargos.
- El cálculo continúa sin bloqueo.

---

## Precondiciones

- El usuario ha iniciado sesión en el sistema.
- Los cargos de la facultad con sueldos y beneficios están configurados.
- Existen factores de inflación en el módulo Inflación para al menos el año base; si faltan años, se usa factor 1.0.
- Existen estudiantes proyectados por período para calcular los pesos de los administrativos.

## Postcondiciones

- La tabla de costos semestrales por cargo y período queda calculada con peso e inflación aplicados.
- El total de sueldos de la facultad por período queda disponible como entrada para CU-SP-04.
- Se registra un log de auditoría con la acción ejecutada.

## Reglas de Negocio

- **RN-79:** La fórmula de costo semestral depende del tipo de contrato:
  - **(a) PhD / Mgs / Medio Tiempo / Administrativo:** `Total = (Sueldo + FondoReserva + AportePatronal) × 6 + DXIII + DXIV + Vacaciones`, multiplicado por `N × Peso × FactorInflacion`.
  - **(b) Tiempo Parcial:** `Total = TarifaHora × HorasAsignadasSemana × SemanasPorMes × 6 × N × Peso × FactorInflacion` (sin beneficios sociales — servicios profesionales).
- **RN-80:** Si el factor de inflación del año es negativo, el sistema lo reemplaza por 1.0; los sueldos nunca se reducen por efecto de la inflación negativa.
- **RN-81:** El peso de los cargos administrativos es dinámico; se recalcula en cada período usando el estcarreraperiodo proyectado.
- **RN-82:** El factor de inflación aplicado es acumulado: para el año 2 se aplica 1 + inf1 + 1 + inf2, y así sucesivamente, conforme al esquema del módulo Inflación.
- **RN-83:** El recálculo se ejecuta automáticamente en menos de 2 segundos ante cualquier cambio en cargos/sueldos, docentes requeridos (incluyendo Medio Tiempo), horas asignadas a Tiempo Parcial, estudiantes proyectados o factores de inflación.
