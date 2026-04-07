```md id="r4wxke"
# Caso de Uso: Consolidar demanda proyectada

## Identificación

| Campo            | Valor                              |
| ---------------- | ---------------------------------- |
| Identificador    | CU-DI-05                           |
| Nombre           | Consolidar demanda proyectada      |
| RF asociado      | RF-DI-05                           |
| Actor primario   | Sistema                            |
| Actor secundario | Planificador Académico / Consultor |

## Descripción

Consolida toda la información de demanda en una vista única.

---

## Flujo Principal — Consolidar demanda

1. Usuario accede vista.
2. Sistema reúne datos.
3. Sistema muestra tablas.
4. Usuario navega.

### Flujos alternos

**2a. Datos parciales**

- Se muestran pendientes.

---

## Precondiciones

- Arancel configurado.
- Estudiantes proyectados.

## Postcondiciones

- Vista consolidada disponible.

## Reglas de negocio

- **RN-149:** Actualización automática.
- **RN-150:** Solo lectura.
```
