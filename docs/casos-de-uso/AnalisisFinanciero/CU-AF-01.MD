# Caso de Uso: Generar estado de Pérdidas y Ganancias

**Identificador:** CU-AF-01  
**Nombre:** Generar estado de Pérdidas y Ganancias  
**RF asociado:** RF-AF-01

**Actor primario:** Sistema (generación automática)  
**Actor secundario:** Planificador Académico / Consultor (visualización)

## Descripción

El sistema genera automáticamente la proyección de Pérdidas y Ganancias por período, calculando la utilidad bruta, utilidad antes de impuestos, y utilidad o pérdida del ejercicio, con filas configurables de participación a trabajadores e impuesto a la renta ($0 para universidad sin fines de lucro).

---

# 1. Flujo Principal — Generación automática

1. El sistema toma ingresos (CU-DI-02) y costos por categoría (CU-CG-01).
2. El sistema calcula:
   - Utilidad Bruta = Ingresos − Costos por Servicios
3. El sistema calcula:
   - Utilidad antes de Impuestos = Utilidad Bruta − Gastos Administración − Gastos Ventas − Otros Gastos − Gastos Financieros
4. El sistema aplica Participación a trabajadores (15%) e Impuesto a la renta (25%) configurables.
5. El sistema calcula:
   - Utilidad o Pérdida del Ejercicio = Utilidad antes de impuestos − Participación − Impuesto
6. El sistema muestra la tabla de P&G por período con todas las líneas de detalle.

## Flujos alternos

### 1a. Ingresos o costos no disponibles

- El sistema muestra mensaje indicando qué módulo falta completar.

---

# 2. Precondiciones

- Ingresos calculados (CU-DI-02)
- Costos consolidados (CU-CG-01)

---

# 3. Postcondiciones

- Tabla de P&G generada
- Utilidad disponible para Flujo de Fondos y Balance

---

# 4. Reglas de negocio

**RN-159:** Participación (15%) e Impuesto (25%) = $0 por defecto  
**RN-160:** Recálculo automático al cambiar ingresos o costos
