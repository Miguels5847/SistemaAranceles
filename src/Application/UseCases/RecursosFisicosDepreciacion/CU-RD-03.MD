# Caso de Uso: Calcular depreciación semestral por activo

## Identificación

| Campo            | Valor                                      |
| ---------------- | ------------------------------------------ |
| Identificador    | CU-RD-03                                   |
| Nombre           | Calcular depreciación semestral por activo |
| RF asociado      | RF-RD-03                                   |
| Actor primario   | Planificador Académico                     |
| Actor secundario | Administrador; Sistema; Consultor          |

## Descripción

El sistema calcula la depreciación semestral de cada activo fijo usando el método de línea recta.

---

## Flujo Principal — Calcular depreciación

1. El Planificador accede al módulo.
2. El sistema verifica activos y parámetros.
3. El Planificador selecciona calcular depreciación.
4. El sistema aplica método línea recta.
5. El sistema determina inicio y fin de depreciación.
6. El sistema guarda resultados.
7. El sistema muestra la tabla.

### Flujos alternos

**2a. No existen activos**

- Se muestra mensaje.
- Retorna al paso 1.

**2b. Vida útil no configurada**

- Se usa valor por defecto.

**4a. Valor igual al residual**

- Depreciación = 0.

**5a. Activo completa vida útil**

- Depreciación se detiene.

---

## Precondiciones

- Usuario autenticado.
- Activos registrados.
- Parámetros configurados.

## Postcondiciones

- Tabla de depreciación calculada.
- Subtotales disponibles.
- Auditoría registrada.

## Reglas de Negocio

- **RN-103:** Depreciación línea recta.
- **RN-104:** Valor residual 5% configurable.
- **RN-105:** Depreciación inicia según período.
- **RN-106:** Depreciación se detiene al finalizar vida útil.
- **RN-107:** Vida útil configurable por administrador.
