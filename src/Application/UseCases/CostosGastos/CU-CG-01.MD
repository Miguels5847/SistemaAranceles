# Caso de Uso: Consolidar costos y gastos por período con imprevistos y ponderaciones

**Identificador:** CU-CG-01  
**Nombre:** Consolidar costos y gastos por período con imprevistos y ponderaciones  
**RF asociado:** RF-CG-01

**Actor primario:** Sistema (consolidación automática)  
**Actor secundario:** Administrador (configuración del factor de imprevistos)

## Descripción

El sistema consolida automáticamente todos los costos y gastos de la carrera desde los módulos anteriores, los organiza en 5 categorías, aplica el factor de imprevistos configurable y calcula la ponderación porcentual de cada rubro respecto al total.

---

# 1. Flujo Principal — Consolidación automática

1. El sistema detecta datos disponibles de los módulos fuente: Sueldos y Planta Central, Demanda e Ingresos, Mantenimiento, Recursos Físicos y Depreciación, Financiamiento.
2. El sistema organiza los rubros en 5 categorías:
   - Costos por Servicios
   - Gastos de Administración
   - Gastos de Ventas
   - Otros Gastos
   - Gasto Financiero
3. El sistema calcula el total de cada categoría por período.
4. El sistema aplica el factor de imprevistos:
   - Total = Suma_5_categorías × Factor_imprevistos
5. El sistema calcula la ponderación de cada rubro:
   - Ponderación = Valor_rubro / Total × 100
6. El sistema muestra la tabla de proyección: Concepto × Período/Año con totales y ponderaciones.

---

# 2. Flujos alternos — Configurar factor de imprevistos

1. El Administrador accede a la configuración del factor de imprevistos.
2. El sistema muestra el valor actual (por defecto 1.05 = 5%).
3. El Administrador modifica el valor y confirma.
4. El sistema valida que el factor sea ≥ 1.0.
5. El sistema recalcula toda la tabla de costos y gastos.
6. El sistema registra la acción en el log de auditoría (CU-US-04).

## Flujos alternos de configuración

### 4a. Factor menor a 1.0

- El sistema muestra mensaje:
  "El factor de imprevistos no puede ser menor a 1.0 (100%)"
- Retorna al paso 2.

### 1a. Datos parcialmente disponibles

- El sistema consolida los rubros disponibles
- Marca con "Pendiente" los faltantes

---

# 3. Precondiciones

- Al menos los módulos de Sueldos y Demanda deben tener datos registrados.

---

# 4. Postcondiciones

- Tabla de costos y gastos consolidada con 5 categorías, imprevistos y ponderaciones.
- Resultados disponibles para Análisis Financiero, Capital de Trabajo y Costo de la Carrera.

---

# 5. Reglas de negocio

**RN-151:** Total = Suma de 5 categorías × Factor_imprevistos (por defecto 1.05)  
**RN-152:** El factor de imprevistos debe ser ≥ 1.0  
**RN-153:** Ponderación_rubro = Valor_rubro / Total × 100  
**RN-154:** Recálculo automático al cambiar cualquier módulo fuente
