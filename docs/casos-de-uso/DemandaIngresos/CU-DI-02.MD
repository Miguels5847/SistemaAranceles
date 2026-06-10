```md id="8hhx8u"
# Caso de Uso: Calcular ingresos proyectados, becas y seguro estudiantil

## Identificación

| Campo            | Valor                                                     |
| ---------------- | --------------------------------------------------------- |
| Identificador    | CU-DI-02                                                  |
| Nombre           | Calcular ingresos proyectados, becas y seguro estudiantil |
| RF asociado      | RF-DI-02                                                  |
| Actor primario   | Sistema                                                   |
| Actor secundario | Administrador                                             |

## Descripción

Calcula automáticamente ingresos, seguro estudiantil y becas por período.

---

## Flujo Principal — Cálculo automático

1. El sistema detecta arancel y estudiantes.
2. Calcula ingresos.
3. Calcula seguro.
4. Calcula becas.
5. Muestra resultados.

### Flujos alternos

**1a. Arancel no configurado**

- Muestra mensaje.

**1b. Estudiantes no disponibles**

- Muestra mensaje.

---

## Flujo alterno — Configurar parámetros

1. Administrador modifica valores.
2. Sistema recalcula.
3. Sistema guarda auditoría.

---

## Precondiciones

- Arancel configurado.
- Estudiantes proyectados.

## Postcondiciones

- Ingresos calculados.
- Seguro calculado.
- Becas calculadas.

## Reglas de negocio

- **RN-137:** Ingreso = Est × (Arancel + Matrícula).
- **RN-138:** Seguro proporcional.
- **RN-139:** Becas porcentaje.
- **RN-140:** Recalculo automático.
```
