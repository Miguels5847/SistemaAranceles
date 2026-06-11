# Plan KAN-49 — Menú guiado, estilos unificados, categorías legibles, rendimiento y distribución

> Rama: `feature/KAN-47-Informe-CES-INF-CES`. Sin commit hasta que el usuario lo pida.
> Regla de oro de F4: **ninguna optimización puede cambiar un solo número** (validación vs Excel pendiente).

## F1 — Menú por flujo de trabajo + guía sutil

Problema: el menú lista módulos en orden histórico de desarrollo, no en el orden en que un
usuario no técnico debe alimentar el sistema (las pantallas de análisis dependen de los datos
de las anteriores).

- `ItemMenu`: + `EsEncabezado` (no clickeable) y `Descripcion` (tooltip explicativo).
- `ConstruirMenu()` reordenado con encabezados de grupo numerados:
  - **Administración**: Usuarios, Auditoría
  - **1 · Configuración base**: Carreras, Inflación, Tasa de Retención y Graduación, Datos Institucionales
  - **2 · Proyección académica**: Proyección de Estudiantes, Demanda e Ingresos
  - **3 · Costos y recursos**: Sueldos Carrera, Aporte Planta Central, Recursos y Depreciación,
    Mantenimiento e Inversión, Capital de Trabajo, Costos y Gastos
  - **4 · Financiamiento y análisis**: Amortización, Análisis Financiero
  - **5 · Resultados**: Reportes
  - Cerrar Sesión (suelto al final)
- Encabezados solo se muestran si el grupo tiene al menos un módulo visible por permisos.
- Tooltip por módulo = qué se hace ahí y qué habilita después (pauta sutil, sin asistentes invasivos).
- `MainWindow.xaml`: DataTemplate distingue encabezado (label pequeño gris) de botón.
- Los `Titulo` NO cambian (SeleccionarMenu compara por título).

## F2 — Estilos unificados

- **Gridlines suaves**: el default WPF de DataGrid es negro. En el estilo global de App.xaml:
  `HorizontalGridLinesBrush`/`VerticalGridLinesBrush` = `#E3E6F0` y `BorderBrush` = `#D5D9E8`.
  (Las vistas que ya definen su brush local lo conservan.)
- **Títulos de módulo un solo color**: todos los títulos de página a `#1A237E` (azul). Cambian los
  verdes: Carreras, Sueldos Carrera, Proyección de Estudiantes, Inflación, Costos y Gastos,
  Análisis Financiero. Los mensajes de éxito (verde) NO cambian: son semánticos.
- **Datos Institucionales**: las 7 cards quedan idénticas: BorderBrush `#C5CAE9`, grosor 1,
  CornerRadius 4, sin fondo especial, header `#1A237E` SemiBold (la card verde de Arancel Óptimo
  y los bordes `#7986CB` se unifican).

## F3 — Categorías de materiales legibles + columna redundante

- Helper único `Application/Comun/CategoriaMaterialDisplay.Formatear(string)`:
  `MATERIALES_SUMINISTROS → "Materiales y Suministros"`, `ASEO_LIMPIEZA → "Suministros de Aseo y Limpieza"`,
  `ACCESORIOS_MATERIALES → "Accesorios y Materiales"`, `OTRO → "Otros"`; fallback genérico
  `_`→espacio + Capitalización (cubre categorías futuras).
- Pantalla Demanda e Ingresos (matrices materiales): se elimina la columna "Categoría" (redundante:
  ya hay fila-encabezado por grupo); la fila-encabezado muestra el nombre formateado en la columna
  Concepto. Aplica a cantidades y a valores monetarios.
- PDF (`SeccionesPdf.MaterialesUnidades/Monetario`): en vez de `CATEGORIA — Concepto` por fila,
  fila de sección (banda) por categoría formateada + conceptos limpios debajo.
- XLSX (`ComponerMateriales`): misma agrupación (fila bold por categoría).
- `DemandaIngresosViewModel.FormatearCategoria` delega en el helper (desaparecen las MAYÚSCULAS).

## F4 — Rendimiento (sin cambiar resultados)

Diagnóstico: la app habla con Postgres (Supabase) por Npgsql; el costo dominante es la LATENCIA
de red por query secuencial, no el cálculo. El patrón seguro ya está validado en Reportes
(KAN-47): un scope de DI por consulta ⇒ DbContext propio ⇒ queries en paralelo.

- `AnalisisFinancieroViewModel.RefrescarAsync`: helper `EnScopeAsync` (scope por consulta) y
  paralelización SOLO de consultas independientes; los precalculados encadenan igual que hoy:
  - Paralelo A: [proyección→demanda], inversiones, capitalTrabajo, arancelVigente.
  - Secuencial (dependencias reales): matriz(demanda), ingresos(arancel), estado(matriz+ingresos), flujo(estado).
  - Paralelo B tras flujo: balance, indicadores, períodoRecuperación, puntoEquilibrio,
    arancelÓptimo, arancelReferencial.
  - Final: dashboard y CES (consumen los anteriores como precalculados).
  - Mismos inputs → mismos números; solo cambia el orden temporal de las lecturas.
- XLSX: `ColumnsUsed().AdjustToContents()` en vez de `Columns()` (medición solo de lo usado) y
  en la hoja "Amort. préstamo" ajustar con las primeras 40 filas (anchos estables).
- Exportación: la composición ya corre fuera del hilo de UI (fix previo).
- NO se toca: fórmulas, orden de cálculo de precalculados, tracking de EF en escrituras, caches.

## F5 — Distribución (ejecutar en otra PC)

- `scripts/publicar.ps1`: `dotnet publish` Release win-x64 self-contained single-file
  (+ReadyToRun). Salida en `publish/` con `SistemaAranceles.Presentation.exe` + appsettings.
  No requiere instalar .NET en la otra máquina.
- `docs/DISTRIBUCION.md`: pasos para copiar a otra PC, dónde va la cadena de conexión
  (appsettings.Local.json junto al exe o variable `SUPABASE_DB_CONNECTION`), requisitos
  (Windows 10/11 x64 + salida a internet hacia Supabase), y dónde quedan los logs.
- `ConstruirConfiguracion` ya usa `AppDomain.CurrentDomain.BaseDirectory` → compatible single-file.

## Verificación

1. Build 0 warnings; tests 131 + 2 verdes.
2. Menú: grupos visibles según permisos; navegación intacta (títulos sin cambios).
3. AF: mismos VAN/TIR/valores que antes de F4 (comparar una carrera antes/después).
4. Reportes PDF/XLSX: categorías "Materiales y Suministros — …" ya no aparecen en mayúsculas crudas.
5. `publicar.ps1` genera exe ejecutable en máquina limpia.
