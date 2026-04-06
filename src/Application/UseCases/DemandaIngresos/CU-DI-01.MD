```md id="b8v9sl"
# Caso de Uso: Configurar arancel y matrícula

## Identificación

| Campo            | Valor                          |
| ---------------- | ------------------------------ |
| Identificador    | CU-DI-01                       |
| Nombre           | Configurar arancel y matrícula |
| RF asociado      | RF-DI-01                       |
| Actor primario   | Planificador Académico         |
| Actor secundario | Sistema                        |

## Descripción

Permite configurar el valor del arancel y la matrícula por estudiante, recalculando automáticamente ingresos y becas.

---

## Flujo Principal — Configurar arancel manualmente

1. El usuario accede al módulo.
2. El sistema muestra valores actuales.
3. El usuario ingresa arancel.
4. El sistema calcula matrícula (10%).
5. El usuario puede modificar matrícula.
6. El usuario confirma.
7. El sistema guarda valores.
8. El sistema recalcula ingresos y becas.
9. El sistema registra auditoría.
10. El sistema muestra confirmación.

### Flujos alternos

**3a. Arancel inválido**

- El sistema muestra error.
- Retorna al paso 3.

---

## Flujo alterno — Recibir arancel desde análisis financiero

1. El sistema recibe arancel.
2. Muestra notificación.
3. Usuario acepta o rechaza.
4. Si acepta recalcula.
5. Si rechaza mantiene valor.

---

## Precondiciones

- Usuario autenticado.

## Postcondiciones

- Arancel y matrícula guardados.

## Reglas de negocio

- **RN-134:** Matrícula = 10% del arancel.
- **RN-135:** Recalculo automático.
- **RN-136:** Puede venir desde análisis financiero.
```
