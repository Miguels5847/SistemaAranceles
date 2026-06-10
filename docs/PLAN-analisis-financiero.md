# PLAN — Análisis Financiero: alinear con el Excel del tutor

> Documento de handoff autocontenido. Rama sugerida: `feature/KAN-44-analisis-financiero-excel`.
> Ejecutar **por fases**, en orden. Cada fase trae tareas a nivel de archivo + criterio de aceptación.
> Reglas del repo (`.claude.md`): respuestas concisas; **no commitear sin pedido explícito**; reutilizar lo existente antes de crear; no agregar features fuera de lo pedido; sin comentarios salvo el "porqué" no obvio; antes de borrar/renombrar, `grep` los referenciantes.

---

## 1. Contexto / Problema
En **Análisis Financiero** (carrera Sistemas Computacionales):
1. Aun aplicando el "arancel óptimo" (~$4014) el **Período de Recuperación** no se genera ("flujo acumulado vuelve a negativo", "múltiples cambios de signo").
2. Usar el **arancel sugerido (modo Automático Costo Carrera)** **dispara** VAN/TIR cuando deberían dar **VAN≈0 / TIR≈9.54%**.
3. Seleccionar **"Costo de la Carrera"** en Demanda **daña** el Análisis Financiero.
4. Parámetros de bisección quemados; mensajes que parecen error; falta comparar arancel vigente vs propuesto; mensajes no cerrables.

Referencia canónica del tutor: `docs/Matriz Fin. Sistemas Computacionales-MATRIZ y LA TRONCAL ORIGINAL.xlsx`.

## 2. Verdad de referencia (verificada leyendo el Excel ORIGINAL)
- **TMR** = `TasaInterés·Inflación + PremioRiesgo = 0.095·0.022 + 0.0933 = 9.539% ≈ "9.54"`. El "TIR 9.54" del usuario **es la TMR**; a VAN=0, TIR=TMR.
- **VAN** = `NPV(TMR, [período0 .. sem8])` sobre **9 flujos SEMESTRALES**, con período 0 **descontado**. En el ORIGINAL: **VAN = 0.0015 ≈ 0**.
- **TIR** = `IRR([período0 .. sem8])`, período 0 **sin** descontar. En el ORIGINAL: **TIR = 9.539% = TMR**.
- **Becas**: ingreso = bruto − 10% becas (`5 Demanda!B48 = SUM(B40:B47) − B34`); **becas como costo = 0** (`10 Costos y Gastos!B16` vacío). ⇒ **becas se cuenta una sola vez** (descuento al ingreso, NO costo).
- **Sin** recuperación de capital de trabajo (`16 Flujo!B18` vacía).
- "Costo de la Carrera" (`11!C8 = ΣCostoPorEstudiante/8 ≈ $1940`) = recuperación de costo, **no** VAN=0.
- Identidad útil: `VAN_excel = ValorActualNeto_semestral / (1+TMR)`; el cero es el mismo ⇒ **VAN≈0 ⟺ TIR≈TMR**.
- El flujo del ORIGINAL **sí recupera** (acumulado semestral final ≈ **+7.789**).

## 3. Causas raíz (→ síntomas)
1. **Doble conteo de becas (modo Manual)**: [`CostoGastoPeriodoDto.CostosPorServicios`](../src/Application/DTOs/CostosGastos/CostosGastosDtos.cs) suma `BecasInstitucionales` como costo **y** [`CalcularIngresosProyectadosQuery`](../src/Application/UseCases/DemandaIngresos/CalcularIngresosProyectadosQuery.cs) ya resta becas del ingreso ⇒ utilidad = bruto − 2·becas → infla costos/arancel → VAN muy negativo → no recupera. (Síntoma 1.)
2. **Inconsistencia Manual/Auto**: [`ObtenerMatrizInvVinBecasQuery`](../src/Application/UseCases/CostosGastos/ObtenerMatrizInvVinBecasQuery.cs) pone becas-costo=0 **solo en Auto** (hack anti-ciclo DI con `Lazy<>`), pero el ingreso sigue neto ⇒ VAN/TIR saltan al cambiar de modo. (Síntomas 2 y 3.)
3. **VAN/TIR con método distinto al Excel**: [`ObtenerIndicadoresFinancierosQuery`](../src/Application/UseCases/AnalisisFinanciero/ObtenerIndicadoresFinancierosQuery.cs) + [`ConsolidadorFlujosFinancieros`](../src/Application/Services/Financieros/ConsolidadorFlujosFinancieros.cs) usan flujo **ANUAL** consolidado; el Excel usa **SEMESTRAL** con período 0 descontado ⇒ TIR ≠ 9.54%.
4. "Múltiples cambios de signo / TIR no única" es **esperado** (el Excel también da `IRR=#NUM!` cuando no recupera); no es bug en sí, pero ver Fase 3 y Fase 7.

## 4. Decisiones aprobadas + aclaraciones del usuario
- **Becas — una sola vez.** Aclaración: **NO eliminar becas**; siguen calculándose como **descuento del ingreso** en Demanda/Ingresos; lo que queda en **0 es becas como costo/gasto** (Costos y Gastos, P&G, Flujo) para evitar doble conteo. Documentar esta distinción en el código.
- **VAN/TIR — dos modos.** Principal/visible = **Compatible Excel/Tutor** (semestral, período 0 descontado, sin recuperación de capital de trabajo). Complementario = **Técnico/Ortodoxo** (período 0 sin descontar, periodicidad de la tasa = periodicidad de los flujos, con recuperación de capital de trabajo). Default = Compatible Excel.
- **"Costo de la Carrera" → referencial.** Arreglarlo (queda consistente con el fix de becas) pero **degradarlo a "Arancel referencial por costo de carrera"**; el **óptimo financiero** es el de la pestaña Arancel Óptimo (VAN=0). Flujo principal: Manual vigente → Análisis → Arancel Óptimo → botón "Usar arancel óptimo".
- **Parámetros de bisección configurables** en Datos Institucionales (no quemados).
- **Estados/mensajes claros** (no todo como error) y **cerrables con X**.
- **TIR normal (no TIRM).** Aclaración: si hay **múltiples raíces**, mostrar la TIR **cercana a la TMR** y **advertir que la viabilidad principal se evalúa con el VAN**.
- **Período de recuperación.** Aclaración: **no asumir** que siempre se resolverá al aplicar el óptimo; **recalcular y mostrar claramente si recupera o no**, indicando si corresponde al **arancel vigente** o al **arancel óptimo**.
- **Verificación Supabase = solo lectura.** Aclaración: **no aplicar/guardar** arancel óptimo sobre datos reales durante la verificación; esa prueba se hace en **entorno local/de prueba**.

---

## 5. Fases de implementación

### Fase 1 — Becas una sola vez (descuento al ingreso; costo = 0)
**Tareas**
- [`CostosGastosDtos.cs`](../src/Application/DTOs/CostosGastos/CostosGastosDtos.cs): quitar `+ BecasInstitucionales` de `CostosPorServicios` (becas no es costo). Mantener el campo/línea mostrándose en **0** (como el Excel).
- [`ObtenerMatrizInvVinBecasQuery.cs`](../src/Application/UseCases/CostosGastos/ObtenerMatrizInvVinBecasQuery.cs): la serie de Becas Institucionales (costo) = 0 siempre. Esto **rompe el ciclo DI** ⇒ eliminar `Lazy<CalcularIngresosProyectadosQuery>` y el caso especial de modo Auto; ajustar el registro en [`App.xaml.cs`](../src/Presentation/App.xaml.cs).
- [`ObtenerArancelOptimoBiseccionQuery.cs`](../src/Application/UseCases/AnalisisFinanciero/ObtenerArancelOptimoBiseccionQuery.cs) `EvaluarArancel`: quitar `− BecasInstitucionales + becas` de `costosYGastos`; mantener `ingresosNetos = bruto − becas`.
- **Documentar en código** (comentario "porqué"): *Becas = descuento al ingreso (Demanda/Ingresos), NO costo. En Costos/P&G/Flujo el rubro Becas = 0 para no doble-contar.*
**Aceptación**: arancel "Costo Carrera" ≈ $1940 (igual al Excel); modos Manual y Auto dan los mismos ingresos/costos; el rubro Becas aparece en 0 en P&G/Flujo y sigue restándose del ingreso en Ingresos Proyectados.

### Fase 2 — Modo de cálculo financiero (CompatibleExcel por defecto + Técnico)
**Tareas**
- Crear enum `ModoCalculoFinanciero { CompatibleExcel = 0, Tecnico = 1 }` en `src/Domain/Enums`. Default = `CompatibleExcel`.
- Propagar el modo como parámetro por: [`ObtenerFlujoFondosQuery.cs`](../src/Application/UseCases/AnalisisFinanciero/ObtenerFlujoFondosQuery.cs), [`ObtenerIndicadoresFinancierosQuery.cs`](../src/Application/UseCases/AnalisisFinanciero/ObtenerIndicadoresFinancierosQuery.cs), [`ObtenerArancelOptimoBiseccionQuery.cs`](../src/Application/UseCases/AnalisisFinanciero/ObtenerArancelOptimoBiseccionQuery.cs), [`ObtenerPeriodoRecuperacionQuery.cs`](../src/Application/UseCases/AnalisisFinanciero/ObtenerPeriodoRecuperacionQuery.cs).
- **Flujos para VAN/TIR**: en CompatibleExcel usar la serie **semestral** (período 0 + 8 semestres); en Técnico usar `ConsolidadorFlujosFinancieros.ConstruirDetalleAnual` (anual). Agregar helper para la serie semestral en [`ConsolidadorFlujosFinancieros.cs`](../src/Application/Services/Financieros/ConsolidadorFlujosFinancieros.cs).
- **Recuperación de capital de trabajo**: en [`ObtenerFlujoFondosQuery.cs`] sumarla **solo en Técnico**; en CompatibleExcel = 0.
- **VAN**: en [`CalculadoraVAN.cs`](../src/Application/Services/Financieros/CalculadoraVAN.cs) agregar convención Excel (período 0 descontado): `VAN_excel = ValorActualNeto(flujos)/(1+tmr)`. Técnico mantiene el actual (período 0 a t=0).
- **UI**: selector de modo en [`AnalisisFinancieroView.xaml`](../src/Presentation/Views/AnalisisFinanciero/AnalisisFinancieroView.xaml) + [`AnalisisFinancieroViewModel.cs`](../src/Presentation/ViewModels/AnalisisFinanciero/AnalisisFinancieroViewModel.cs), default CompatibleExcel, etiquetas claras.
**Aceptación**: en CompatibleExcel, VAN≈0 y TIR≈9.54% reproducen el ORIGINAL; el selector cambia entre ambos métodos.

### Fase 3 — TIR normal cercana a la TMR (raíces múltiples)
**Tareas**
- [`CalculadoraTIR.cs`](../src/Application/Services/Financieros/CalculadoraTIR.cs): agregar `CalcularCercanaA(flujos, tmrTasa)` que, si hay varias raíces (varios cambios de signo), devuelve la TIR **más cercana a la TMR** (no la primera desde −90%). **TIR normal, no TIRM.** Mantener conteo de cambios de signo y advertencia.
- Usar `CalcularCercanaA(flujos, tmr.TmrTasa)` en [`ObtenerIndicadoresFinancierosQuery.cs`] y [`ObtenerArancelOptimoBiseccionQuery.cs`].
**Aceptación**: con flujos semestrales del ORIGINAL la TIR ≈ 9.54%; si hay múltiples raíces, se elige la cercana a la TMR y se muestra advertencia de que la viabilidad se evalúa con VAN.

### Fase 4 — Parámetros de bisección en Datos Institucionales
**Tareas**
- Agregar a `DatosInstitucionales` (entidad `src/Domain/Entities/DatosInstitucionales.cs`, DTO, vista [`DatosInstitucionalesView.xaml`](../src/Presentation/Views/DatosInstitucionales/DatosInstitucionalesView.xaml) + ViewModel) y **migración SQL/Supabase**: `Tolerancia VAN` ($1.00), `Margen aproximación VAN` ($2.00), `Arancel mínimo` ($500), `Arancel máximo` ($5000), `Máx. iteraciones` (60). Con defaults.
- [`ObtenerArancelOptimoBiseccionQuery.cs`]: reemplazar las constantes `ArancelMinimo/ArancelMaximo/ToleranciaVan/MaxIteraciones` por los valores de `datos` (ya inyecta `IRepositorioDatosInstitucionales`).
**Aceptación**: editar los parámetros en Datos Institucionales cambia el comportamiento de la bisección.

### Fase 5 — Estado de convergencia claro (no "error")
**Tareas**
- [`CalculadoraArancelOptimoBiseccion.cs`](../src/Application/Services/Financieros/CalculadoraArancelOptimoBiseccion.cs): agregar `MargenAproximacionVan` a `EntradaBiseccionArancel`; reemplazar la lógica final por:
  - `|VAN| ≤ tolerancia` → **Calculado** / "Convergió".
  - `|VAN| ≤ margenAproximacionVan` → **Equilibrio aproximado por redondeo monetario**.
  - `|VAN| > margen` → **No convergió dentro del rango definido**.
- Quitar el estado "Máximo de iteraciones" tratado como error.
**Aceptación**: VAN $1.28 con tolerancia $1 / margen $2 → "Equilibrio aproximado", no error.

### Fase 6 — Comparativa Arancel vigente vs propuesto + período recuperación claro
**Tareas**
- En la pestaña Arancel Óptimo ([`AnalisisFinancieroView.xaml`]/[`AnalisisFinancieroViewModel.cs`]) mostrar:
  - **Vigente**: arancel manual actual, matrícula, total (de [`ObtenerArancelEfectivoQuery.cs`](../src/Application/UseCases/DemandaIngresos/ObtenerArancelEfectivoQuery.cs)).
  - **Referencial costo carrera**: arancel (de [`ObtenerArancelOptimoCarreraQuery.cs`](../src/Application/UseCases/CostosGastos/ObtenerArancelOptimoCarreraQuery.cs)).
  - **Óptimo financiero VAN=0**: arancel, matrícula óptima, total (de `ArancelOptimoBiseccionDto`).
  - **Diferencia $ y %** (vigente vs óptimo).
- **Período de recuperación**: recalcular y mostrar **claramente si recupera o no**, indicando si el resultado corresponde al **arancel vigente** o al **arancel óptimo** aplicado. No asumir que el óptimo siempre recupera.
**Aceptación**: se ve vigente vs óptimo con diferencias; el período de recuperación indica con qué arancel se calculó y si recupera.

### Fase 7 — Mensajes/banners cerrables con severidad
**Tareas**
- Control banner reutilizable con severidad **Error / Advertencia / Información / Éxito** y botón **X** para cerrar; reemplaza los `TextBlock` planos de mensajes en [`AnalisisFinancieroView.xaml`] (ViewModel: colección de mensajes cerrables).
- Ejemplos: `[Info] VAN $1.28 dentro del margen configurado [X]`; `[Advertencia] La TIR puede no ser única; se prioriza el VAN [X]`; `[Info] La recuperación mostrada corresponde al arancel vigente [X]`.
**Aceptación**: los avisos no se ven como error grave y se pueden cerrar.

### Fase 8 — Degradar "Costo de la Carrera" a referencial
**Tareas**
- Renombrar etiquetas en [`DemandaIngresosView.xaml`](../src/Presentation/Views/DemandaIngresos/DemandaIngresosView.xaml) y el string `FuenteCalculo` en [`ObtenerArancelEfectivoQuery.cs`] a **"Arancel referencial por costo de carrera"**. Dejar claro en UI que el **óptimo financiero** es el VAN=0 (pestaña Arancel Óptimo).
- Confirmar flujo principal: Manual vigente → Análisis (VAN/TIR/TMR) → Arancel Óptimo → botón "Usar arancel óptimo" ([`AnalisisFinancieroViewModel.UsarArancelOptimoAsync`]).
**Aceptación**: el modo automático no se presenta como "óptimo financiero" ni dispara el análisis.

---

## 6. Verificación
1. **Supabase = solo lectura.** Ubicar IDs de "Sistemas Computacionales – Matriz" + escenario Optimista; confirmar configuración de arancel y existencia de proyección. **No** aplicar/guardar arancel óptimo sobre datos reales; pruebas de "aplicar" en **entorno local/de prueba**.
2. Modo **CompatibleExcel**: flujo semestral, **VAN≈0** y **TIR≈9.54%** coinciden con `ORIGINAL.xlsx` (coinciden las **reglas**, no necesariamente al centavo si los datos difieren). Regresión con "La Troncal".
3. Aplicar arancel óptimo (VAN=0) **en entorno de prueba** → VAN≈0, TIR≈TMR; **recalcular período de recuperación** y mostrar si recupera o no (indicando arancel usado). En el ORIGINAL recupera (acumulado final +7.789).
4. VAN $1.28 con tolerancia $1 / margen $2 → "Equilibrio aproximado", no error.
5. Parámetros editables en Datos Institucionales se reflejan en la bisección.
6. Comparativa vigente vs óptimo visible; mensajes cerrables con X.
7. `dotnet test tests/Application.Tests/SistemaAranceles.Application.Tests.csproj` (casos nuevos: becas-una-vez; VAN Excel = `ValorActualNeto/(1+tmr)`; TIR semestral; TIR cercana a TMR; estados de convergencia por tolerancia/margen). `dotnet build src/Presentation/SistemaAranceles.Presentation.csproj`.

## 7. Notas / riesgos
- Cambio transversal en el módulo financiero + **1 migración SQL** (Datos Institucionales).
- El fix de becas también afecta **Punto de Equilibrio** (usa los 9 rubros con becas=0 → queda coherente con el Excel).
- CompatibleExcel reproduce el Excel aunque éste sea internamente inconsistente (VAN descuenta período 0, TIR no); Técnico queda documentado como sustento ortodoxo.
- **Becas**: distinción a respetar en todo el código — descuento al ingreso (sí) vs costo (0); nunca re-sumar en P&G/Flujo.
