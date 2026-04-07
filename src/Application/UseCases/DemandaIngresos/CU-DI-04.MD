```md id="xg8a6g"
# Caso de Uso: Registrar materiales y calcular costos con inflación

## Identificación

| Campo            | Valor                                                |
| ---------------- | ---------------------------------------------------- |
| Identificador    | CU-DI-04                                             |
| Nombre           | Registrar materiales y calcular costos con inflación |
| RF asociado      | RF-DI-04                                             |
| Actor primario   | Planificador Académico                               |
| Actor secundario | Sistema                                              |

## Descripción

Permite registrar materiales y calcular costos con inflación.

---

## Flujo Principal — Registrar materiales

1. Usuario accede sección.
2. Sistema muestra catálogo.
3. Usuario agrega material.
4. Sistema muestra formulario.
5. Usuario confirma.
6. Sistema guarda.
7. Sistema proyecta cantidades.
8. Sistema calcula costos.
9. Sistema muestra tabla.
10. Sistema registra auditoría.

### Flujos alternos

**5a. Campos incompletos**

- Mensaje.

**7a. Sin estudiantes**

- Advertencia.

---

## Precondiciones

- Estudiantes proyectados.
- Inflación registrada.

## Postcondiciones

- Materiales registrados.
- Costos calculados.

## Reglas de negocio

- **RN-145:** Cantidad proporcional.
- **RN-146:** Costo con inflación.
- **RN-147:** Cantidad fija permitida.
- **RN-148:** Recalculo automático.
```
