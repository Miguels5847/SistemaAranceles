# SistemaAranceles

Aplicación de escritorio WPF para simulación y análisis de aranceles en apertura de nuevas carreras universitarias. Proyecto de tesis con arquitectura limpia, persistencia en PostgreSQL (Supabase) y RBAC granular.

## Stack

- **.NET 8** + **WPF** (MVVM)
- **EF Core** + **PostgreSQL (Supabase pooler 6543)**
- **CommunityToolkit.Mvvm** · **FluentValidation** · **BCrypt.Net-Next**
- **ClosedXML** (XLSX) · **QuestPDF** (PDF)

## Arquitectura

Clean Architecture 4 capas:

```
Domain          ← Entidades, VOs, enums, contratos puros
Application     ← Use cases (1 archivo = 1 caso), DTOs, interfaces servicios
Infrastructure  ← EF Core, repositorios, servicios externos (BCE, hash, auditoría)
Presentation    ← WPF Views/ViewModels/State (SesionActual)
```

## Estado por épica

| # | Épica | KANs | Estado |
|---|---|---|---|
| 1 | Setup & Arquitectura | 01–05 | ✅ |
| 2 | Usuarios (CRUD + Login + Menú dinámico + AuditLog) | 06–09 | ✅ |
| 3 | Inflación (CRUD + Proyección + Solo lectura) | 10–12 | ✅ |
| — | Infra/RBAC/QA (KAN-13/14, multi-fase) | 13–14 | 🟡 Fase 6 en curso |
| 4 | Tasa Retención | 13–16* | ⏳ |
| 5–13 | Estudiantes / Sueldos / Recursos / … / Reportes | 17–49 | ⏳ |
| 14 | Cierre & Validación | 50–53 | ⏳ |

\* KANs 13/14 originales (Épica 4) renombrados internamente; los KAN-13/14 ejecutados son tareas de infraestructura.

## Funcionalidades implementadas

- Login con BCrypt + retry/timeout + toggle visibilidad contraseña
- Sesión por inactividad (30 min con countdown)
- CRUD usuarios (soft/hard delete, validación correo único, hash)
- RBAC con `rol`, `permiso`, `rol_permiso` y `usuario_permiso_override` por usuario
- Permisos efectivos en memoria (`SesionActual.TienePermiso("MOD.ACCION")`)
- Auditoría fire-and-forget desacoplada (no bloquea ni revierte)
- Inflación: 9 use cases (CRUD anual + proyectar + importar BCE + limpiar)
- Métodos proyección: regresión lineal (default) y promedio suave
- RLS progresivo (Admin bypass; Analista/Visualizador restringidos)

## Estructura del repo

```
src/
  Domain/         Entities/  ValueObjects/  Enums/  Interfaces/  Common/
  Application/    UseCases/  DTOs/  Interfaces/
  Infrastructure/ Persistence/  Servicios/  Export/  DI/
  Presentation/   Views/  ViewModels/  Converters/  State/  Mensajes/  Services/
sql/              # Scripts versionados KAN03..KAN14 (precheck/apply/postcheck/rollback)
docs/             # Informes épicas + DB
```

## Quick start

```bash
# Compilar
dotnet build src/Presentation/SistemaAranceles.Presentation.csproj

# Ejecutar
dotnet run --project src/Presentation/SistemaAranceles.Presentation.csproj
```

Configurar `src/Presentation/appsettings.Local.json` con cadena Supabase (`Maximum Pool Size=10`, `Minimum Pool Size=2`).

## GitFlow

- `main` — estable
- `develop` — integración
- `feature/KAN-xx` — desarrollo por historia técnica → merge a `develop`

## Documentación

- **`contexto.md`** — snapshot maestro (leer primero en cualquier chat nuevo)
- **`Diagramas.md`** — diagnóstico BD vs diagramas (hallazgos H1–H7)
- **`ANALISIS_COMPLETO_RENDIMIENTO.md`** — fixes críticos rendimiento
- **`docs/EPICA-2-Informe-KAN06-KAN09.md`** — cierre Épica 2
- **`docs/conexion-supabase-postgresql.md`** — guía conexión

### Diagramas (`src/Application/UseCases/`)

- `BD ER/BD-01..BD-03B.puml` — ER tablas core/académico/financiero
- `Diagramas de Clase Dominio/DC-01..DC-04.puml` (DC-01 dividido en 4, DC-02.1 corregido)
- `Diagramas de Secuencia/DS-01..DS-11.puml` (DS-02 dividido en 3, DS-03 dividido en 4)

## Optimizaciones aplicadas

- DbContext **Scoped** + scope manual en VMs async → evita pool exhausted
- INSERT booleanos con `TRUE` literal (no `1`) → fix SqlState 42804
- Batch loading `ANY(@ids)` → elimina N+1 en listado usuarios+roles
- `RevocarAsync` reutiliza conexión del scope → respeta pool
- Timeouts realistas con pooler: roles 15s · session 8s · login 25s
- Precarga automática lista usuarios si Admin

## Restricciones de trabajo

- ❌ Sin commit automático
- ❌ No romper login ni degradar tiempos
- ❌ No introducir N+1
- ❌ No skip hooks (`--no-verify`) sin pedir
- ✅ Mantener Clean Architecture + 1 caso por archivo
- ✅ Confirmar antes de destructivos (drop, force-push, delete branch)

## Restore Supabase (referencia)

**Backup:** Custom · Solo schema `public` · No owner · No privileges · Sin realtime/storage/auth/extensions internas
**Restore:** Pre-data + Data + Post-data · No owner · No privileges · `Clean before restore` solo en pruebas
