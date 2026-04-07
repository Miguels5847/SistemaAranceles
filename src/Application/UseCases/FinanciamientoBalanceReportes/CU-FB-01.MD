```md
# Caso de Uso: Configurar financiamiento y generar amortización de préstamo

## Identificación

| Campo            | Valor                                                        |
| ---------------- | ------------------------------------------------------------ |
| Identificador    | CU-FB-01                                                     |
| Nombre           | Configurar financiamiento y generar amortización de préstamo |
| RF asociado      | RF-FB-01                                                     |
| Actor primario   | Administrador                                                |
| Actor secundario | Sistema (cálculo de amortización francesa)                   |

## Descripción

Permite configurar cómo se financia la inversión inicial (porcentaje de recursos propios vs crédito bancario) y generar automáticamente la tabla de amortización mensual con sistema francés (cuota fija) si hay préstamo.

---

## Flujo Principal — Configurar financiamiento

1. El Administrador accede a la configuración de financiamiento.
2. El sistema muestra la inversión inicial total y los campos de porcentaje.
3. El Administrador ingresa los porcentajes y confirma.
4. El sistema valida que propios + financiado = 100%.
5. El sistema calcula los montos.
6. Si crédito > 0 el sistema solicita tasa y plazo.
7. El Administrador ingresa datos del préstamo.
8. El sistema genera la tabla de amortización francesa.
9. El sistema calcula totales semestrales.
10. El sistema registra auditoría.
11. El sistema muestra la tabla.

### Flujos alternos

**4a. Porcentajes no suman 100%**

- El sistema muestra mensaje.
- Retorna al paso 3.

**5a. Campos incompletos**

- No se genera tabla.
- Valores = 0.

---

## Precondiciones

- Inversión inicial calculada.

## Postcondiciones

- Financiamiento configurado.
- Tabla de amortización generada.
- Intereses disponibles para Balance.
- Capital disponible para Balance.

## Reglas de negocio

- **RN-178:** Propios + Financiado = 100%.
- **RN-179:** Sistema francés (cuota fija).
- **RN-180:** Intereses semestrales → Balance.
```
