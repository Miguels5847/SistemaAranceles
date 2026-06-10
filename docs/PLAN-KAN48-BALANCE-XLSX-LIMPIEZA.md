# Plan KAN-48 — Limpieza de repo, Balance Proyectado real, cableado Financiamiento y XLSX

> Guía: hojas del Excel `docs/Matriz Fin...Modificada.xlsx` (Balance, 12 P&G, 16 Flujo, Financiamiento, Amort. prestamo). OJO: la hoja Balance tiene `#REF!` incluso en el ORIGINAL → el balance se RECALCULA desde las queries, no se copian valores.
> Build con app abierta: `-p:OutDir=$env:TEMP\SAKan44Build\`. Tests: 123+2.

## Hallazgos del análisis (previo a fases)

### A. Repositorio (ruido)
1. **`.gitignore` roto/agresivo**: ignora `*.md`, `*.txt`, `*.xlsx` GLOBALMENTE → los planes (`docs/PLAN-KAN47…`, este), informes y los 2 Excel guía NO están versionados. README/Diagramas.md están tracked solo porque se agregaron antes de la regla.
2. Raíz con residuos no versionados: `run-output.txt`, `contexto.md`, `Script de todos los modulos desarollados.txt`, `Diagramas Analisis.md`, `PLAN_OPTIMIZACION_ANALISIS_FINANCIERO.md`, carpetas `bin/` y `obj/` en raíz (residuo de builds mal ubicados).
3. Tracked en raíz pero históricos: `ANALISIS_COMPLETO_RENDIMIENTO.md`, `Diagramas.md` (98 KB) → docs/historial/.
4. **36 archivos `.puml` DENTRO de `src/Application/UseCases/`** (BD ER, Diagramas de Clase, Diagramas de Secuencia) → moverlos a `docs/diagramas/` (no compilan, pero no es su lugar).
5. `sql/` con ~80 scripts ya aplicados → `sql/historial/` (se conservan, solo se ordenan).
6. Excel: hojas `Hoja1`, `Hoja2`, `1 Estudiantes v2`, `2 Tasa de Retención v2` son borradores del autor (ignorar como guía).

### B. Bugs / deuda detectada
1. **`Interes = 0` hardcodeado** en `ObtenerMatrizCostosGastosQuery` (línea ~190: `gastoFinanciero = 0m * factor`): el préstamo KAN-44B NO alimenta Costos y Gastos → tampoco EPG ni flujo ni balance. La "Amortización" que sí entra es la de ACTIVOS DIFERIDOS (`ObtenerTablaAmortizacionQuery`), no la del préstamo. **Este es el gap del punto 3 del pedido.**
2. `CapitalTrabajoViewModel` catch de CargarEscenariosAsync asigna `Escenarios=[]/null` SIN supresión → dispara el handler y duplica `RefrescarCapitalTrabajoAsync` (menor).
3. `ServicioInactividad.ResetarActividad()` sin callers (muerto desde timer fijo) — eliminar.
4. Permisos legacy activos en BD sin uso en C#: `AF.EJEC`, `PR.VER`, `PR.EJEC`, `ES.EJECUTAR` → SQL corto para inactivarlos (lo ejecuta el usuario).
5. Paralelismo del reporte por dirección: probado en build/tests, falta validación runtime contra Supabase (posibles límites de concurrencia) — vigilar en la primera prueba.

### C. Documentación (CU/ERD) — qué se toca
- **ERD: SIN cambios.** El balance es 100 % derivado (no persiste); XLSX tampoco persiste.
- **CU a actualizar/crear**: “Generar Balance Proyectado” (nuevo), “Exportar reporte XLSX” (nuevo), ajustar “Consolidación de costos y gastos” (ahora incluye gasto financiero del préstamo).
- Diagramas que YA existen y solo se revisan: `DC-04.3 — Financiamiento y Balance.puml`, `Diagrama-Secuencia-11-Generación de balance y reporte CES.puml`.

---

## Fase 0 — Limpieza (sin tocar funcionalidad; requiere commit al final)
1. `.gitignore`: quitar `*.md`, `*.txt`, `*.xlsx` globales; reemplazar por raíz puntual (`/run-output.txt`, `/contexto.md`, etc. o borrarlos).
2. Borrar de raíz: `run-output.txt`, `contexto.md`, `Script de todos los modulos desarollados.txt`, `bin/`, `obj/` (raíz). Mover a `docs/historial/`: `ANALISIS_COMPLETO_RENDIMIENTO.md`, `Diagramas.md`, `Diagramas Analisis.md`, `PLAN_OPTIMIZACION_ANALISIS_FINANCIERO.md`.
3. `git mv` de los 36 `.puml` → `docs/diagramas/{bd-er, clases, secuencia}/`.
4. `sql/` → `sql/historial/` los aplicados (quedan en raíz de sql/ solo los de KAN-46 en adelante).
5. Versionar: planes de docs/, los 2 .xlsx guía.
6. Código muerto: eliminar `ResetarActividad()`; `sql/KAN48_inactivar_permisos_legacy.sql` (AF.EJEC, PR.*, ES.EJECUTAR → esta_activo=false; usuario ejecuta).
7. Fix menor CapitalTrabajo (supresión en catch).
- **Verificación**: build 0 warnings + tests verdes + `git status` limpio.

## Fase 1 — Financiamiento alimenta Costos/EPG/Flujo (punto 3)
> ⚠️ Impacto: VAN/TIR/arancel óptimo CAMBIARÁN al dejar de ser interés=0. Es el objetivo, pero los valores actuales validados se moverán.
1. Application: `CalculadoraAmortizacionPrestamo` (tabla francesa por período semestral, partiendo de `ResumenFinanciamientoDto` — hoy la tabla solo vive en el VM de Amortización → moverla/reusarla en Application).
2. `ObtenerMatrizCostosGastosQuery`: `Interes` del período = interés semestral del préstamo (hoja "Amort. prestamo"); parámetro precalculado opcional para no recomputar.
3. `ObtenerFlujoFondosQuery`: revisar contra hoja "16 Flujo de Fondos" — ingreso del préstamo y pago de cuotas (capital+interés) según Excel.
4. EPG hereda automáticamente (GastoFinanciero=Interes vía costos). 
5. Tests: interés del período 1 = f(monto, tasa, plazo); EPG con gasto financiero > 0; flujo cuadra con cuotas.

## Fase 2 — Balance Proyectado real (puntos 1 y 2)
1. `BalanceProyectadoDto` + `ObtenerBalanceProyectadoQuery` replicando la hoja "Balance":
   - **Activos**: Corriente (Caja/Bancos ← recursos propios y saldo de caja del flujo), Realizable (suministros/materiales ← Capital de Trabajo/Materiales), Fijo tangible (activos por categoría − depreciación acumulada ← TotalesActivos + MatrizDepreciacion).
   - **Pasivo**: Corriente (15 % participación + 25 % IR del período ← EPG), Largo plazo (saldo del préstamo + intereses por pagar ← tabla de Fase 1).
   - **Patrimonio**: Capital (recursos propios) + utilidad acumulada (EPG).
   - Cuadre: `TotalActivo == TotalPasivo + TotalPatrimonio` (test).
   - Precalculados: EPG, costos, financiamiento, depreciación, totales activos, capital trabajo.
2. Reportes: la sección `BalanceProyectado` pasa a usar el balance REAL; el EPG queda como sección propia "Estado de Pérdidas y Ganancias" (se agrega a Financiera y Completo — hoy el "balance" del PDF es en realidad el EPG).
3. Pantalla: nueva pestaña "Balance" en Análisis Financiero (misma estructura de la hoja) — *confirmar si se quiere en pantalla o solo PDF/XLSX*.
4. Docs: CU nuevo + revisar DC-04.3 y Secuencia-11.

## Fase 3 — Exportación XLSX (punto 4)
1. **ClosedXML 0.105.0 ya está instalado** — sin dependencias nuevas.
2. `IServicioExportacionXlsx` (Application, byte[]) + `ServicioExportacionXlsxClosedXml` (Infrastructure/Export) **reutilizando `ReporteDireccionDatos`** (mismos datos del PDF): una hoja por sección, espejo del Excel original (5 Demanda, 7 Sueldos, 10 Costos y Gastos, 16 Flujo, Balance, INF CES…).
3. UI: botón "Exportar XLSX" junto al de PDF (mismo selector de dirección, mismo permiso REP.EXPORTAR). Archivo `Reporte_{Slug}_{Carrera}_{Escenario}_{fecha}.xlsx`.

## Fase 4 — Verificación integral
1. Comparar PDF/XLSX vs Excel Modificada (orden y conceptos; valores recalculados, no los #REF!).
2. Runtime: probar paralelismo del reporte (Completo) contra Supabase.
3. Build 0 warnings + tests todos verdes.

## Decisiones pendientes del usuario
1. **Fase 1 mueve VAN/TIR/óptimo** (interés deja de ser 0): ¿confirmas?
2. Archivos históricos de raíz: ¿borrar definitivo o mover a docs/historial/? (plan asume mover los tracked, borrar los temporales).
3. Balance en pantalla (pestaña en Análisis Financiero) ¿o solo en PDF/XLSX?
4. Fase 0 requiere commit (git mv/rm): ¿autorizas commit al terminar esa fase?
