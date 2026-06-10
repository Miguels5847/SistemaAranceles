# Caso de Uso: Calcular costo de la carrera y arancel propuesto

**Identificador:** CU-CG-02  
**Nombre:** Calcular costo de la carrera y arancel propuesto  
**RF asociado:** RF-CG-02

**Actor primario:** Sistema (cálculo automático)  
**Actor secundario:** Planificador Académico / Consultor (visualización)

## Descripción

El sistema calcula automáticamente el costo por estudiante por período, el costo total de la carrera a lo largo de su duración, y propone un arancel y matrícula basados en la distribución de costos entre el número de estudiantes proyectados.

---

# 1. Flujo Principal — Cálculo automático

1. El sistema toma los costos y gastos consolidados (CU-CG-01) y la proyección de estudiantes (CU-ES-01).
2. El sistema calcula el costo por estudiante por período:
   - Costo_por_estudiante = Total_Costos_Gastos / Total_estudiantes
3. El sistema calcula el costo total de la carrera:
   - Costo_total = Suma de costos promedio × Nº semestres
4. El sistema calcula el arancel propuesto:
   - Arancel = Costo_total / Nº semestres
5. El sistema calcula la matrícula:
   - Matrícula = Arancel × 10%
6. El sistema muestra:
   - Tabla por período con Total Costos
   - Nº Estudiantes
   - Costo por Estudiante
   - Resumen con Costo total carrera, Arancel, Matrícula y Total por semestre

---

# Flujos alternos

## 1a. Costos y gastos no consolidados

- El sistema muestra mensaje:
  "Se requiere la consolidación de costos y gastos (CU-CG-01)"

---

# 2. Precondiciones

- Costos y gastos consolidados (CU-CG-01)
- Proyección de estudiantes disponible (CU-ES-01)

---

# 3. Postcondiciones

- Costo por estudiante calculado
- Arancel propuesto calculado
- Matrícula calculada
- El arancel puede ser ajustado por Análisis Financiero (CU-AF-04)

---

# 4. Reglas de negocio

**RN-155:** Costo_por_estudiante = Total_Costos_Gastos / Total_estudiantes  
**RN-156:** Arancel_propuesto = Costo_total_carrera / Nº_semestres  
**RN-157:** Matrícula = Arancel × 10%  
**RN-158:** El costo por estudiante tiende a bajar conforme aumentan los estudiantes
