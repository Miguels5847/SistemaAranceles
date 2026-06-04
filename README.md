# Sistema de Aranceles Universitarios

Aplicacion de escritorio WPF para simulacion, proyeccion y analisis financiero de apertura de nuevas carreras universitarias. El sistema modela estudiantes, retencion, docentes, sueldos, inversiones, capital de trabajo, presupuestos, materiales, ingresos y parametros institucionales, con persistencia PostgreSQL/Supabase y control de acceso por roles/permisos.

## Estado Actual

Rama documentada: `feature/KAN-44-Dashboard-Financiero`.

El proyecto usa Clean Architecture y ya contiene los flujos principales de seguridad, usuarios, carreras, inflacion, tasa de retencion, proyeccion de estudiantes, sueldos carrera, datos institucionales, recursos/depreciacion, mantenimiento e inversion, capital de trabajo, Demanda e Ingresos, Costos y Gastos, Costo de Carrera y Analisis Financiero (incluido el cuadro regulatorio CES / INF CES).

| Area | Estado |
| --- | --- |
| Seguridad, login, sesion, usuarios, RBAC y auditoria | Implementado |
| Carreras y escenarios | Implementado |
| Inflacion anual, importacion y proyeccion | Implementado |
| Tasa de Retencion y Graduacion | Implementado |
| Proyeccion de Estudiantes | Implementado |
| Sueldos Carrera | Implementado |
| Datos Institucionales | Implementado y ampliado para Epicas 9-11 |
| Aporte Planta Central | Implementado |
| Recursos y Depreciacion | Implementado |
| Mantenimiento e Inversion | Implementado como contenedor de Mantenimiento, Activos Diferidos e Inversion Inicial |
| Capital de Trabajo | Implementado |
| Demanda e Ingresos, Epica 9 | Implementado en sus flujos principales |
| Costos y Gastos (Epica 10) | Implementado (matriz por periodo + col J = SUM(B:I)) |
| Costo de Carrera (arancel sugerido) | Implementado |
| Analisis Financiero (Epica 11) | Implementado: P&G, Flujo de Fondos, TIR/VAN, Periodo de Recuperacion, Punto de Equilibrio, Arancel Optimo, Dashboard y CES / INF CES |
| Financiamiento, Balance y Reportes | Estructura parcial o pendiente |

## Stack

- .NET 8.
- WPF + MVVM.
- CommunityToolkit.Mvvm.
- EF Core 8.
- PostgreSQL/Supabase mediante Npgsql.
- FluentValidation.
- BCrypt.Net-Next.
- ClosedXML, CsvHelper y HtmlAgilityPack.
- QuestPDF.
- OxyPlot.Wpf.
- MahApps.Metro e IconPacks.

## Arquitectura

```text
src/
  Domain/          Entidades, enums, value objects y reglas de dominio
  Application/     DTOs, interfaces, commands, queries y casos de uso
  Infrastructure/  EF Core, repositorios, servicios externos, SQL bootstrap y DI
  Presentation/    WPF Views, ViewModels, converters, state y servicios UI
sql/               Scripts SQL versionados e idempotentes para Supabase/PostgreSQL
docs/              Informes y documentacion auxiliar
tests/             Pruebas automatizadas de Application y Presentation
```

Reglas de arquitectura:

- Domain no depende de Application, Infrastructure ni Presentation.
- Application define contratos `IRepositorio*` y casos de uso.
- Infrastructure implementa repositorios y configuracion EF.
- Presentation consume casos de uso y ViewModels; no debe inyectar `ContextoAplicacion` directamente.
- Los ViewModels que necesitan servicios scoped usan `IServiceProvider.CreateScope()`.
- Los permisos en runtime se validan con `SesionActual.TienePermiso("MOD.ACCION")`; administrador puede tener bypass explicito cuando el modulo lo requiere.

## Configuracion y Ejecucion

Crear o ajustar `src/Presentation/appsettings.Local.json` con la cadena de conexion real. El archivo base `appsettings.json` incluye una cadena plantilla y `Session:TimeoutMinutes`.

```powershell
# Restaurar y compilar solucion
dotnet build

# Compilar la app WPF
dotnet build .\src\Presentation\SistemaAranceles.Presentation.csproj

# Ejecutar
dotnet run --project .\src\Presentation\SistemaAranceles.Presentation.csproj

# Pruebas
dotnet test
```

Si la app WPF esta abierta, Windows puede bloquear `SistemaAranceles.Presentation.exe` y el build normal falla con `MSB3027`/`MSB3021`. Cerrar la app y repetir, o validar sin tocar el binario abierto con una salida temporal:

```powershell
$out = Join-Path $env:TEMP 'SistemaArancelesCodexBuild\Presentation\'
dotnet build .\src\Presentation\SistemaAranceles.Presentation.csproj -p:OutDir=$out

$testOut = Join-Path $env:TEMP 'SistemaArancelesCodexTest\'
dotnet test -p:OutDir=$testOut
```

## Navegacion y Sesion

La ventana principal esta en `src/Presentation/MainWindow.xaml` y el menu lateral se construye en `MainViewModel`.

El menu actual se ordena asi, segun permisos:

1. Usuarios.
2. Carreras.
3. Inflacion.
4. Tasa de Retencion y Graduacion.
5. Proyeccion de Estudiantes.
6. Sueldos Carrera.
7. Datos Institucionales.
8. Aporte Planta Central.
9. Recursos y Depreciacion.
10. Mantenimiento e Inversion.
11. Capital de Trabajo.
12. Demanda e Ingresos.
13. Costos y Gastos.
14. Analisis Financiero.
15. Configuracion.
16. Reportes.
17. Auditoria.
18. Cerrar Sesion.

Permisos asociados a los modulos financieros: `CG.VER` (Costos y Gastos), `AF.VER` (Analisis Financiero). El administrador tiene bypass.

Notas de UI recientes:

- El item activo del menu queda sombreado con fondo resaltado, barra lateral amarilla y sombra.
- Los textos largos del menu, como "Tasa de Retencion y Graduacion", pueden envolver en dos lineas y tienen tooltip con el nombre completo.
- El contador de sesion se actualiza al iniciar y al resetear actividad.
- Al recuperar foco desde otra ventana se ignoran eventos automaticos durante una breve ventana de gracia para evitar que el timer se reinicie solo por cambiar de ventana.

## Modulos Funcionales

### Seguridad, Usuarios y Auditoria

Incluye:

- Login con BCrypt.
- Control de sesion por inactividad.
- CRUD de usuarios.
- Soft delete/hard delete segun estado.
- Roles, permisos y permisos efectivos por usuario.
- Auditoria desacoplada.

Rutas:

- `src/Application/UseCases/Autenticacion`
- `src/Application/UseCases/Usuarios`
- `src/Application/UseCases/Permisos`
- `src/Application/UseCases/Auditoria`
- `src/Presentation/Views/Usuarios`
- `src/Presentation/Views/Auditoria`

### Carreras e Inflacion

Carreras y escenarios alimentan los modulos academicos y financieros. Inflacion permite CRUD, importacion, proyeccion y seleccion de valores por anio.

Rutas:

- `src/Application/UseCases/Inflacion`
- `src/Presentation/Views/Carreras`
- `src/Presentation/Views/Inflacion`

### Tasa de Retencion y Graduacion

Incluye configuraciones por carrera/escenario, simulaciones, criterios de referencia, metas de retencion y graduacion, y seleccion de simulacion base para la proyeccion.

Rutas:

- `src/Application/UseCases/TasaRetencion`
- `src/Presentation/Views/TasaRetencion`
- `src/Domain/Retencion`

### Proyeccion de Estudiantes

Calcula matricula, estudiantes por periodo, horas de docencia asistida, aplicacion practica y docentes requeridos.

Reglas importantes actuales:

- Las horas acumuladas reales se mantienen en la matriz academica.
- Para desglosar docentes se redondean las horas acumuladas del periodo a entero antes de llamar a `DesglosarDocentesPorPeriodo`.
- Las filas de docentes por periodo muestran enteros.
- `Tiempo Completo = 18h`, `Medio Tiempo = 12h`, `Tiempo Parcial` cubre el residuo entero.
- Overrides de horas por periodo se soportan mediante `OverrideHorasPeriodo`.

Rutas:

- `src/Application/UseCases/Estudiantes`
- `src/Application/DTOs/Estudiantes`
- `src/Presentation/Views/Estudiantes`
- `sql/KAN_OverrideHorasPeriodo.sql`

### Sueldos Carrera

Calcula y muestra sueldos por carrera, escenario y periodo usando cargos, proyeccion de estudiantes, inflacion y consolidado docente.

Rutas:

- `src/Application/UseCases/CargosFacultad`
- `src/Presentation/Views/CargosFacultad`
- `src/Presentation/ViewModels/CargosFacultad`

Scripts:

- `sql/KAN20b_CargoFacultad_TipoContrato_TarifaHora.sql`

### Datos Institucionales y Aporte Planta Central

Datos Institucionales centraliza parametros institucionales usados por presupuesto, seguro, becas, matricula, inflacion base y calculos de Demanda e Ingresos.

Aporte Planta Central calcula cargos y proyecciones asociadas a planta central.

Rutas:

- `src/Application/UseCases/SueldosPlantaCentral`
- `src/Presentation/Views/DatosInstitucionales`
- `src/Presentation/Views/PlantaCentral`

Scripts:

- `sql/KAN22_DatosInstitucionales.sql`
- `sql/KAN22_permisos.sql`
- `sql/KAN35_datos_institucionales_demanda_ingresos.sql`

### Recursos y Depreciacion

Modulo para activos fijos, catalogo base, categorias personalizadas, inversion futura, matriz de inversiones y matriz de depreciacion.

Rutas:

- `src/Application/UseCases/RecursosFisicosDepreciacion`
- `src/Presentation/Views/RecursosFisicos`
- `src/Presentation/ViewModels/RecursosFisicos`

Scripts:

- `sql/KAN24_activo_fijo.sql`
- `sql/KAN24_catalogo_y_tipo_calculo.sql`
- `sql/KAN24_categoria_personalizada.sql`
- `sql/KAN24_permisos.sql`
- `sql/KAN25_inversion_futura.sql`

### Mantenimiento e Inversion

Modulo contenedor con tres bloques:

- Mantenimiento.
- Activos Diferidos.
- Inversion Inicial.

Ruta:

- `src/Presentation/Views/MantenimientoInversion/MantenimientoInversionView.xaml`

#### Mantenimiento

Implementa servicios basicos, mantenimiento y proyeccion semestral con matrices por periodo, totales y detalle anual.

Rutas:

- `src/Application/UseCases/Mantenimiento`
- `src/Presentation/Views/Mantenimiento`
- `src/Presentation/ViewModels/Mantenimiento`
- `sql/KAN28_servicios_mantenimiento.sql`

#### Activos Diferidos

CRUD de activos diferidos y tabla de amortizacion con resumen anual.

Rutas:

- `src/Application/UseCases/ActivoDiferido`
- `src/Presentation/Views/ActivoDiferido`
- `src/Presentation/ViewModels/ActivoDiferido`
- `sql/KAN31_activos_diferidos.sql`

#### Inversion Inicial

Consolida activos diferidos, activos fijos, capital de trabajo, imprevistos y total de inversion inicial.

Rutas:

- `src/Application/UseCases/InversionInicial`
- `src/Presentation/Views/InversionInicial`
- `src/Presentation/ViewModels/InversionInicial`

### Capital de Trabajo

Implementa la hoja "6 Capital de trabajo" por carrera, escenario y periodo base.

Incluye:

- Gastos de Servicio y Administracion desde Sueldos Carrera.
- Materiales y suministros.
- Suministros de aseo y limpieza.
- Accesorios y materiales.
- Resumen con valor mensual y capital de trabajo.

Reglas:

- `Valor mensual = TotalSemestre / 6` para gastos de servicio y administracion.
- `Valor mensual = Cantidad * ValorUnitario` para los bloques de insumos.
- `CapitalTrabajo = TotalMensual * MesesCapitalTrabajo`.
- `MesesCapitalTrabajo` usa 2 por defecto.

Rutas:

- `src/Application/UseCases/CapitalTrabajo`
- `src/Application/DTOs/CapitalTrabajo`
- `src/Presentation/Views/CapitalTrabajo`
- `src/Presentation/ViewModels/CapitalTrabajo`
- `sql/KAN29_capital_trabajo.sql`

## Epica 9: Demanda e Ingresos

La vista principal es `src/Presentation/Views/DemandaIngresos/DemandaIngresosView.xaml` y el ViewModel compuesto es `DemandaIngresosViewModel`.

Pestanas actuales:

1. Configuracion de Arancel.
2. Demanda Proyectada.
3. Presupuestos y Seguro.
4. Ingresos Proyectados.
5. Materiales en Cantidades.
6. Materiales Monetarios.

La pestana `7. Resumen` fue eliminada. El "Resumen consolidado" ahora vive dentro de la pestana `1. Configuracion de Arancel`, debajo de la tabla/formulario de configuraciones.

### KAN-32: Configuracion de Arancel

- Tabla `configuracion_arancel_carrera`.
- Configuracion por carrera y opcionalmente por escenario.
- Modo Manual implementado.
- Modo Automatico queda pendiente de Epica 10/Costo Carrera.
- Arancel efectivo y matricula efectiva.
- Fallback de configuracion especifica a configuracion global de la carrera.
- Cambio de escenario refresca arancel efectivo sin reutilizar estado anterior.

Rutas:

- `src/Application/UseCases/DemandaIngresos/ObtenerArancelEfectivoQuery.cs`
- `src/Application/UseCases/DemandaIngresos/GuardarConfiguracionArancelCarreraCommand.cs`
- `src/Infrastructure/Persistence/Repositories/RepositorioConfiguracionArancelCarrera.cs`
- `sql/KAN32_configuracion_arancel_carrera.sql`

### KAN-33: Ingresos Proyectados

- Usa demanda proyectada y arancel efectivo.
- Calcula ingreso bruto, becas, ingreso neto y total general.
- Presenta matriz Ciclo x Periodo.

Ruta:

- `src/Application/UseCases/DemandaIngresos/CalcularIngresosProyectadosQuery.cs`

### KAN-34: Materiales con Inflacion

Materiales en Cantidades y Materiales Monetarios ya soportan consumos configurables.

Unidades soportadas:

- `por_estudiante`: `cantidad = estudiantes_periodo * consumo`.
- `por_estudiante_mes`: `cantidad = estudiantes_periodo * consumo * meses_operativos`.
- `fijo_periodo`: `cantidad = consumo`.
- `por_docente`: `cantidad = docentes_necesarios_periodo * consumo + cantidad_fija_adicional`.

Notas:

- `por_docente` usa la fila real `Docentes Requeridos` de Demanda Proyectada.
- `cantidad_fija_adicional` permite casos como `docentes + 4` para Grapadora/Perforadora.
- Materiales monetarios siguen usando `cantidad * precio_unitario * factor_inflacion`.
- Precios vienen desde Capital de Trabajo mediante item vinculado.
- La UI muestra unidad amigable y columna `Adicional fijo`.

Rutas:

- `src/Domain/Entities/RatioMaterialDemanda.cs`
- `src/Domain/Enums/UnidadRatioMaterial.cs`
- `src/Application/UseCases/DemandaIngresos/CalcularMaterialesPorPeriodoQuery.cs`
- `src/Application/UseCases/DemandaIngresos/GuardarRatioMaterialDemandaCommand.cs`
- `src/Infrastructure/Persistence/Repositories/RepositorioRatioMaterialDemanda.cs`
- `src/Presentation/ViewModels/DemandaIngresos/DemandaIngresosViewModel.cs`

Scripts:

- `sql/KAN34_ratio_material_demanda.sql`
- `sql/KAN34_update_ratio_material_demanda_unidad_fijo_periodo.sql`
- `sql/KAN34_add_por_docente_ratio_material_demanda.sql`

### KAN-35: Presupuestos y Seguro

- Presupuestos institucionales.
- Montos asignados a carrera.
- Seguro estudiantil.
- Becas institucionales.
- Matricula y valor ciclo por estudiante.
- Inflacion aplicada desde Datos Institucionales.

Rutas:

- `src/Application/UseCases/DemandaIngresos/ObtenerPresupuestosCarreraQuery.cs`
- `src/Application/DTOs/DemandaIngresos/PresupuestoDemandaDto.cs`
- `src/Presentation/Views/DatosInstitucionales`
- `sql/KAN35_datos_institucionales_demanda_ingresos.sql`

## Epica 10: Costos, Gastos y Costo de Carrera

### Costos y Gastos (hoja Excel "10 Costos y Gastos")

Consolida todos los rubros de costo y gasto por periodo (8 semestres) a partir de Sueldos Carrera, Mantenimiento, Recursos/Depreciacion, Materiales, Presupuestos y Seguro. La columna J del Excel = `SUM(B:I)` = total a 8 semestres por rubro, reproducida en codigo con `ValoresPorPeriodo.Sum(p => p.Rubro)`.

- Query: `src/Application/UseCases/CostosGastos/ObtenerMatrizCostosGastosQuery.cs` -> `MatrizCostosGastosDto`.
- DTOs: `src/Application/DTOs/CostosGastos/CostosGastosDtos.cs`.
- `MatrizCostosGastosDto.ValoresPorPeriodo` = lista por periodo de `CostoGastoPeriodoDto`; `.TotalGeneral` = `Sum(TotalCostosGastos)`.
- `CostoGastoPeriodoDto` expone rubros (docentes `TiempoCompletoPhd/Mgs/MedioTiempo/TiempoParcial/OcasionalTipo2TecnicoDocente`, `MantenimientoEdificio`, `CapacitacionDocente`, `Internacionalizacion`, `MaterialesSuministros`, `ServiciosBasicos`, `CostoSeguroEstudiantil`, `Investigacion`, `Vinculacion`, `Depreciacion`, `Interes`) y computados `GastosAdministracion`, `GastosVentas` (= `MarketingComunicacion`), `OtrosGastos`, `GastoFinanciero` (= `Interes`), `CostosServicios`, `TotalCostosGastos`.
- `factorImprevisto` (1.05 por defecto) atraviesa las queries de costos y financieras.
- Vista/VM: `src/Presentation/Views/CostosGastos`, `src/Presentation/ViewModels/CostosGastos`. Permiso `CG.VER`.

### Costo de Carrera (arancel sugerido por costo)

Calcula el arancel "propuesto" a partir del costo total de la carrera. Es la base del Modo Automatico de arancel y del cuadro CES.

- Query: `src/Application/UseCases/CostosGastos/ObtenerCostoCarreraQuery.cs` -> `CostoCarreraResultadoDto`.
- Acepta `matrizPrecalculada` para no recalcular Costos/Gastos.
- Campos: `ArancelSugeridoSemestre`, `MatriculaSugerida`, `TotalPorSemestre`, `TieneDatos`.
- `ObtenerArancelEfectivoQuery.FuenteCalculo` = "Referencial por costo de carrera".

## Epica 11: Analisis Financiero

Ventana: `src/Presentation/Views/AnalisisFinanciero/AnalisisFinancieroView.xaml`
ViewModel: `src/Presentation/ViewModels/AnalisisFinanciero/AnalisisFinancieroViewModel.cs`
Permiso: `AF.VER`.

`RefrescarAsync` orquesta todas las queries con un patron de **precalculados**: calcula los DTOs pesados una sola vez (matriz costos, demanda, ingresos, costo carrera, arancel efectivo, inversiones) y los pasa a las queries dependientes para evitar round-trips N+1 a Supabase. Los mensajes y advertencias se muestran como banners cerrables (X).

Pestanas:

1. Estado de Perdidas y Ganancias.
2. Flujo de Fondos.
3. Indicadores (TIR / VAN).
4. Periodo de Recuperacion.
5. Punto de Equilibrio.
6. Arancel Optimo (biseccion).
7. Dashboard Financiero.
8. CES / INF CES.

### Modo de calculo

Modo unico fijo: **Compatible Excel**, que reproduce el comportamiento del Excel del tutor (VAN ~= 0, TIR ~= 9.54%): flujos semestrales, VAN con la convencion `=VAN()` de Excel (periodo 0 tambien descontado) y sin recuperacion de capital de trabajo. El modo "Tecnico" (anual ortodoxo) se elimino porque la tesis se compara directamente contra el Excel.

### Reglas financieras criticas

- **Becas**: se modelan como **descuento de ingreso** (costo = 0), nunca como costo; coincide con el Excel y evita un doble conteo previo.
- **TIR**: TIR normal (no TIRM); con multiples cambios de signo / multiples raices se reporta la raiz **mas cercana a la TMR**.
- **Periodo de Recuperacion**: no se asume que siempre converge; hay estados de no-convergencia.
- **Punto de Equilibrio**: se calcula con el **ultimo periodo proyectado** (no promedio).
- **Arancel Optimo (biseccion)**: tolerancia de VAN, maximo de iteraciones y demas parametros son configurables desde Datos Institucionales; mensaje claro al alcanzar el maximo de iteraciones y estados de convergencia explicitos.
- El Dashboard ya no exporta CSV.
- La opcion "Costo Carrera" es informacion **referencial** y no rompe el analisis al seleccionarla.

### Pestana 8: CES / INF CES (Consejo de Educacion Superior, Ecuador)

Reproduce las hojas Excel "INF CES" y "CES" como cuadro regulatorio de **salida (solo lectura)**, salvo un campo referencial editable (costo de carreras similares, dato externo de la IES).

- DTO: `src/Application/DTOs/AnalisisFinanciero/CesDto.cs` (`CesInfFilaDto`, `CesParametroFilaDto`, `CesDistribucionFilaDto`).
- Query: `src/Application/UseCases/AnalisisFinanciero/ObtenerCesQuery.cs` (registrada en `App.xaml.cs`); acepta precalculados.
- ViewModel: `Ces`, `InfCes`, `ParametrosCes`, `DistribucionCes`, `TieneCes`, `CostoCarrerasSimilares`.
- **INF CES**: presupuesto de la 1a cohorte clasificado por las 4 funciones sustantivas (Provision de educacion superior / Fomento cientifico-tecnologico / Vinculacion con la sociedad / Otros), con gastos corrientes (= suma por rubro de la matriz + becas) e inversion (`InversionInicialTotalDto.ActivosDiferidos` + `SubtotalActivosFijos` + suma de `MatrizInversiones.TotalesPorPeriodo` + `MaterialesProyectadosDto.TotalCosto`).
- **CES**: parametros de justificacion del arancel (ratios academico/total, investigacion/total, vinculacion/total, infraestructura/total; arancel vigente vs propuesto; matricula = 10%; estudiantes 1a cohorte; remuneracion academica promedio).
- **Distribucion referencial de costos**: bloque destacado que muestra como se reparte el 100% del costo de la carrera por categoria (academico/docentes, administrativo, bienes y servicios, becas, investigacion, vinculacion, otros = ~100% corriente) e infraestructura/inversion mostrada aparte como referencial.
- La remuneracion academica promedio sale de `GenerarResumenSueldosQuery` (cargos docentes tiempo completo) y es **aproximada** (la formula original del Excel es opaca).

## Mapeo Excel a Sistema

| Hoja / Bloque Excel | Modulo |
| --- | --- |
| 1 Estudiantes | Proyeccion de Estudiantes |
| 2 Tasa de Retencion | Tasa de Retencion y Graduacion |
| 6 Capital de trabajo | Capital de Trabajo |
| 7 Sueldos | Sueldos Carrera |
| 8 Mantenimiento | Mantenimiento e Inversion / Mantenimiento |
| Activos fijos y depreciacion | Recursos y Depreciacion |
| Activos diferidos y amortizacion | Mantenimiento e Inversion / Activos Diferidos |
| Inversion inicial | Mantenimiento e Inversion / Inversion Inicial |
| Demanda, ingresos y materiales | Demanda e Ingresos |
| 10 Costos y Gastos | Costos y Gastos |
| Costo de carrera / arancel sugerido | Costo de Carrera (Analisis Financiero) |
| Estado de resultados, flujo, TIR/VAN, punto de equilibrio | Analisis Financiero |
| INF CES y CES | Analisis Financiero / pestana CES / INF CES |

## Scripts SQL Relevantes

Los scripts viven en `sql/` y son preferentemente idempotentes. No se ejecutan automaticamente contra Supabase desde la aplicacion.

Scripts destacados:

- `KAN_OverrideHorasPeriodo.sql`
- `KAN20b_CargoFacultad_TipoContrato_TarifaHora.sql`
- `KAN22_DatosInstitucionales.sql`
- `KAN22_permisos.sql`
- `KAN23_permisos_tre_es.sql`
- `KAN24_activo_fijo.sql`
- `KAN24_catalogo_y_tipo_calculo.sql`
- `KAN24_categoria_personalizada.sql`
- `KAN24_permisos.sql`
- `KAN25_inversion_futura.sql`
- `KAN28_servicios_mantenimiento.sql`
- `KAN29_capital_trabajo.sql`
- `KAN30_DatosInstitucionalesParametrosInversion.sql`
- `KAN31_activos_diferidos.sql`
- `KAN32_configuracion_arancel_carrera.sql`
- `KAN34_ratio_material_demanda.sql`
- `KAN34_update_ratio_material_demanda_unidad_fijo_periodo.sql`
- `KAN34_add_por_docente_ratio_material_demanda.sql`
- `KAN35_datos_institucionales_demanda_ingresos.sql`

## Pruebas

Proyectos:

- `tests/Application.Tests/SistemaAranceles.Application.Tests.csproj`
- `tests/Presentation.Tests/SistemaAranceles.Presentation.Tests.csproj`

Cobertura actual de pruebas automatizadas (Application 108 + Presentation 2 = 110):

- Reglas de proyeccion de estudiantes.
- Calculo de docentes.
- Inflacion y sueldos.
- Recursos fisicos y depreciacion.
- Inversion futura.
- Datos institucionales.
- Demanda e Ingresos: aranceles y materiales.
- Costos/Gastos y calculos de Analisis Financiero.
- ViewModel de consumos en Proyeccion de Estudiantes.

Comandos:

```powershell
dotnet build .\src\Presentation\SistemaAranceles.Presentation.csproj
dotnet test
```

Ultima verificacion conocida:

- Build de Presentation: 0 warnings, 0 errores.
- `dotnet test`: Application 108 + Presentation 2 pruebas superadas, 0 errores.

## Convenciones de Trabajo

- No hacer commits automaticos.
- No usar `git reset --hard`, `git checkout --` ni borrados destructivos sin pedido explicito.
- No tocar Supabase desde codigo para cambios de schema; preparar scripts SQL y esperar ejecucion/confirmacion.
- Mantener cambios acotados al modulo solicitado.
- Antes de editar, buscar referencias con `rg`.
- Para cambios WPF, compilar `SistemaAranceles.Presentation.csproj`.
- Para cambios de calculo o contratos, ejecutar `dotnet test`.
- Para valores monetarios y decimales en UI, preferir propiedades `Display` ya formateadas.

## Pendientes y Riesgos Conocidos

- Implementados: Costos y Gastos, Costo de Carrera y Analisis Financiero (P&G, Flujo, TIR/VAN, Recuperacion, Punto de Equilibrio, Arancel Optimo, Dashboard y CES / INF CES).
- Pendientes mayores: Financiamiento, Balance y Reportes.
- La remuneracion academica promedio del cuadro CES (C5) es aproximada; la formula original del Excel (`7 Sueldos`!E238) usa un divisor opaco y no se reproduce al centavo.
- Si la app esta abierta, el build normal puede fallar por bloqueo del `.exe`.
- Los cambios visuales en WPF requieren cerrar y reabrir la app para ver el binario actualizado.
- Mantener sincronizados `README.md` y `.claude.md` cuando cambien reglas de Demanda e Ingresos, Costos, Analisis Financiero, docentes, menu o sesion.

## Documentacion Adicional

- `.claude.md`: guia operativa para agentes.
- `contexto.md`: contexto amplio del proyecto.
- `docs/`: informes y material auxiliar.
- `Diagramas.md` y `Diagramas Analisis.md`: diagramas y analisis historico.
- `src/Application/UseCases/BD ER`: diagramas ER.
- `src/Application/UseCases/Diagramas de Clase Dominio`: diagramas de clases.
- `src/Application/UseCases/Diagramas de Secuencia`: diagramas de secuencia.
