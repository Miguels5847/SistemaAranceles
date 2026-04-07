```md
# Caso de Uso: Generar Balance Proyectado

## Identificación

| Campo | Valor |
|------|------|
| Identificador | CU-FB-02 |
| Nombre | Generar Balance Proyectado |
| RF asociado | RF-FB-02 |
| Actor primario | Sistema |
| Actor secundario | Planificador Académico / Consultor |

## Descripción

El sistema genera automáticamente el balance proyectado por año calculando Activos, Pasivos y Patrimonio.

---

## Flujo Principal — Generación automática

1. El sistema reúne datos de todos los módulos.
2. El sistema calcula Activos.
3. El sistema calcula Pasivos.
4. El sistema calcula Patrimonio.
5. El sistema verifica ecuación contable.
6. El sistema muestra el balance.

### Flujos alternos

**5a. Balance no cuadra**
- El sistema muestra alerta.
- Permite visualización.

---

## Precondiciones

- P&G generado.
- Depreciación acumulada calculada.
- Financiamiento configurado.

## Postcondiciones

- Balance proyectado generado.
- Verificación de ecuación contable.

## Reglas de negocio

- **RN-181:** Activo = Pasivo + Patrimonio.
- **RN-182:** Si no cuadra se alerta.
- **RN-183:** Recálculo automático.
```
