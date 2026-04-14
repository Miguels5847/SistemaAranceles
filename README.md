# SistemaAranceles

## Descripcion del proyecto

SistemaAranceles es un proyecto de tesis orientado al desarrollo de una aplicacion de escritorio para la simulacion y analisis de aranceles en procesos de apertura de nuevas carreras.

El objetivo tecnico es construir una solucion mantenible y escalable, con separacion de responsabilidades entre capas, trazabilidad respecto a requerimientos funcionales y capacidad de validacion progresiva por modulos.

## Arquitectura aplicada

Se adopta una Clean Architecture simplificada para entorno WPF, con las siguientes capas:

- Domain: entidades, reglas de negocio puras, value objects, enums e interfaces del dominio.
- Application: casos de uso, DTOs, contratos de servicios y validaciones.
- Infrastructure: persistencia con Entity Framework Core, repositorios, exportaciones y adaptadores externos.
- Presentation: interfaz WPF con patron MVVM (Views, ViewModels y Commands).

Esta estructura permite aislar la logica de negocio del framework de interfaz y de los detalles de infraestructura.

## Stack tecnologico

- .NET 8
- WPF
- Entity Framework Core + PostgreSQL (Supabase)
- CommunityToolkit.Mvvm
- FluentValidation
- BCrypt.Net-Next
- ClosedXML
- QuestPDF

## Estado actual

Fase 1 (requerimientos funcionales): completada.

Epic 1:

- KAN-01 Crear solucion WPF + 4 proyectos Clean Architecture: completado.
- KAN-02 Instalar paquetes base de la solucion: completado.
- KAN-03 Diseno de base de datos y migraciones: completado.
- KAN-04 Entidades de dominio base: completado.
- KAN-05 DbContext y repositorio generico: completado.

Epic 2:

- KAN-06 CRUD Usuarios: implementado y separado en rama `feature/KAN-06-CRUD-Usuarios`.
- KAN-07 Login + BCrypt + sesion por rol: implementado y separado en rama `feature/KAN-07-login-sesion-rol`.
- KAN-08 Menu dinamico por rol + permisos: implementado y separado en rama `feature/KAN-08-menu-dinamico-permisos`.
- KAN-09 AuditLog acciones criticas: implementado y separado en rama `feature/KAN-09-auditlog-acciones-criticas`.

Estado de cierre Epic 2:

- Epic 2 cerrada funcionalmente (KAN-06 a KAN-09 completados).
- Consulta de auditoria estable y optimizada para entorno Supabase.
- Informe consolidado: `docs/EPICA-2-Informe-KAN06-KAN09.md`.

Notas tecnicas recientes:

- Se aplicaron scripts de soporte para Supabase y sincronizacion de migraciones EF Core.
- Se ejecuto seed inicial de roles, permisos y usuario administrador.
- Se corrigio la rehidratacion de Id en creacion de usuario antes de asignar rol.

Epic 3:

- KAN-10 CRUD Inflacion + validacion (RF-IN-01/RF-IN-02): implementado.
- KAN-11 Proyeccion y grafico de inflacion: implementado con metodo configurable.
- KAN-12 Solo lectura para modulos dependientes + endurecimiento RBAC: implementado.

Estado de cierre parcial Epic 3:

- Se separaron ramas y commits por KAN para trazabilidad.
- Proyeccion de inflacion configurada por opcion (`regresion-lineal` por defecto, `promedio-suave` opcional).
- Se mantiene soporte de ajuste manual sobre valores proyectados.
- Se reforzo el guardado de rol/permisos para evitar perdida de rol al editar usuarios.
- Se restringio auditoria a Administrador con permiso `AUD.VER`.
- Se corrigio el contador de sesion por inactividad para evitar que quede congelado.
- Se agregaron placeholders de modulos en desarrollo para pruebas de permisos por rol.

## Roadmap inmediato

1. Integrar modulos consumidores (Sueldos, Demanda, Mantenimiento, Costos y Gastos) para consumir directamente el contrato de solo lectura de inflacion.
2. Completar pantallas funcionales de modulos con placeholder actualmente habilitados por permisos (Carreras, Proyecciones, Analisis Financiero, Configuracion, Reportes).
3. Mantener integracion por historia tecnica en ramas `feature/KAN-xx` y merge secuencial hacia `develop`.

## Dependencias de inflacion (solo lectura)

La inflacion se administra en su propio modulo (KAN-10) y se expone para consumo en solo lectura en modulos dependientes.

```text
Inflacion (fuente unica de verdad)
	-> Sueldos
	-> Demanda
	-> Mantenimiento
	-> Costos y Gastos
```

Reglas:

- Los modulos consumidores no deben editar ni eliminar datos de inflacion.
- Las variaciones por modulo se gestionan como logica de calculo local, no como mutacion del dato base de inflacion.
- El use case de consumo para dependientes retorna datos marcados con `SoloLectura = true`.

## Estrategia de ramas

Se aplicara una estrategia GitFlow simple:

- main: rama estable.
- develop: rama de integracion.
- feature/KAN-xx: desarrollo por historia tecnica.

Cada avance se integra primero en develop y posteriormente en main cuando cumpla los criterios de cierre del hito.

# Restore

## Backup (para un solo archivo restaurable):

Formato: Custom
Filename: public_full.backup
Dump options #1:
Only schema: No
Only data: No
Include blobs: No (si no usas blobs)
Dump options #2:
Use INSERT commands: opcional
No owner: Yes
No privileges: Yes
Objects:
Schema: solo public
No incluir realtime, storage, auth, extensions internas

## Restore (ese mismo archivo):

Archivo: public_full.backup
Pre-data: Yes
Data: Yes
Post-data: Yes
No owner: Yes
No privileges: Yes
Clean before restore: solo en entorno de prueba, no en producción
