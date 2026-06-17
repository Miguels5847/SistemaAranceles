# Guía operativa de validación — Aplicativo vs Excel (doble carril)

> **Carrera:** Administración de Empresas · **8 ciclos / 8 períodos (2023-ABR … 2026-SEP)**
> **Complementa** al *Plan de Validación del Aplicativo v4.1* (OE-6). No lo reemplaza: aquí se reparte el trabajo entre el **docente (Excel)** y el **estudiante (aplicativo)**, y se alinea cada dato con lo que el programa realmente hace.
> **Documento de trabajo · Junio 2026**

---

## 1. Cómo leer esta guía

Cada parámetro tiene **dos carriles**:

- **Docente -> Excel**: hoja y celda donde se carga/lee el valor.
- **Estudiante -> Aplicativo**: ruta `Menú -> módulo -> pantalla -> campo`.

Marcadores de la columna **Estado**:

- **OK** — el valor por defecto del aplicativo ya es el de validación; solo verificar.
- **CAMBIAR** — el aplicativo trae **otro** valor por defecto; **hay que sobrescribirlo** antes de validar.
- **NOTA** — el aplicativo lo calcula de forma distinta al Excel; se documenta la diferencia (no es error).

**Doble rol de las matrices** (igual que el plan, sección 1): la **matriz original** es la referencia de oro del caso base (arancel ya ajustado, VAN cercano a cero). La **matriz modificada** aporta los escenarios con el selector. El aplicativo replica el **estado ajustado** (bisección que lleva el VAN cercano a cero), por lo que el arancel final se compara contra la **matriz original**.

---

## 2. Reporte de alineación Aplicativo vs Excel (¿el plan está bien?)

Resultado de cruzar el plan v4.1 con el código del aplicativo:

| Parámetro del plan | Estado | Detalle |
|---|---|---|
| Rango de bisección 500–5000 | OK | Por defecto 500 / 5000 en Datos Institucionales. |
| Catálogo de materiales (Tabla 2C) | OK | Mismos 16 ítems, cantidades y precios que el catálogo del aplicativo. |
| Amortización de diferidos 20 % anual | OK | Tasa por defecto del activo diferido = 20 %. |
| Factor de imprevistos 1,05 | OK | Valor por defecto en Costos y Gastos / Análisis Financiero. |
| Plazo del préstamo 2 años | OK | Por defecto 24 meses. |
| **Tasa de interés financiera 9,5 %** | CAMBIAR | El aplicativo trae **8 %**. |
| **Premio al riesgo 9,33 %** | CAMBIAR | El aplicativo trae **5 %**. |
| **Tasa del préstamo 15 %** | CAMBIAR | El aplicativo trae **15,02 %**. |
| **Permiso Municipal y Bomberos $300 conjunto** | CAMBIAR | El aplicativo siembra **2 registros en $0** ("Permiso Municipal" y "Permiso de Bomberos"). Poner **uno en $300 y el otro en $0** (suma = $300, sin duplicar). |
| **Servicios de mantenimiento (Tabla 2)** | CAMBIAR | Los valores por defecto del aplicativo (Agua 300.000, etc.) **no** son los del Excel (Agua 120.000, etc.). Editar cada rubro a los valores del Excel. |
| TMR / "inflación del modelo" 2,2 % (D4) | NOTA | El Excel usa **2,2 % fijo** en `14 VAN!D4`. El aplicativo **deriva** la inflación promedio de la **serie del módulo Inflación** (aprox. **1,34 %** para 2023-2026) y calcula la TMR con ella. La TMR difiere aprox. 0,08 pp -> diferencia **categoría C** (menor al 1 %, dentro del umbral del 5 %). **Decisión adoptada:** dejar que el aplicativo derive la TMR y cargar la **misma serie INEC** (Tabla 2D) en ambos. |

> **Nota sobre el Excel.** El plan tiene una inconsistencia interna: `14 VAN!D4` usa 2,2 % como "inflación del modelo", mientras la serie INEC de la Tabla 2D promedia aprox. 1,34 %. El aplicativo no tiene un campo separado de "inflación del modelo"; usa una sola serie de inflación. Por eso la pequeña diferencia de TMR es **esperada** y se registra como categoría C.

**Fórmula de la TMR en el aplicativo** (idéntica a la del Excel del docente, salvo el origen de la inflación):

```
TMR (%) = (tasa de interés financiera x inflación promedio / 100) + premio al riesgo
```

---

## 3. Parámetros constantes en los tres escenarios

Se cargan/verifican **una sola vez** antes de la sesión. Solo cambian las tres palancas de la sección 6.

| Parámetro | Valor de validación | Excel (hoja · celda) | Aplicativo (ruta) | Estado |
|---|---|---|---|---|
| Carrera | Administración de Empresas | Identificación | Carreras -> seleccionar/crear la carrera | OK |
| Número de ciclos | 8 | 2 Tasa de Retención | Carreras -> *Total ciclos* = 8; y Tasa de Retención y Graduación -> *Total ciclos* = 8 | OK |
| Paralelos Abril / Septiembre | 1 / 2 | 2 Tasa de Retención · C5 / D5 | Tasa de Retención y Graduación -> *Paralelos Período 1* = 1, *Período 2* = 2 | OK |
| Años de proyección / períodos | 2023–2026 / 8 | 1 Estudiantes · B3:I3 | Proyección de Estudiantes (escenario 2023->2026, 8 períodos) | OK |
| Semanas por semestre | 16 | 1 Estudiantes (fórmulas) | Constante del aplicativo (verificar que use 16) | OK |
| Descuentos por ciclo | 0 % (ciclos 1–8) | Demanda e Ingresos | Demanda e Ingresos -> *Descuentos por ciclo* = 0 % | OK |
| Serie de inflación | Tabla 2D | Inflación · B2:C17 | Inflación -> cargar serie idéntica (ver sección 4.3) | OK |
| Tasa de interés bancaria | 9,5 % | 14 VAN · D3 | Datos Institucionales -> *Tasa de interés financiera* = 9,5 | **CAMBIAR (def. 8)** |
| Premio al riesgo | 9,33 % | 14 VAN · D5 | Datos Institucionales -> *Premio al riesgo* = 9,33 | **CAMBIAR (def. 5)** |
| Inflación del modelo | 2,2 % | 14 VAN · D4 | *No se teclea*: el aplicativo deriva la inflación promedio de la serie de Inflación | NOTA |
| Factor de imprevistos | 1,05 | 10 Costos y Gastos · B1 | Costos y Gastos / Análisis Financiero -> *Factor de imprevisto* = 1,05 | OK |
| Servicios de mantenimiento | Tabla siguiente | 8 Mantenimiento · B8:B19 | Mantenimiento e Inversión -> pestaña *Mantenimiento* -> editar cada rubro | **CAMBIAR** |
| Estudiantes de la unidad académica | 285 | 7 Sueldos · L1 | *Calculado* desde Proyección de Estudiantes (no se teclea); verificar 285 en el histórico | NOTA |
| Estudiantes de la universidad | 14.169 | Planta central · B2 | Datos Institucionales -> *N.º estudiantes universidad* = 14169 | OK (verificar) |
| Docentes de la universidad | 891 | Planta central · F2 | Datos Institucionales -> *N.º docentes universidad* = 891 | OK (verificar) |
| Tasa de interés del préstamo | 15 % | Amort. préstamo · D2 | Datos Institucionales -> *Tasa interés anual préstamo* = 15 | **CAMBIAR (def. 15,02)** |
| Plazo del préstamo | 2 años | Amort. préstamo · D4 | Datos Institucionales -> *Plazo préstamo* = 24 meses | OK |
| Presupuesto institucional base | 870.000 | 5 Demanda · N2 | Demanda e Ingresos -> *Presupuesto base* (verificar) | OK (verificar) |
| Permiso Municipal y Bomberos | $300 conjunto | 4 Inversión Inicial · B4 | Mantenimiento e Inversión -> pestaña *Activos Diferidos* (ver sección 5) | **CAMBIAR** |

### Servicios de mantenimiento (valores del Excel a cargar en el aplicativo)

| Rubro | Valor (Excel) | Celda |
|---|---|---|
| Agua | 120.000 | 8 Mantenimiento · B8 |
| Internet | 624.000 | B9 |
| Energía eléctrica | 296.000 | B10 |
| Comunicaciones | 18.000 | B11 |
| Servicio de limpieza | 399.400 | B15 |
| Seguros bienes y vehículos | 120.000 | B16 |
| Servicio de seguridad | 265.000 | B18 |
| Refacciones e infraestructura | 2.175.000 | B19 |

> En el aplicativo: **Mantenimiento e Inversión -> pestaña Mantenimiento**. Si la carrera trae los rubros por defecto con otros montos, **editarlos** a estos valores. Si faltan, usar **"Generar por defecto"** y luego editar.

---

## 4. Tablas de carga única

### 4.1 Carga horaria académica semestral (Tabla 2B)

Excel: hoja **1 Estudiantes**, filas B16:I16 (docencia) y B20:I20 (práctica).
Aplicativo: **Proyección de Estudiantes -> override de horas por período**.

| Período | Docencia asistida | Aplicación práctica | Celdas |
|---|---|---|---|
| 2023-ABR | 288 | 160 | B16 / B20 |
| 2023-SEP | 336 | 176 | C16 / C20 |
| 2024-ABR | 320 | 192 | D16 / D20 |
| 2024-SEP | 336 | 208 | E16 / E20 |
| 2025-ABR | 336 | 240 | F16 / F20 |
| 2025-SEP | 320 | 264 | G16 / G20 |
| 2026-ABR | 304 | 336 | H16 / H20 |
| 2026-SEP | 256 | 400 | I16 / I20 |

### 4.2 Materiales y suministros (Tabla 2C) — coincide con el catálogo del aplicativo

Excel: hoja **6 Capital de trabajo**. Aplicativo: **Capital de Trabajo -> materiales** (o "Generar por defecto").

| Categoría | Artículo | Cant. base | Unidad | P. unitario |
|---|---|---|---|---|
| Papelería | Resma de papel bond 75 g | 6 | Resma | $3,25 |
| Papelería | Cartuchos de impresora (Color) | 0,60 | Unidad | $50,00 |
| Papelería | Cartuchos de impresora (Negro) | 0,60 | Unidad | $40,00 |
| Papelería | Carpetas de cartón | 60 | Unidad | $1,00 |
| Papelería | Porta files | 30 | Unidad | $2,00 |
| Útiles | Esferos, minas, lápiz, borradores, correctores | 30 | Unidad | $0,25 |
| Útiles | Grapas y clips | 0,60 | Caja | $1,00 |
| Aseo | Desinfectante | 0,90 | Galón | $4,00 |
| Aseo | Jabón líquido | 1,08 | Galón | $3,00 |
| Aseo | Papel higiénico (rollo grande) | 23,40 | Rollo | $10,50 |
| Aseo | Escoba | 2 | Unidad | $10,50 |
| Aseo | Paquete de fundas de basura | 6 | Paquete | $0,80 |
| Aseo | Trapeador | 3 | Unidad | $2,50 |
| Aseo | Cloro | 0,90 | Galón | $2,50 |
| Accesorios | Grapadora | 5,25 | Unidad | $15,00 |
| Accesorios | Perforadora | 5,25 | Unidad | $10,00 |

### 4.3 Inflación de la validación (Tabla 2D)

Excel: hoja **Inflación**. Aplicativo: **Inflación** (misma serie). Promedio 2023-2026 aprox. **1,34 %** (alimenta la TMR del aplicativo).

| Año | Inflación | Celda | Condición |
|---|---|---|---|
| 2023 | 1,35 % | B14:C14 | Oficial cerrado |
| 2024 | 0,53 % | B15:C15 | Oficial cerrado |
| 2025 | 1,91 % | B16:C16 | Oficial cerrado |
| 2026 | 1,55 % | B17:C17 | Proyección (no es valor cerrado) |

---

## 5. Permiso Municipal y Bomberos (Tabla 2E)

Valor institucional **conjunto** de **$300,00** (informe de Costos y Gastos de Administración de Empresas). **No** duplicar ni asumir $300 por cada permiso.

**En el aplicativo** — *Mantenimiento e Inversión -> pestaña Activos Diferidos*:
- El aplicativo crea **dos** registros por defecto en $0: **"Permiso Municipal"** y **"Permiso de Bomberos"**.
- Para la validación: poner **uno en $300,00 y el otro en $0,00** (la suma debe ser exactamente $300). Tasa de amortización **20 % anual**.

| Concepto | Valor | Cálculo |
|---|---|---|
| Total activos diferidos | $300,00 | 4 Inversión Inicial · C3 |
| Amortización anual | $60,00 | 300 x 20 % |
| Amortización semestral | $30,00 | 60 / 2 |
| Amortización en 8 períodos | $240,00 | 30 x 8 |
| Saldo al finalizar 8 períodos | $60,00 | 300 - 240 |

---

## 6. Palancas variables por escenario (Tabla 3)

Lo **único** que cambia entre escenarios.

| Palanca | Crítico | Base (Histórico) | Optimista |
|---|---|---|---|
| Estudiantes que ingresan al ciclo 1 | 24 | 30 | 35 |
| Tasa de retención por ciclo | 82,0 % | 89,8 % | 94,7 % |
| Tasa de graduación por ciclo | 88,0 % | 94,74 % | 97,0 % |

- **Excel:** selector **`2 Tasa de Retención!V19`** -> `1` = pesimista, `2` = histórico, `3` = óptimo.
- **Aplicativo:** *Tasa de Retención y Graduación* -> cargar las tres palancas del escenario -> luego *Análisis Financiero* -> **"Usar arancel óptimo"** (ejecuta la bisección que lleva el VAN cercano a cero).

---

## 7. Celdas de salida críticas (Tabla 4) — dónde leerlas

| Variable | Excel (hoja · celda) | Aplicativo (dónde se lee) |
|---|---|---|
| Total estudiantes acumulado | 1 Estudiantes · I12 | Proyección de Estudiantes |
| Retención acumulada | 2 Tasa de Retención · X10 | Tasa de Retención y Graduación (simulación) |
| Titulación acumulada | 2 Tasa de Retención · X3 | Tasa de Retención y Graduación (simulación) |
| Costo por estudiante | 11 Costo de la Carrera · C6 | Costos y Gastos |
| Costo de la carrera | 11 Costo de la Carrera · C7 | Costos y Gastos |
| Arancel por semestre | 11 Costo de la Carrera · C8 | Costos y Gastos / Análisis Financiero |
| Matrícula | 11 Costo de la Carrera · C9 | Costos y Gastos |
| Total por semestre | 11 Costo de la Carrera · C10 | Costos y Gastos |
| Tasa Mínima de Rendimiento (TMR) | 14 VAN · D6 | Análisis Financiero |
| Tasa Interna de Retorno (TIR) | 13 TIR · D6 | Análisis Financiero |
| Valor Actual Neto (VAN) | 14 VAN · C14 | Análisis Financiero |
| Punto de Equilibrio anual | 15 Punto de equilibrio · B26 | Análisis Financiero |
| Total por semestre (CES) | INF CES · B26 | Reportes -> Informe CES |

---

## 8. Métricas (sin cambios respecto al plan v4.1)

- **Error relativo (%)** = `|App - Excel| / |Excel| x 100`. Umbral **5 %** por celda. *(No se altera la fórmula.)*
- **VAN del caso base** (referencia cercana a 0): error **absoluto** menor o igual a **$3,00** (el relativo no aplica porque el denominador tiende a 0).
- **Tiempo de cálculo:** 3 ejecuciones por escenario, promedio +/- desviación estándar.
- **Inconsistencias:** A = error del Excel · B = error del aplicativo · C = diferencia numérica menor. *La diferencia de TMR por el origen de la inflación entra como **C**.*
- **Trazabilidad:** el aplicativo registra usuario/fecha/módulo/valor anterior/nuevo (esperado mayor al 90 %); el Excel cercano a 0 %.
- **Reproducibilidad:** 3 corridas del aplicativo deben dar el mismo valor hasta 2 decimales.

---

## 9. Checklist previo a la sesión

| N.º | Ítem | Estado |
|---|---|---|
| 1 | La matriz original abre con arancel 1.827,78 y VAN cercano a 0 (caso base). | Sí / No |
| 2 | Carrera = Administración de Empresas, 8 ciclos, 8 períodos 2023-2026. | Sí / No |
| 3 | Paralelos Abril = 1, Septiembre = 2. | Sí / No |
| 4 | Carga horaria (Tabla 2B) idéntica en Excel y aplicativo. | Sí / No |
| 5 | Descuentos de ciclos 1–8 = 0 %. | Sí / No |
| 6 | Serie de inflación idéntica (2023-25 oficiales, 2026 proyección). | Sí / No |
| 7 | Catálogo de materiales = Tabla 2C. | Sí / No |
| 8 | **Tasa de interés financiera = 9,5 %** (cambiado del default 8). | Sí / No |
| 9 | **Premio al riesgo = 9,33 %** (cambiado del default 5). | Sí / No |
| 10 | **Tasa del préstamo = 15 %** (cambiado del default 15,02) y plazo 2 años. | Sí / No |
| 11 | **Servicios de mantenimiento** editados a los valores del Excel (sección 3). | Sí / No |
| 12 | **Permiso Municipal y Bomberos = $300 conjunto** (un registro 300, otro 0), amortización 20 %. | Sí / No |
| 13 | Estudiantes universidad = 14.169 y docentes universidad = 891. | Sí / No |
| 14 | Factor de imprevistos = 1,05. | Sí / No |
| 15 | Rango de bisección 500–5.000 y ajuste automático converge a VAN cercano a 0. | Sí / No |
| 16 | El módulo de Usuarios registra auditoría completa. | Sí / No |

---

## 10. Planillas de registro por escenario

Las tres siguen el mismo formato. El **docente** llena la columna **Excel**; el **estudiante**, la columna **Aplicativo** y el **error**.

### 10.1 Escenario 1 — Caso Base (Histórico) · referencia: matriz ORIGINAL

**Palancas:** estudiantes ciclo 1 = **30** · retención = **89,8 %** · graduación = **94,74 %**

**Tiempos (3 ejecuciones)**

| Ejecución | Tiempo Excel (s) | Tiempo App (s) | Observación |
|---|---|---|---|
| 1 | | | |
| 2 | | | |
| 3 | | | |
| Promedio | | | |
| Desv. estándar | | | |

**Valores comparados**

| Variable | Valor Excel | Valor Aplicativo | Error rel. (%) |
|---|---|---|---|
| Total estudiantes acumulado | | | |
| Costo por estudiante | | | |
| Costo de la carrera | | | |
| Arancel por semestre | | | |
| Matrícula | | | |
| Total por semestre | | | |
| TMR | | | |
| TIR | | | |
| VAN *(absoluto menor o igual a $3)* | | | |
| Punto de Equilibrio anual | | | |
| Arancel ajustado por bisección | | | |

**Inconsistencias**

| N.º | Variable | Descripción | Categoría (A/B/C) |
|---|---|---|---|
| 1 | | | |
| 2 | | | |

**Síntesis**

| Indicador | Valor |
|---|---|
| Error relativo promedio (%) | |
| Error relativo máximo (%) | |
| Tiempo promedio Excel / App (s) | |
| Reducción de tiempo (%) | |
| Inconsistencias matriz / aplicativo | |
| Trazabilidad del aplicativo (%) | |
| Reproducibilidad (Sí/No) | |

**Acta** — Operador Excel: Ing. Sandro Ortiz, Mgs. ____________ · Operador Aplicativo: Miguel Álvarez ____________ · Fecha: __________

---

### 10.2 Escenario 2 — Caso Optimista (Óptimo) · referencia: matriz ORIGINAL

**Palancas:** estudiantes ciclo 1 = **35** · retención = **94,7 %** · graduación = **97,0 %**

**Tiempos (3 ejecuciones)**

| Ejecución | Tiempo Excel (s) | Tiempo App (s) | Observación |
|---|---|---|---|
| 1 | | | |
| 2 | | | |
| 3 | | | |
| Promedio | | | |
| Desv. estándar | | | |

**Valores comparados**

| Variable | Valor Excel | Valor Aplicativo | Error rel. (%) |
|---|---|---|---|
| Total estudiantes acumulado | | | |
| Retención acumulada | | | |
| Titulación acumulada | | | |
| Costo por estudiante | | | |
| Costo de la carrera | | | |
| Arancel por semestre | | | |
| Matrícula | | | |
| Total por semestre | | | |
| TMR | | | |
| TIR | | | |
| VAN | | | |
| Punto de Equilibrio anual | | | |
| Arancel ajustado por bisección | | | |

**Inconsistencias**

| N.º | Variable | Descripción | Categoría (A/B/C) |
|---|---|---|---|
| 1 | | | |
| 2 | | | |

**Síntesis**

| Indicador | Valor |
|---|---|
| Error relativo promedio (%) | |
| Error relativo máximo (%) | |
| Tiempo promedio Excel / App (s) | |
| Reducción de tiempo (%) | |
| Inconsistencias matriz / aplicativo | |
| Trazabilidad del aplicativo (%) | |
| Reproducibilidad (Sí/No) | |

**Acta** — Operador Excel: Ing. Sandro Ortiz, Mgs. ____________ · Operador Aplicativo: Miguel Álvarez ____________ · Fecha: __________

---

### 10.3 Escenario 3 — Caso Crítico (Pesimista) · referencia: matriz ORIGINAL

**Palancas:** estudiantes ciclo 1 = **24** · retención = **82,0 %** · graduación = **88,0 %**

**Tiempos (3 ejecuciones)**

| Ejecución | Tiempo Excel (s) | Tiempo App (s) | Observación |
|---|---|---|---|
| 1 | | | |
| 2 | | | |
| 3 | | | |
| Promedio | | | |
| Desv. estándar | | | |

**Valores comparados**

| Variable | Valor Excel | Valor Aplicativo | Error rel. (%) |
|---|---|---|---|
| Total estudiantes acumulado | | | |
| Retención acumulada | | | |
| Titulación acumulada | | | |
| Costo por estudiante | | | |
| Costo de la carrera | | | |
| Arancel por semestre | | | |
| Matrícula | | | |
| Total por semestre | | | |
| TMR | | | |
| TIR | | | |
| VAN | | | |
| Punto de Equilibrio anual | | | |
| Arancel ajustado por bisección | | | |

**Inconsistencias**

| N.º | Variable | Descripción | Categoría (A/B/C) |
|---|---|---|---|
| 1 | | | |
| 2 | | | |

**Síntesis**

| Indicador | Valor |
|---|---|
| Error relativo promedio (%) | |
| Error relativo máximo (%) | |
| Tiempo promedio Excel / App (s) | |
| Reducción de tiempo (%) | |
| Inconsistencias matriz / aplicativo | |
| Trazabilidad del aplicativo (%) | |
| Reproducibilidad (Sí/No) | |

**Acta** — Operador Excel: Ing. Sandro Ortiz, Mgs. ____________ · Operador Aplicativo: Miguel Álvarez ____________ · Fecha: __________

---

## 11. Consolidado de los tres escenarios

| Indicador | Base | Optimista | Crítico |
|---|---|---|---|
| Error relativo promedio (%) | | | |
| Error relativo máximo (%) | | | |
| Tiempo promedio Excel (s) | | | |
| Tiempo promedio Aplicativo (s) | | | |
| Reducción de tiempo (%) | | | |
| Inconsistencias de la matriz | | | |
| Inconsistencias del aplicativo | | | |
| Arancel ajustado por bisección | | | |

### Acta de aprobación

| Por el Tutor Académico | Por el Estudiante |
|---|---|
| | |
| Ing. Sandro Ortiz, Mgs. — Docente Tutor | Miguel Álvarez — Estudiante |
| Fecha: ______________ | Fecha: ______________ |
