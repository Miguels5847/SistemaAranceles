# Sistema de Aranceles Universitarios

Aplicación de escritorio (Windows, WPF) para **calcular el arancel viable de una carrera
universitaria**. Proyecta estudiantes, docentes, costos, inversiones e ingresos, y entrega
indicadores financieros (VAN, TIR, punto de equilibrio), el **arancel óptimo** (VAN ≈ 0),
los cuadros regulatorios **CES / INF CES** y reportes exportables a PDF y Excel.

Persistencia en PostgreSQL (Supabase) y control de acceso por roles y permisos.

> 📘 **¿Eres usuario final?** Lee la **[Guía de Usuario](docs/GUIA_USUARIO.md)**: explica
> el flujo completo botón por botón, sin tecnicismos.

---

## Índice

- [¿Qué hace el sistema?](#qué-hace-el-sistema)
- [Stack](#stack)
- [Arquitectura](#arquitectura)
- [Configuración y ejecución](#configuración-y-ejecución)
- [Distribución (ejecutable)](#distribución-ejecutable-para-otra-pc)
- [Módulos](#módulos)
- [Mapeo Excel → Sistema](#mapeo-excel--sistema)
- [Reglas de negocio clave](#reglas-de-negocio-clave)
- [Pruebas](#pruebas)
- [Convenciones de trabajo](#convenciones-de-trabajo)
- [Documentación adicional](#documentación-adicional)

---

## ¿Qué hace el sistema?

El flujo de trabajo completo (el menú lateral lo replica, numerado del 1 al 5):

```text
 Carrera → Metas de retención/graduación → Proyección de estudiantes
        → Activos, mantenimiento e inversión → Arancel y materiales (demanda)
        → Costos consolidados → Análisis financiero (VAN/TIR/arancel óptimo)
        → Reportes PDF/XLSX
```

Cada corrida se hace por **carrera + escenario** (Histórico, Optimista, Pesimista).
Todos los módulos del flujo están implementados; el estado actual del proyecto es
validación contra la matriz Excel institucional y ajustes finos.

## Stack

| Capa                | Tecnologías                                                                    |
| ------------------- | ------------------------------------------------------------------------------ |
| Runtime / UI        | .NET 8, WPF + MVVM (CommunityToolkit.Mvvm), MahApps.Metro + IconPacks, OxyPlot |
| Datos               | EF Core 8, Npgsql → PostgreSQL/Supabase                                        |
| Reglas / validación | FluentValidation, BCrypt.Net-Next                                              |
| Export / archivos   | QuestPDF (PDF), ClosedXML (XLSX), CsvHelper, HtmlAgilityPack                   |

## Arquitectura

El sistema sigue **Clean Architecture** (arquitectura por capas con inversión de
dependencias). El código se organiza en cuatro proyectos .NET independientes bajo `src/`,
ordenados de adentro (núcleo de negocio) hacia afuera (detalles técnicos). La regla de oro:
**las dependencias solo apuntan hacia adentro**; el núcleo no conoce la infraestructura ni la UI.

```text
                 +---------------------------------------------------+
   depende de    |                  Presentation                     |   WPF + MVVM
  hacia adentro  |   (Views, ViewModels, Converters, State)          |
       |         +------------------------+--------------------------+
       v                                  | usa
                 +------------------------v--------------------------+
                 |                 Infrastructure                    |   EF Core, QuestPDF,
                 |   (Repositorios, EF Config, Export, Servicios, DI)|   ClosedXML, Npgsql
                 +------------------------+--------------------------+
                                          | implementa puertos / usa
                 +------------------------v--------------------------+
                 |                  Application                      |   Casos de uso (CQRS),
                 |   (UseCases, DTOs, Interfaces=puertos, Services)  |   servicios de cálculo
                 +------------------------+--------------------------+
                                          | usa
                 +------------------------v--------------------------+
                 |                     Domain                        |   Entidades + reglas
                 |   (Entities, Enums, ValueObjects, Constantes)     |   (no depende de NADA)
                 +---------------------------------------------------+
```

### Estructura de carpetas

```text
src/
  Domain/            Núcleo de negocio, SIN dependencias externas
    Entities/          Entidades con invariantes (Carrera, ConfiguracionRetencion,
                       CargoFacultad, ActivoFijo, DatosInstitucionales, ...)
    Enums/ ValueObjects/ Constantes/ Retencion/ Common/

  Application/       Orquestación, casos de uso y contratos (puertos)
    UseCases/          Queries (Obtener*Query) y Commands (Guardar*/Eliminar*Command) por módulo
    Services/          Cálculo puro: CalculadoraVAN/TIR/TMR, CalculadoraArancelOptimoBiseccion, ...
    Interfaces/        Puertos IRepositorio* / IServicio* (los implementa Infrastructure)
    DTOs/              Objetos de transferencia por módulo

  Infrastructure/    Detalles técnicos: implementa los puertos de Application
    Persistence/       Repositorios EF Core, mapeo a snake_case, migraciones
    Export/            PDF (QuestPDF) y XLSX (ClosedXML)
    Servicios/ DI/     Hash BCrypt, importaciones, composition root

  Presentation/      WPF + MVVM (capa más externa)
    Views/ ViewModels/ Converters/ Behaviors/ State/

sql/                 Scripts SQL versionados e idempotentes (se ejecutan manualmente)
docs/                Guía de usuario, distribución, diagramas UML/ER, casos de uso
tests/               Pruebas de Application y Presentation
scripts/             publicar.ps1 (ejecutable + zip), utilidades
```

### Regla de dependencias

- **Domain** no depende de nada: C# puro, invariantes validadas en el constructor
  (`GuardiaDominio`). Único proyecto sin paquetes externos.
- **Application** depende solo de Domain. Define los puertos (`IRepositorio*`,
  `IServicio*`) y los casos de uso. No conoce EF Core ni WPF.
- **Infrastructure** implementa los puertos (`RepositorioX : IRepositorioX`). Aquí vive
  la base de datos, los archivos y el PDF.
- **Presentation** invoca casos de uso desde los ViewModels; no inyecta el `DbContext`.

### Patrones y convenciones de código

- **CQRS ligero**: `*Query` para lecturas, `*Command` para escrituras.
- **Servicios de cálculo puro** (`Calculadora*`, `Consolidador*`): deterministas, sin I/O,
  testeables (VAN, TIR, TMR, bisección del arancel óptimo).
- **Patrón de precalculados**: los DTOs pesados (matriz de costos, demanda, ingresos) se
  calculan una vez y se pasan a las queries dependientes — evita round-trips N+1 a Supabase.
- **Nomenclatura**: puertos `IRepositorio<Entidad>` / implementaciones `Repositorio<Entidad>`;
  DTOs con sufijo `Dto`; propiedades `*Display` ya formateadas para la UI.
- **Mapeo EF explícito** a `snake_case` con `HasColumnName`.
- **Permisos en runtime**: `SesionActual.TienePermiso("MOD.ACCION")`, con bypass del
  administrador donde aplica.
- **Borrado lógico + índice único** (regla anti-23505): si una tabla usa soft delete y
  tiene índice único, el índice debe ser **filtrado** (`WHERE esta_activo = TRUE`, como
  `mantenimiento_servicio`) o el insert debe **revivir** la fila inactiva de la misma
  clave (como `carrera`, `configuracion_retencion`, `proyeccion_estudiantes`). Nunca
  insertar a ciegas.
- **Sin dependencias ocultas entre carreras**: los datos por carrera se copian desde
  plantillas/catálogos (siembra al crear la carrera o botón "Generar por defecto");
  prohibido que una carrera "preste" sus filas a otra en tiempo de consulta — borrar la
  carrera dueña rompería a las demás (bug de la plantilla de cargos, KAN-47).

## Configuración y ejecución

1. Crear `src/Presentation/appsettings.Local.json` con la cadena de conexión real
   (el archivo está **ignorado en git**; `appsettings.json` trae una plantilla sin
   credenciales). **Nunca** commitear cadenas de conexión ni contraseñas.
2. Compilar y ejecutar:

```powershell
dotnet build                                                          # solución completa
dotnet run --project .\src\Presentation\SistemaAranceles.Presentation.csproj
dotnet test                                                           # pruebas
```

> **Tip:** si la app WPF está abierta, Windows bloquea el `.exe` y el build falla
> (`MSB3027`). Compila hacia una carpeta temporal:
>
> ```powershell
> dotnet build .\src\Presentation\SistemaAranceles.Presentation.csproj -p:OutDir=$env:TEMP\SABuild\
> dotnet test -p:OutDir=$env:TEMP\SATest\
> ```

Los cambios de esquema de base de datos **no** se aplican automáticamente: los scripts
viven en `sql/` y se ejecutan manualmente en Supabase (son idempotentes). Algunas columnas
nuevas se auto-reparan al iniciar la app (DDL self-healing en repositorios concretos).

## Distribución (ejecutable para otra PC)

```powershell
powershell -ExecutionPolicy Bypass -File scripts\publicar.ps1
```

Produce:

- `publish\` — `SistemaAranceles.exe` autocontenido (incluye .NET 8, ~107 MB),
  configuración, fuentes PDF e `instalar.bat`.
- `SistemaAranceles-win64.zip` — el paquete completo para USB/correo/Drive (git-ignorado).

En la PC destino: descomprimir y ejecutar `instalar.bat` (copia a
`%LOCALAPPDATA%\SistemaAranceles\App` y crea acceso directo; sin permisos de
administrador). El `.exe` necesita a su lado `appsettings.Local.json` y la carpeta
`LatoFont`. Detalles y diagnóstico: [docs/DISTRIBUCION.md](docs/DISTRIBUCION.md).

## Módulos

El menú lateral agrupa los módulos por flujo (cada ítem se muestra según permisos):

| Grupo                         | Módulos                                                                                                                         | Notas                                                                                                                                                               |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Administración                | Usuarios, Auditoría                                                                                                             | RBAC, permisos efectivos, audit log de acciones críticas.                                                                                                           |
| 1 · Configuración base        | Carreras, Inflación, **Tasa de Retención y Graduación**, Datos Institucionales                                                  | El input de retención/graduación son **metas acumuladas**; la tasa por ciclo se deriva (`meta^(1/pasos)`).                                                          |
| 2 · Proyección académica      | Proyección de Estudiantes, Demanda e Ingresos                                                                                   | Matriz de cohortes; docentes TC=18h / MT=12h / TP residuo. Arancel (óptimo/manual/referencial), descuentos por ciclo, materiales con 4 unidades de consumo.         |
| 3 · Costos y recursos         | Sueldos Carrera, Aporte Planta Central, Recursos y Depreciación, Mantenimiento e Inversión, Capital de Trabajo, Costos y Gastos | "Generar por defecto" disponible en activos, servicios y consumos. Costos y Gastos consolida todo (hoja Excel "10").                                                |
| 4 · Financiamiento y análisis | Amortización, Análisis Financiero                                                                                               | 3 fuentes de financiamiento + tabla francesa; P&G, flujo, TIR/VAN, recuperación, punto de equilibrio, arancel óptimo (bisección), dashboard, CES/INF CES y balance. |
| 5 · Resultados                | Reportes                                                                                                                        | Informe por dirección destinataria; PDF (QuestPDF) y XLSX (ClosedXML); inicia con "Resumen de Indicadores Clave".                                                   |

**Ayudas al usuario:** cada módulo tiene un badge "?" con qué ingresar y qué resultados
produce, y el módulo Carreras tiene el botón **"Guía completa"** (9 pasos, botón por botón).

## Mapeo Excel → Sistema

El sistema reproduce la matriz financiera Excel institucional:

| Hoja / bloque Excel                                | Módulo                                              |
| -------------------------------------------------- | --------------------------------------------------- |
| 1 Estudiantes                                      | Proyección de Estudiantes                           |
| 2 Tasa de Retención                                | Tasa de Retención y Graduación                      |
| 6 Capital de trabajo                               | Capital de Trabajo                                  |
| 7 Sueldos                                          | Sueldos Carrera                                     |
| 8 Mantenimiento                                    | Mantenimiento e Inversión → Mantenimiento           |
| Activos fijos y depreciación                       | Recursos y Depreciación                             |
| Activos diferidos / amortización                   | Mantenimiento e Inversión → Activos Diferidos       |
| Inversión inicial                                  | Mantenimiento e Inversión → Inversión Inicial       |
| 5 Demanda / 9 Inv. Vin. Becas                      | Demanda e Ingresos / Costos y Gastos                |
| 10 Costos y Gastos                                 | Costos y Gastos                                     |
| 11 Costo de la Carrera                             | Costos y Gastos → Costo de la carrera (referencial) |
| 12 P&G, 13 TIR, 14 VAN, 15 P. Equilibrio, 16 Flujo | Análisis Financiero                                 |
| INF CES y CES                                      | Análisis Financiero → pestaña CES / INF CES         |
| Amort. préstamo / Financiamiento                   | Amortización                                        |
| Balance                                            | Análisis Financiero → Balance Proyectado            |

## Reglas de negocio clave

- **Terminología**: "arancel" = lo que se **cobra**; "Costo por Semestre" = costo
  referencial. El cuadro CES reporta el **arancel óptimo (VAN ≈ 0)** y muestra el costo
  referencial al lado.
- **Metas de retención/graduación**: el usuario ingresa la **meta acumulada** (ej. 65 %
  a mitad de carrera); la tasa por ciclo que alimenta los cálculos se deriva como
  `meta^(1/pasos)` y se muestra como informativa.
- **Modo de cálculo financiero**: "Compatible Excel" (flujos semestrales, convención
  `=VAN()` de Excel, sin recuperación de capital de trabajo) para validar contra la
  matriz institucional.
- **Becas**: descuento de ingreso, nunca costo (evita doble conteo). Las becas de
  gobierno son 0 por diseño (no hay aporte estatal).
- **TIR**: con múltiples raíces se reporta la más cercana a la TMR.
- **Punto de equilibrio**: se calcula con el último período proyectado.
- **Arancel óptimo**: bisección con tolerancia, rango e iteraciones configurables en
  Datos Institucionales; estados de convergencia explícitos.
- **Impuestos**: participación trabajadores (15 %) + impuesto renta (25 %) con toggle en
  Datos Institucionales (OFF = igual al Excel).
- **Docentes**: contratación entera con Medio Tiempo (12 h) y Tiempo Parcial para el
  residuo; difiere deliberadamente del Excel (FTE fraccional) y está documentado en la
  validación.

## Pruebas

```powershell
dotnet test
```

- `tests/Application.Tests` — reglas de proyección, docentes, inflación, sueldos,
  recursos, demanda/aranceles/materiales, costos y cálculos financieros,
  derivación meta→tasa de retención.
- `tests/Presentation.Tests` — ViewModels.

Última verificación: **build 0 warnings / 0 errores; 153 pruebas pasando** (151 + 2).

## Documentación adicional

| Documento                                    | Contenido                                                                                         |
| -------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| [docs/GUIA_USUARIO.md](docs/GUIA_USUARIO.md) | **Guía de usuario completa**: flujo de 9 pasos botón por botón, FAQ y glosario.                   |
| [docs/DISTRIBUCION.md](docs/DISTRIBUCION.md) | Requisitos, pasos y diagnóstico de la distribución del ejecutable.                                |
| `docs/Diagramas de Clase Dominio puml/`      | Diagramas de clases del dominio (PlantUML; fuente de las Figuras 46/47 y el Anexo A de la tesis). |
| `docs/diagramas/bd-er/`                      | Diagramas entidad-relación.                                                                       |
| `docs/diagramas/secuencia/`                  | Diagramas de secuencia.                                                                           |
| `docs/casos-de-uso/`                         | Especificaciones de casos de uso por módulo.                                                      |
| `sql/`                                       | Scripts SQL idempotentes, en orden por ticket (KAN-\*).                                           |
