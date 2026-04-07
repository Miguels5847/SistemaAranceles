```md id="9e9qsg"
# Caso de Uso: Distribuir presupuestos institucionales

## Identificación

| Campo            | Valor                                   |
| ---------------- | --------------------------------------- |
| Identificador    | CU-DI-03                                |
| Nombre           | Distribuir presupuestos institucionales |
| RF asociado      | RF-DI-03                                |
| Actor primario   | Planificador Académico                  |
| Actor secundario | Sistema                                 |

## Descripción

Distribuye presupuestos institucionales proporcionalmente a la carrera.

---

## Flujo Principal — Registrar y distribuir

1. Usuario accede sección.
2. Sistema muestra presupuestos.
3. Usuario modifica valores.
4. Sistema calcula distribución.
5. Sistema aplica inflación.
6. Sistema muestra resultados.
7. Sistema registra auditoría.

### Flujos alternos

**3a. Valores negativos**

- Error.

**4a. Datos no disponibles**

- Mensaje.

---

## Precondiciones

- Estudiantes y docentes disponibles.
- Inflación registrada.

## Postcondiciones

- Presupuestos distribuidos.

## Reglas de negocio

- **RN-142:** Distribución proporcional.
- **RN-143:** Ajuste por inflación.
- **RN-144:** Recalculo automático.
```
