# Conexion de SistemaAranceles a Supabase (PostgreSQL)

## Objetivo

Conectar el proyecto a una base de datos PostgreSQL en Supabase manteniendo la arquitectura limpia.

## Decision tecnica

- Proveedor de base de datos: PostgreSQL.
- Plataforma: Supabase.
- Estrategia inicial: aplicacion de escritorio .NET conectada directamente a BD.
- Evolucion futura opcional: capa API intermedia sin cambiar logica de negocio.

## Cambios implementados en el proyecto

1. Se agrego el proveedor EF Core para PostgreSQL en infraestructura.
2. La fabrica de contexto en tiempo de diseno ahora:

- usa `SUPABASE_DB_CONNECTION` si existe,
- usa SQLite local como respaldo si la variable no esta configurada.

## Configuracion local (PowerShell)

1. Definir cadena de conexion en variable de entorno de usuario:

```powershell
setx SUPABASE_DB_CONNECTION "Host=db.zpdkdbonmsjqljozaczp.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=TU_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
```

2. Cerrar y abrir la terminal para que tome la nueva variable.

3. Verificar que la variable existe:

```powershell
[Environment]::GetEnvironmentVariable("SUPABASE_DB_CONNECTION", "User")
```

## Aplicar migraciones a Supabase

Ejecutar desde la raiz del repo:

```powershell
dotnet ef database update --project src/Infrastructure/SistemaAranceles.Infrastructure.csproj --startup-project src/Presentation/SistemaAranceles.Presentation.csproj
```

## Validacion rapida

- Si la variable `SUPABASE_DB_CONNECTION` esta definida, EF Core usa PostgreSQL.
- Si no esta definida, EF Core usa `sistema_aranceles.db` (SQLite local).

## Seguridad minima recomendada

- No guardar la password en codigo fuente.
- No publicar la cadena completa en repositorio ni tesis.
- Rotar credenciales si se compartieron por chat o documentos.
- Usar usuario de BD con privilegios minimos para la aplicacion.

## Texto sugerido para tesis (resumen)

"La persistencia se implementa en la capa de Infraestructura con Entity Framework Core, utilizando PostgreSQL en Supabase como motor de datos. La arquitectura mantiene desacoplamiento mediante repositorios y unidad de trabajo, permitiendo que la logica de dominio y aplicacion permanezca independiente del proveedor de base de datos. Como estrategia de despliegue inicial, la aplicacion de escritorio realiza conexion directa a la base de datos; no obstante, la arquitectura permite evolucionar a una API intermedia sin cambios en las reglas de negocio."
