```md
# Caso de Uso: Calcular depreciación acumulada por período

## Identificación

| Campo            | Valor                                                                         |
| ---------------- | ----------------------------------------------------------------------------- |
| Identificador    | CU-RD-04                                                                      |
| Nombre           | Calcular depreciación acumulada por período                                   |
| RF asociado      | RF-RD-04                                                                      |
| Actor primario   | Sistema (cálculo automático de acumulación progresiva)                        |
| Actor secundario | Planificador Académico (visualización y aprobación); Consultor (solo lectura) |

## Descripción

El sistema calcula la depreciación acumulada por período sumando progresivamente las depreciaciones semestrales de todos los activos (iniciales y adquiridos en el tiempo). La acumulación incluye los activos desde su período de inicio de depreciación; cuando un activo completa su vida útil, su depreciación semestral se detiene, pero la acumulada permanece fija. Los totales alimentan al módulo Costos y Gastos (gasto depreciación del período) y al módulo Balance (depreciación acumulada total).

---

## Flujo Principal — Calcular y mostrar depreciación acumulada

1. El Planificador Académico accede al módulo de Recursos Físicos y Depreciación y selecciona la carrera.
2. El sistema verifica que existan depreciaciones semestrales calculadas por activo (CU-RD-03).
3. El Planificador Académico selecciona la opción "Calcular Depreciación Acumulada".
4. El sistema calcula la depreciación acumulada período a período con suma progresiva:
   - Dep_acumulada_P1 = Dep_semestral_P1.
   - Dep_acumulada_Pi = Dep_acumulada_P(i-1) + Dep_semestral_Pi.
5. La acumulación incorpora:
   - Activos iniciales (año 0).
   - Activos adquiridos en períodos futuros.
   - Activos que completaron su vida útil.
6. El sistema calcula el total de depreciación del período para Costos y Gastos.
7. El sistema calcula el total de depreciación acumulada para el Balance.
8. El sistema guarda los resultados y registra la acción en el log de auditoría.
9. El sistema muestra la tabla de depreciación acumulada.

### Flujos alternos

**2a. No existen depreciaciones semestrales calculadas**

- El sistema muestra mensaje informativo.
- El flujo retorna al paso 1.

**4a. Todos los activos tienen depreciación semestral igual a 0**

- El sistema mantiene la depreciación acumulada igual al período anterior.
- Se muestra nota informativa.

**4b. Se agregan nuevos activos o se recalculan depreciaciones**

- El sistema detecta cambios.
- Se recalcula automáticamente la depreciación acumulada.

---

## Flujo alterno — Recalcular acumulado ante cambios

1. El sistema detecta cambios en activos o depreciaciones.
2. El sistema recalcula automáticamente.
3. El sistema actualiza valores para Costos y Gastos y Balance.
4. El sistema registra auditoría.

---

## Precondiciones

- Usuario autenticado.
- Existen depreciaciones semestrales calculadas por activo.

## Postcondiciones

- Tabla de depreciación acumulada calculada y almacenada.
- Total de depreciación del período disponible para Costos y Gastos.
- Total acumulado disponible para Balance.
- Auditoría registrada.

## Reglas de Negocio

- **RN-108:** Dep_acumulada_Pi = Dep_acumulada_P(i-1) + Dep_semestral_Pi.
- **RN-109:** Activos futuros se incorporan desde su período de adquisición.
- **RN-110:** Activos que completan vida útil dejan de depreciarse.
- **RN-111:** Depreciación del período se envía a Costos y Gastos.
- **RN-112:** Depreciación acumulada se envía al Balance.
- **RN-113:** Recalculo automático ante cambios en menos de 2 segundos.
```
