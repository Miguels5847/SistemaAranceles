# Plan KAN-47 — Reportes por Dirección + Reporte Completo + Gráficos

> Rama: `feature/KAN-47-Informe-CES-INF-CES`. Build con app abierta: `-p:OutDir=$env:TEMP\SAKan44Build\`.
> NO romper los 3 reportes existentes; reutilizar DTOs/queries; sin permisos nuevos (REP.EXPORTAR ya cubre); foco PDF (QuestPDF).

## Estado actual (verificado)
- Módulo Reportes existe: `ReportesViewModel` + `ReportesView` (3 tabs CES/Financiero/Demanda) + `IServicioExportacionPdf` (3 métodos byte[]) + 3 documentos QuestPDF + `EstilosPdf`.
- El PDF de Demanda YA está reforzado: título "Reporte de Demanda y Matrícula", arancel/matrícula vigentes, demanda por ciclo, docentes requeridos, ingresos por ciclo. Solo faltan los 2 gráficos de barras.
- `DemandaProyectadaDto` ya trae `TotalesPorPeriodo` (gráfico matrícula) y `DocentesPorPeriodo` (tipos: TC PhD/Mgs, MT, TP, ocasional) — cero queries nuevas para gráficos.
- No existe mecanismo de gráficos → solución mínima con primitivas QuestPDF (sin dependencias nuevas).
- "Balance proyectado" = `ObtenerEstadoPerdidasGananciasQuery` (EPG); no existe otra query de balance.

## Arquitectura
Un solo flujo: selector "Dirección / Destinatario" en ReportesView → `ExportarPdfDireccionCommand` → VM junta DTOs existentes (cada sección con try/catch → null) → `ReporteDireccionDatos` → `GenerarReporteDireccion` → `ReporteDireccionDocument` compone solo las secciones de la dirección. Secciones sin datos imprimen: "No existen datos suficientes para generar esta sección." (no rompe el PDF).

## Archivos
| Acción | Archivo | Responsabilidad |
|---|---|---|
| Crear | `src/Application/DTOs/Reportes/DireccionReporte.cs` | enum `DireccionReporte` (7), enum `SeccionReporte` (22), `SeccionesReporte.ParaDireccion/Titulo/SlugArchivo` |
| Modificar | `src/Application/DTOs/Reportes/ReporteDatos.cs` | + record `ReporteDireccionDatos` (todo nullable) |
| Modificar | `src/Application/Interfaces/Servicios/IServicioExportacionPdf.cs` | + `byte[] GenerarReporteDireccion(ReporteDireccionDatos)` |
| Crear | `src/Infrastructure/Export/GraficosPdf.cs` | barras simples/agrupadas con primitivas QuestPDF (alto fijo, leyenda) |
| Crear | `src/Infrastructure/Export/SeccionesPdf.cs` | renderers compartidos: movidos de los 3 docs (arancel, demanda, docentes, ingresos, inversión+capital, financiamiento, indicadores, matriz genérica, CES) + nuevos (materiales u/$, activos, depreciación, sueldos, mantenimiento, planta central, invVin, balance EPG) |
| Crear | `src/Infrastructure/Export/Documentos/ReporteDireccionDocument.cs` | compone por lista de secciones |
| Modificar | `ReporteCesDocument/ReporteFinancieroDocument/ReporteDemandaDocument` | delegan a SeccionesPdf (output idéntico); Demanda + 2 gráficos |
| Modificar | `ServicioExportacionPdfQuestPdf.cs` | + método |
| Modificar | `src/Presentation/ViewModels/Reportes/ReportesViewModel.cs` | `Direcciones`/`DireccionSeleccionada` + `ExportarPdfDireccionAsync` |
| Modificar | `src/Presentation/Views/Reportes/ReportesView.xaml` | ComboBox dirección + botón "Exportar PDF por dirección" |
| Crear | `tests/.../Reportes/SeccionesReporteTests.cs` | mapeo dirección→secciones |

## Mapeo Dirección → Secciones (orden de impresión)
- **Financiera**: InversionInicial, CapitalTrabajo, FinanciamientoAmortizacion, Indicadores (TMR/VAN/TIR/viabilidad), CostosGastos, FlujoFondos, BalanceProyectado.
- **GestionDocente**: DemandaTabla, GraficoMatricula, DocentesTabla, GraficoDocentes.
- **Administrativa**: MaterialesUnidades, MaterialesMonetario, ActivosFijos, InversionInicial, Depreciacion, Mantenimiento.
- **TalentoHumano**: DocentesTabla, GraficoDocentes, Sueldos, PlantaCentral.
- **EstrategiaComercial**: ArancelMatricula, DemandaTabla, GraficoMatricula, Ingresos (bruto/becas/neto).
- **GeneralCes**: Ces (INF CES + parámetros + distribución + arancel/matrícula propuesta), InvVinBecas.
- **Completo** (orden del informe original): ArancelMatricula, DemandaTabla, GraficoMatricula, DocentesTabla, GraficoDocentes, Ingresos, MaterialesUnidades, MaterialesMonetario, ActivosFijos, InversionInicial, CapitalTrabajo, Depreciacion, Sueldos, Mantenimiento, PlantaCentral, InvVinBecas, CostosGastos, FinanciamientoAmortizacion, Indicadores, FlujoFondos, BalanceProyectado, Ces.
  (La "Proyección de necesidades" sale del mismo `MaterialesProyectadosDto` que "Materiales" — no se duplica la sección.)

## Datos reutilizados (queries existentes, cero recálculo)
| Sección | Query / DTO |
|---|---|
| ArancelMatricula | `ObtenerArancelEfectivoQuery` → `ArancelEfectivoDto` |
| Demanda + Docentes + gráficos | `ObtenerDemandaProyectadaQuery` → `DemandaProyectadaDto` |
| Ingresos | `CalcularIngresosProyectadosQuery` (arancelPrecalculado) |
| Materiales u/$ | `CalcularMaterialesPorPeriodoQuery` → `MaterialesProyectadosDto` (pivot Cantidades/Monetarios) |
| ActivosFijos | `ListarActivosFijosQuery` + `ObtenerTotalesActivosQuery` |
| InversionInicial | `ObtenerInversionInicialTotalQuery` |
| CapitalTrabajo | `ObtenerResumenCapitalTrabajoQuery` |
| Depreciacion | `ObtenerMatrizDepreciacionQuery` (filas resumen + totales por período) |
| Sueldos | `GenerarResumenSueldosQuery(carrera, escenario, 285m default)` |
| Mantenimiento | `ObtenerResumenMantenimientoQuery` |
| PlantaCentral | `CalcularAportePlantaCentralCarreraQuery` |
| InvVinBecas | `ObtenerMatrizInvVinBecasQuery` (Filas → matriz genérica) |
| CostosGastos | `ObtenerMatrizCostosGastosQuery` |
| FinanciamientoAmortizacion | `ObtenerResumenAmortizacionQuery` → `ResumenFinanciamientoDto` |
| Indicadores | `ObtenerIndicadoresFinancierosQuery` (flujoPrecalculado) |
| FlujoFondos | `ObtenerFlujoFondosQuery` (capitalPrecalculado) |
| BalanceProyectado | `ObtenerEstadoPerdidasGananciasQuery` (costos+ingresos precalculados) |
| Ces | `ObtenerCesQuery` → `CesDto` |

## Gráficos (GraficosPdf, primitivas QuestPDF)
- Alto fijo 110pt; por período una columna; barra = spacer `Height(alto-h)` + caja `Height(h).Background(color)`, h = valor/max*alto; etiquetas debajo; leyenda (cuadrito color + nombre) para multi-serie; sin datos → nota.
- Matrícula: 1 serie (TotalesPorPeriodo) color `#1A237E`.
- Docentes: serie por cada fila de `DocentesPorPeriodo` (paleta fija 6 colores).

## VM / UI
- `DireccionReporteOpcion(Valor, Etiqueta)`; lista de 7; default = Completo NO → default `Financiera` (primera).
- Validaciones: sin carrera → "Selecciona una carrera antes de exportar."; sin escenario → "Selecciona el escenario antes de exportar." (varias queries exigen escenarioId int).
- Cada sección se carga con helper `Seguro(...)` (catch → null) ⇒ criterio 13.
- Archivo: `Reporte_{Slug}_{Carrera}_{Escenario}_{yyyyMMdd}.pdf`; slugs: Financiera, Gestion_Docente, Administrativa, Talento_Humano, Estrategia_Comercial, CES, Completo.
- Botón visible solo con `PuedeExportar` (REP.EXPORTAR || admin). Sin permisos nuevos.

## Verificación
1. `dotnet build` 0 warnings; `dotnet test` 114(+nuevos)+2 verdes.
2. Los 3 exportes actuales generan PDFs idénticos en contenido (CES/Financiero) y Demanda con 2 gráficos extra.
3. Exportar cada dirección produce solo sus secciones; Completo trae todo en orden; secciones sin datos → nota, sin excepción.
