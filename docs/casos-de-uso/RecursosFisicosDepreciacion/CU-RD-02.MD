# Caso de Uso: Calcular costos monetarios de necesidades proyectadas con inflación

## Identificación

| Campo            | Valor                                                               |
| ---------------- | ------------------------------------------------------------------- |
| Identificador    | CU-RD-02                                                            |
| Nombre           | Calcular costos monetarios de necesidades proyectadas con inflación |
| RF asociado      | RF-RD-02                                                            |
| Actor primario   | Planificador Académico                                              |
| Actor secundario | Sistema; Consultor                                                  |

## Descripción

El sistema calcula automáticamente el costo monetario de cada activo proyectado en períodos futuros aplicando el factor de inflación acumulado.

---

## Flujo Principal — Calcular costos monetarios

1. El Planificador accede al módulo.
2. El sistema verifica activos y factores de inflación.
3. El Planificador selecciona calcular costos.
4. El sistema obtiene factores de inflación.
5. El sistema calcula el costo monetario por período.
6. El sistema calcula subtotales por período.
7. El sistema guarda resultados.
8. El sistema muestra la tabla.

### Flujos alternos

**2a. No existen activos**

- El sistema muestra mensaje.
- Retorna al paso 1.

**4a. Inflación faltante**

- Se usa factor 1.0.
- Continúa cálculo.

**5a. Cantidad igual a 0**

- Costo calculado como 0.

**4b. Inflación negativa**

- Se usa factor 1.0.

---

## Precondiciones

- Usuario autenticado.
- Activos registrados.
- Factores de inflación disponibles.

## Postcondiciones

- Tabla de costos monetarios calculada.
- Totales disponibles para otros módulos.
- Auditoría registrada.

## Reglas de Negocio

- **RN-99:** Costo = Cantidad × Valor_unitario × Factor_inflación.
- **RN-100:** Inflación negativa se reemplaza por 1.0.
- **RN-101:** Cálculo automático en menos de 2 segundos.
- **RN-102:** Inversiones necesarias se exponen a otros módulos.
