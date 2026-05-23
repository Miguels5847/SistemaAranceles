# SistemaAranceles

Aplicacion de escritorio WPF para simulacion, proyeccion y analisis de aranceles en apertura de nuevas carreras universitarias. El proyecto usa arquitectura limpia, persistencia PostgreSQL/Supabase y control de acceso por roles/permisos.

## Stack

- .NET 8 + WPF + MVVM.
- EF Core + PostgreSQL/Supabase.
- CommunityToolkit.Mvvm.
- FluentValidation.
- BCrypt.Net-Next.
- ClosedXML para XLSX.
- QuestPDF para PDF.

## Arquitectura

```text
src/
  Domain/          Entidades, enums y reglas de dominio
  Application/     DTOs, interfaces, commands, queries y use cases
  Infrastructure/  EF Core, repositorios, servicios externos, DI
  Presentation/    WPF Views, ViewModels, converters, state y servicios UI
sql/               Scripts SQL versionados e idempotentes
docs/              Informes y documentacion auxiliar
tests/             Pruebas automatizadas
```

Reglas principales:

- Mantener Clean Architecture.
- Un caso de uso por clase cuando sea posible.
- Repositorios bajo interfaces `IRepositorio*`.
- ViewModels no deben inyectar `ContextoAplicacion` directamente; deben crear scopes con `IServiceProvider`.
- Permisos en runtime con `SesionActual.TienePermiso("MOD.ACCION")`.

## Quick Start

```powershell
# Compilar
dotnet build .\src\Presentation\SistemaAranceles.Presentation.csproj

# Ejecutar
dotnet run --project .\src\Presentation\SistemaAranceles.Presentation.csproj
```

Configurar `src/Presentation/appsettings.Local.json` con la cadena de conexion Supabase/PostgreSQL.

Si el build falla por archivo bloqueado (`MSB3027`), cerrar la app WPF en ejecucion y volver a compilar.

## Estado General

| Area | Estado |
| --- | --- |
| Seguridad, login, usuarios, RBAC y auditoria | Implementado |
| Inflacion | Implementado |
| Tasa de Retencion | Implementado |
| Proyeccion de Estudiantes | Implementado |
| Sueldos Carrera | Implementado |
| Recursos y Depreciacion | Implementado en su flujo principal |
| Mantenimiento e Inversion | Implementado en servicios/mantenimiento, activos diferidos e inversion inicial |
| Capital de Trabajo | Implementado como modulo por carrera |
| Demanda/Ingresos | Parcial/documentado |
| Costos/Gastos | Parcial/documentado |
| Sueldos Planta Central | Parcial/documentado |
| Financiamiento, Balance, Reportes, Analisis Financiero | Pendiente/parcial |

## Modulos Principales

### Seguridad, Usuarios y Auditoria

- Login con BCrypt.
- Sesion por inactividad.
- CRUD de usuarios.
- Soft delete/hard delete segun estado.
- RBAC con roles, permisos y overrides por usuario.
- Auditoria fire-and-forget desacoplada.

Rutas relevantes:

- `src/Application/UseCases/Usuarios`
- `src/Application/UseCases/Autenticacion`
- `src/Application/UseCases/Auditoria`
- `src/Presentation/Views/Usuarios`
- `src/Presentation/Views/Auditoria`

### Inflacion

- CRUD de inflacion anual.
- Importacion y proyeccion.
- Prioridad de datos importados sobre estimaciones cuando coinciden en año.

Rutas:

- `src/Application/UseCases/Inflacion`
- `src/Presentation/Views/Inflacion`

### Tasa de Retencion

- Configuracion por carrera y escenario.
- Simulaciones por cohorte.
- Metas de retencion y graduacion.
- Listado, seleccion y eliminacion de simulaciones.

Rutas:

- `src/Application/UseCases/TasaRetencion`
- `src/Presentation/Views/TasaRetencion`

### Proyeccion de Estudiantes

- Proyeccion por carrera, escenario y simulacion base.
- Matricula por periodo.
- Horas de docencia asistida y aplicacion practica.
- Docentes requeridos por periodo.
- Overrides de horas por periodo.
- Recalculo en cascada hacia Sueldos Carrera y Capital de Trabajo.

Rutas:

- `src/Application/UseCases/Estudiantes`
- `src/Presentation/Views/Estudiantes`
- `sql/KAN_OverrideHorasPeriodo.sql`

### Sueldos Carrera

- Vista autogenerada por carrera, escenario y periodo.
- Usa cargos, estudiantes proyectados e inflacion.
- Incluye resumen pivot de sueldos por periodos.
- Query por periodo: `GenerarTablaSueldosPeriodoQuery`.
- Resumen: `GenerarResumenSueldosQuery`.

Rutas:

- `src/Application/UseCases/CargosFacultad`
- `src/Presentation/Views/CargosFacultad`
- `src/Presentation/ViewModels/CargosFacultad`

### Recursos y Depreciacion

Modulo para activos fijos, inversion futura y depreciacion.

Incluye:

- CRUD de activos fijos.
- Catalogo base de activos.
- Categorias personalizadas.
- Matriz de inversiones futuras.
- Matriz de depreciacion.
- Totales consumidos por Inversion Inicial.

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

Modulo contenedor con pestañas:

- Mantenimiento.
- Activos Diferidos.
- Inversion Inicial.

Ruta:

- `src/Presentation/Views/MantenimientoInversion/MantenimientoInversionView.xaml`

#### Mantenimiento

Implementa la hoja de servicios y mantenimiento.

Pestañas:

- A. Servicios Basicos: CRUD de rubros.
- B. Mantenimiento: CRUD de rubros.
- C. Proyeccion Semestral: matriz por periodos, totales y detalle anual.

La proyeccion semestral muestra:

- años y periodos;
- numero de alumnos;
- costo de servicios basicos;
- costo de mantenimiento;
- total semestral por periodo;
- recuadros de total Servicios Basicos, Mantenimiento y Total General;
- detalle anual de rubros de servicios basicos y mantenimiento.

Rutas:

- `src/Application/UseCases/Mantenimiento`
- `src/Presentation/Views/Mantenimiento`
- `src/Presentation/ViewModels/Mantenimiento`
- `sql/KAN28_servicios_mantenimiento.sql`

#### Activos Diferidos

Implementa activos diferidos y tabla de amortizacion.

Pestañas:

- A. Activos Diferidos: CRUD de permisos/rubros.
- B. Tabla de Amortizacion: detalle y totales.

Notas recientes:

- Corregido el problema visual donde el texto o fila desaparecia al seleccionar.
- La tabla de amortizacion muestra totales por periodo.

Rutas:

- `src/Application/UseCases/ActivoDiferido`
- `src/Presentation/Views/ActivoDiferido`
- `src/Presentation/ViewModels/ActivoDiferido`
- `sql/KAN31_activos_diferidos.sql`

#### Inversion Inicial

Consolida:

- Activos Diferidos.
- Activos Fijos.
- Capital de Trabajo.
- Imprevistos 5%.
- Total de inversion inicial.

La tabla usa los encabezados:

- Tipo de Inversion.
- Valor.
- Monto total.

Rutas:

- `src/Application/UseCases/InversionInicial`
- `src/Presentation/Views/InversionInicial`
- `src/Presentation/ViewModels/InversionInicial`

### Capital de Trabajo

Implementacion KAN-29 basada en la hoja "6 Capital de trabajo".

Caracteristicas:

- Modulo por carrera.
- Filtros superiores: Carrera, Escenario y Periodo base.
- Menu ubicado debajo de Mantenimiento e Inversion.
- Pestaña A: Gastos de Servicio y Administracion, solo lectura.
- Pestañas B/C/D: CRUD de insumos operativos por carrera.
- Pestaña E: resumen detallado con cantidad, concepto, valor unitario, valor mensual y totales.

Reglas:

- El bloque A se alimenta desde Sueldos Carrera con el primer periodo de la proyeccion.
- `Valor mensual = TotalSemestre / 6`.
- B/C/D calculan `Valor mensual = Cantidad * ValorUnitario`.
- `CapitalTrabajo = TotalMensual * MesesCapitalTrabajo`.
- `MesesCapitalTrabajo` usa 2 por defecto.

Rutas:

- `src/Application/UseCases/CapitalTrabajo`
- `src/Application/DTOs/CapitalTrabajo`
- `src/Presentation/Views/CapitalTrabajo`
- `src/Presentation/ViewModels/CapitalTrabajo`
- `sql/KAN29_capital_trabajo.sql`

## Mapeo Excel a Sistema

| Hoja / Bloque Excel | Modulo del sistema |
| --- | --- |
| 1 Estudiantes | Proyeccion de Estudiantes |
| 2 Tasa de Retencion | Tasa de Retencion |
| 6 Capital de trabajo | Capital de Trabajo |
| 7 Sueldos | Sueldos Carrera |
| 8 Mantenimiento | Mantenimiento e Inversion / Mantenimiento |
| Activos fijos | Recursos y Depreciacion |
| Activos diferidos | Mantenimiento e Inversion / Activos Diferidos |
| Inversion inicial | Mantenimiento e Inversion / Inversion Inicial |

## Scripts SQL Relevantes

Scripts recientes y/o usados por modulos actuales:

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
- `KAN31_activos_diferidos.sql`

## Menu Actual

El menu se construye en `MainViewModel`.

Orden funcional observado:

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
12. Configuracion.
13. Reportes.
14. Auditoria.
15. Cerrar Sesion.

La visibilidad depende de permisos y rol administrador.

## Pruebas y Verificacion

Comando base:

```powershell
dotnet build .\src\Presentation\SistemaAranceles.Presentation.csproj
```

Durante los ultimos ajustes de UI y logica, el build del proyecto Presentation paso con:

- 0 errores.
- 0 advertencias.

## GitFlow

- `main`: estable.
- `develop`: integracion.
- `feature/KAN-xx-*`: trabajo por historia o modulo.

Rama observada al actualizar esta documentacion:

```text
feature/KAN-31-Activos-Diferidos
```

## Documentacion Adicional

- `.claude.md`: notas operativas para agentes.
- `contexto.md`: snapshot amplio del proyecto, si esta disponible.
- `docs/`: informes y guias.
- `src/Application/UseCases/BD ER`: diagramas ER.
- `src/Application/UseCases/Diagramas de Clase Dominio`: diagramas de clases.
- `src/Application/UseCases/Diagramas de Secuencia`: diagramas de secuencia.

## Restricciones de Trabajo

- No hacer commits automaticos.
- No usar comandos destructivos sin aprobacion explicita.
- No degradar login, RBAC ni auditoria.
- No introducir consultas N+1.
- No mover datos operativos por carrera a Datos Institucionales salvo regla institucional clara.
- Para valores monetarios en UI, preferir propiedades `Display` ya formateadas.

## Restore Supabase

Referencia rapida:

- Backup: formato custom, schema `public`, no owner, no privileges.
- Restore: pre-data, data y post-data; no owner, no privileges.
- `Clean before restore` solo en entornos de prueba.
