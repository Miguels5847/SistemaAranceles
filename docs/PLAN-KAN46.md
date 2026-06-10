# Plan KAN-46 — Descuentos editables, 3 aranceles, timer mm:ss, permisos granulares, Reportes Fase 1

> Al aprobarse: guardar copia de este plan en `docs/PLAN-KAN46.md` del proyecto y crear los `.sql` en `sql/`.
> Rama: `feature/KAN-46-Balance-Proyectado`. Build con app abierta: `-p:OutDir=$env:TEMP\SAKan44Build\`.

## Context

5 tareas sobre lo ya construido, sin romper lo que funciona:
1. **Descuentos por ciclo**: la columna `DataGridTextColumn` exige doble-click/F2 y el valor pendiente de la celda activa no se comitea al pulsar "Guardar" → se pierde. Afecta Demanda, Ingresos, VAN, TIR, arancel óptimo.
2. **3 aranceles confundibles**: "Usar arancel óptimo" guarda `ModoCalculoArancel="Manual"` → se pierde el origen. Decisión del usuario: **3er modo en BD** (`OptimoFinanciero`) + panel comparativo.
3. **Timer**: muestra "45 min" (ceiling) → no se ve si baja. Mostrar `mm:ss` (el timer ya es System.Timers.Timer confiable, tick 1 s).
4. **Permisos**: "Configuración" (CFG.*) sigue activo en BD y aparece en la UI de permisos de usuario aunque el módulo ya no existe; Amortización reutiliza DI.VER; AF.VER compartido por 3 módulos; MI/DI_NG/CG quizá sin sembrar. Decisión: **granular completo**.
5. **Reportes Fase 1**: módulo real (reemplaza placeholder) con Reporte CES + Financiero + Demanda, pantalla + exportar PDF (QuestPDF 2026.2.4 ya instalado, sin uso). Fase 2 (plan futuro): Informe de Costos completo, Docente, Administrativo.

## Orden de ejecución

| # | Tarea | SQL del usuario requerido antes |
|---|---|---|
| 1 | Timer mm:ss | No |
| 2 | Descuentos edición directa | No |
| 3 | 3er modo arancel | SQL-A (constraint) antes de probar guardado |
| 4 | Permisos granulares | SQL-B (diagnóstico) → reporta → SQL-C (correctivo) → recién entonces C# |
| 5 | Módulo Reportes | SQL-C garantiza REP.VER/REP.EXPORTAR |

---

## Tarea 1 — Timer con segundos

[MainViewModel.cs](src/Presentation/ViewModels/MainViewModel.cs) `FormatearTiempoRestante` (~146-152):
```csharp
private static string FormatearTiempoRestante(TimeSpan tiempo)
{
    var t = tiempo < TimeSpan.Zero ? TimeSpan.Zero : tiempo;
    return $"{(int)t.TotalMinutes:00}:{t.Seconds:00}";
}
```
`(int)t.TotalMinutes` (no `t.Minutes`) para soportar > 59 min. El default `_tiempoRestanteSesion = "45:00"` ya coincide. Sin tocar ServicioInactividad.

## Tarea 2 — Descuentos por ciclo: edición directa

[DemandaIngresosView.xaml](src/Presentation/Views/DemandaIngresos/DemandaIngresosView.xaml) líneas ~267-278. **Solo XAML, cero code-behind, NO hace falta CommitEdit** (con `CellTemplate` no existe transacción de edición; cada tecla empuja a `DescuentoCicloEditableView.Porcentaje`).

1. Columna "% Descuento" → `DataGridTemplateColumn` con TextBox siempre visible:
```xml
<DataGridTemplateColumn Header="% Descuento" Width="180">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBox Text="{Binding Porcentaje, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                     IsEnabled="{Binding DataContext.DescuentosEditables,
                                 RelativeSource={RelativeSource AncestorType=DataGrid}}"
                     BorderThickness="0" Background="Transparent"
                     VerticalContentAlignment="Center" Padding="4,2"/>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```
2. En el DataGrid: quitar `IsReadOnly="{Binding DescuentosEditables, Converter=...InverseBool}"` → `IsReadOnly="True"` (el control de edición pasa al `IsEnabled` del TextBox; evita edit-mode en columna Ciclo).
3. NO usar `CellEditingTemplate` (reintroduciría el bug del commit).

Flujo Guardar/Cancelar/validación 0-100 existente en `DemandaIngresosViewModel` no cambia.

## Tarea 3 — Tercer modo de arancel `OptimoFinanciero`

### SQL-A (usuario ejecuta PRIMERO): crear `sql/KAN46_arancel_modo_optimo.sql`
Hay CHECK constraint (migración KAN32) que rechaza el modo nuevo → sin esto, error 23514:
```sql
BEGIN;
ALTER TABLE public.configuracion_arancel_carrera
    DROP CONSTRAINT IF EXISTS "CK_configuracion_arancel_carrera_modo";
ALTER TABLE public.configuracion_arancel_carrera
    ADD CONSTRAINT "CK_configuracion_arancel_carrera_modo"
    CHECK (modo_calculo_arancel IN ('Manual', 'AutomaticoCostoCarrera', 'OptimoFinanciero'));
COMMIT;
```

### Cambios C# (capas en orden)
1. `src/Domain/Enums/ModoCalculoArancel.cs`: agregar `OptimoFinanciero`.
2. [ConfiguracionArancelCarrera.cs](src/Domain/Entities/ConfiguracionArancelCarrera.cs):
   - `CambiarConfiguracion` (~55): `if (modoCalculo is ModoCalculoArancel.Manual or ModoCalculoArancel.OptimoFinanciero)` → exige ArancelManual > 0.
   - `CalcularArancelEfectivo` (~91): brazo `OptimoFinanciero => ArancelManual`.
3. `GuardarConfiguracionArancelCarreraCommand.cs` (~21-29): helper `esValorFijo = Manual || OptimoFinanciero` exige arancel; `AutomaticoCostoCarrera` pasa; otro → rechaza.
4. [ObtenerArancelEfectivoQuery.cs](src/Application/UseCases/DemandaIngresos/ObtenerArancelEfectivoQuery.cs) (~58-64): `if (modo is Manual or OptimoFinanciero)` y `fuente = modo == OptimoFinanciero ? "Óptimo financiero (VAN≈0)" : "Manual"`. NO tocar los fallbacks `Enum.TryParse(..., ignoreCase) ?? Manual` (ni el del repositorio) — protegen filas viejas.
5. `src/Presentation/Converters/ModoArancelDisplayConverter.cs`: caso `"OptimoFinanciero" => "Óptimo financiero (VAN≈0)"` + ConvertBack.
6. [AnalisisFinancieroViewModel.cs](src/Presentation/ViewModels/AnalisisFinanciero/AnalisisFinancieroViewModel.cs) `UsarArancelOptimoAsync` (~720): `ModoCalculoArancel = "OptimoFinanciero"` (era "Manual").
7. **"Usar arancel sugerido" de DemandaIngresos NO cambia** (sigue guardando "Manual"): ese botón copia el referencial por costo, no el VAN≈0 — etiquetarlo OptimoFinanciero mentiría. Si después se aplica sugerido sobre un óptimo, vuelve a Manual (decisión explícita del usuario, correcto).
8. [DemandaIngresosViewModel.cs](src/Presentation/ViewModels/DemandaIngresos/DemandaIngresosViewModel.cs):
   - Constante `ModoOptimoFinanciero = "OptimoFinanciero"`; incluirla en `ModosCalculo` (~177) (si no, el ComboBox del form queda en blanco al editar una config en ese modo).
   - `FormEsModoManual` (~289) y validación de `GuardarConfiguracionAsync` (~948): tratar OptimoFinanciero como valor fijo (helper `EsModoValorFijo`).
   - **Panel comparativo** (3 props solo lectura, SIN ejecutar bisección):
     - Deseado/Manual: `ArancelManual` si modo=="Manual", si no "—".
     - Referencial por costo: `ArancelSugeridoCostoCarrera?.ArancelSugeridoSemestre` (ya se carga) o "Costo de carrera pendiente".
     - Óptimo financiero: `ArancelManual` si modo=="OptimoFinanciero", si no "No aplicado — calcúlalo en Análisis Financiero".
     - `OnPropertyChanged` de las 3 al terminar las cargas de Configuraciones y ArancelSugerido.
9. [DemandaIngresosView.xaml](src/Presentation/Views/DemandaIngresos/DemandaIngresosView.xaml): panel comparativo (UniformGrid 3 mini-cards, estilo del banner ~135-171) bajo el banner del Tab 1.

**No tocar:** `ObtenerArancelOptimoBiseccionQuery` (no lee el modo), TMR/VAN/TIR, becas.
Tests opcionales recomendados: command acepta OptimoFinanciero con arancel / rechaza sin arancel.

## Tarea 4 — Permisos granulares

### SQL-B diagnóstico (usuario ejecuta y reporta): crear `sql/KAN46_permisos_diagnostico.sql`
Solo SELECTs: (1) inventario `permiso` completo; (2) códigos esperados por el C# vs existentes/inactivos/faltantes (lista: US/CA/INF/TRE/ES/DI/PC/RD/MI/AF/DI_NG/CG/REP/AUD/CFG); (3) asignaciones rol→permiso; (4) overrides de usuario sobre AF.VER, DI.VER, DI.EDITAR, DI_NG.EDITAR, CFG.*; (5) `pg_constraint` de configuracion_arancel_carrera (confirma Tarea 3).

### SQL-C correctivo idempotente (tras revisar diagnóstico): crear `sql/KAN46_permisos_granular_apply.sql`
1. INSERT … ON CONFLICT (codigo) DO UPDATE (reactiva + normaliza modulo_nombre legible) para: `AMO.VER/AMO.EDITAR` (Amortizacion), `SC.VER` (Sueldos Carrera), `CT.VER` (Capital de Trabajo), `AF.EDITAR` (Analisis Financiero), `MI.VER/CREAR/EDITAR/ELIMINAR`, `DI_NG.VER/EDITAR/CALCULAR`, `CG.VER`, `REP.VER/REP.EXPORTAR`. Solo códigos que el C# consume (no repetir el problema CFG).
2. `UPDATE permiso SET esta_activo=FALSE WHERE codigo IN ('CFG.VER','CFG.EDITAR')` → desaparece de la UI de permisos (la query de EditarUsuario filtra esta_activo).
3. Normalizar modulo_nombre de AF.* → 'Analisis Financiero', DI_NG.* → 'Demanda e Ingresos'.
4. Migrar asignaciones de rol copiando del permiso reemplazado: AF.VER→{SC.VER, CT.VER}, DI.VER→AMO.VER, DI.EDITAR→AMO.EDITAR, DI_NG.EDITAR→AF.EDITAR (ON CONFLICT DO NOTHING). Igual con `usuario_permiso_override` (copia `concedido`).
5. Asignar los posiblemente-nunca-sembrados a roles: Administrador/Analista todo; Visualizador solo *.VER.
6. **No se borra nada** → el .exe viejo sigue funcionando durante la transición. Usuarios deben re-login (permisos se cargan en LoginUseCase).

### Cambios C# (DESPUÉS de SQL-C) — mapeo viejo→nuevo
| Archivo | Dónde | Viejo → Nuevo |
|---|---|---|
| [MainViewModel.cs](src/Presentation/ViewModels/MainViewModel.cs) | ~120 página inicial fallback | AF.VER → SC.VER |
| MainViewModel.cs | menú Sueldos Carrera (~189) + `MostrarCargosFacultadAsync` (~358) | AF.VER → SC.VER |
| MainViewModel.cs | menú Capital de Trabajo (~214) + `MostrarCapitalTrabajoAsync` (~369) | AF.VER → CT.VER |
| MainViewModel.cs | menú Análisis Financiero (~229) + Mostrar (~407) | AF.VER (sin cambio) |
| MainViewModel.cs | menú Amortización (~234) + `MostrarAmortizacionAsync` (~417) | DI.VER → AMO.VER |
| [AmortizacionViewModel.cs](src/Presentation/ViewModels/Amortizacion/AmortizacionViewModel.cs) | `PuedeEditar` (~58) | DI.EDITAR → AMO.EDITAR |
| AnalisisFinancieroViewModel.cs | `PuedeEditar` (~142) | DI_NG.EDITAR → AF.EDITAR |
| CargosFacultadViewModel.cs | `PuedeVer` (~103) | AF.VER → SC.VER |

Sin cambios: DI.* (Datos Institucionales, dueño legítimo), DI_NG.* en DemandaIngresos, MI.*, CG.VER, REP/AUD/US/CA/INF/TRE/ES/PC/RD. `EditarUsuarioViewModel.NormalizarModulo` es genérico — no se toca (por eso los modulo_nombre se siembran ya legibles).

## Tarea 5 — Módulo Reportes Fase 1 (CES + Financiero + Demanda, PDF)

### Nuevos archivos
1. `src/Application/DTOs/Reportes/ReporteDatos.cs` — 3 records agregadores (encabezado carrera/escenario/fecha + DTOs existentes; no duplicar datos):
   - `ReporteCesDatos { …, CesDto }`
   - `ReporteFinancieroDatos { …, InversionInicialTotalDto, ResumenCapitalTrabajoDto, MatrizCostosGastosDto, ResumenFinanciamientoDto, FlujoFondosDto, IndicadoresFinancierosDto, DashboardFinancieroDto }`
   - `ReporteDemandaDatos { …, DemandaProyectadaDto, IngresosProyectadosDto, ArancelEfectivoDto }`
2. `src/Application/Interfaces/Servicios/IServicioExportacionPdf.cs` — 3 métodos que devuelven `byte[]` (Infrastructure no conoce diálogos; Presentation escribe el archivo).
3. `src/Infrastructure/Export/ServicioExportacionPdfQuestPdf.cs` — licencia en constructor estático: `static … { QuestPDF.Settings.License = LicenseType.Community; }`. No referenciar QuestPDF desde Presentation.
4. `src/Infrastructure/Export/Documentos/ReporteCesDocument.cs`, `ReporteFinancieroDocument.cs`, `ReporteDemandaDocument.cs` + `EstilosPdf.cs` (helpers celda/moneda). CES: 3 cuadros de CesDto (InfCes/Parametros/Distribucion).
5. `src/Presentation/ViewModels/Reportes/ReportesViewModel.cs` — patrón AnalisisFinancieroViewModel: selector carrera/escenario compartido (reusar `ListarCarrerasConProyeccionQuery` / `ListarEscenariosConProyeccionPorCarreraQuery`), TabControl 3 reportes con carga lazy, DTOs existentes directo a la vista, `PuedeExportar => EsAdministrador || TienePermiso("REP.EXPORTAR")`, `ExportarPdfCommand` → record agregador → servicio PDF → `SaveFileDialog` (`Reporte_CES_{carrera}_{escenario}_{yyyyMMdd}.pdf`) → `File.WriteAllBytes`.
6. `src/Presentation/Views/Reportes/ReportesView.xaml(.cs)` — grids read-only; columnas dinámicas de periodos con la técnica code-behind de DemandaIngresosView.

### Modificaciones
- `src/Infrastructure/DI/InfrastructureExtensions.cs`: `AddTransient<IServicioExportacionPdf, ServicioExportacionPdfQuestPdf>()`.
- `App.xaml.cs`: `AddTransient<ReportesViewModel>()` (las queries base ya están registradas).
- `MainWindow.xaml`: xmlns + DataTemplate ReportesViewModel→ReportesView.
- `MainViewModel.cs` (~239): reemplazar `MostrarModuloEnDesarrollo("Reportes", …)` por `MostrarReportesAsync()` con gate `REP.VER || EsAdministrador` (hoy falta el `|| EsAdministrador`).

## Riesgos / gotchas
1. SQL-A antes de probar "Usar arancel óptimo" (CHECK 23514).
2. No tocar los 2 fallbacks `Enum.TryParse(...) ?? Manual` (query + repositorio).
3. SQL-C antes de desplegar el C# de permisos; nada se borra → sin ventana de ruptura; re-login obligatorio.
4. T1: solo `CellTemplate`, nunca `CellEditingTemplate`.
5. Binding decimal con PropertyChanged: estados intermedios inválidos ("12.") no actualizan la fuente (igual que hoy; validación 0-100 del VM cubre).
6. Si existe rol "Planificador" en el diagnóstico, decidir con el usuario si entra en el paso 5 del SQL-C.

## Verificación
1. Build 0 warnings + `dotnet test` 114+2 verdes.
2. Timer: `45:00` → `44:59` → … visible cada segundo.
3. Descuentos: editar % y pulsar Guardar sin salir de la celda → recargar → persiste.
4. Aranceles: aplicar óptimo → banner "Óptimo financiero (VAN≈0)"; panel comparativo con los 3 valores; VAN/TIR idénticos a aplicar el mismo valor como Manual.
5. Permisos: tras SQL-C + re-login, Analista/Visualizador conservan sus módulos; UI de permisos sin "Configuracion", con "Amortizacion", "Sueldos Carrera", "Capital de Trabajo".
6. Reportes: menú navega, 3 tabs cargan, Exportar PDF genera archivo abrible; sin REP.EXPORTAR no hay botón; sin REP.VER no hay menú.

## Entregables al aprobar
- Copia del plan → `docs/PLAN-KAN46.md`.
- `sql/KAN46_arancel_modo_optimo.sql`, `sql/KAN46_permisos_diagnostico.sql`, `sql/KAN46_permisos_granular_apply.sql` (el usuario los ejecuta y reporta resultados).
- Fase 2 (plan aparte): Informe de Costos completo, Reporte Docente, Reporte Administrativo.
