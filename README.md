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

## Roadmap inmediato

1. Iniciar Epic 3 con KAN-10: CRUD Inflacion + validacion (RF-IN-01/02), prioridad tier-1 backend/frontend.
2. Tomar como base de desarrollo todo lo consolidado en `develop` antes de implementar KAN-10.
3. Mantener integracion por historia tecnica en ramas `feature/KAN-xx` y merge secuencial hacia `develop`.

## Estrategia de ramas

Se aplicara una estrategia GitFlow simple:

- main: rama estable.
- develop: rama de integracion.
- feature/KAN-xx: desarrollo por historia tecnica.

Cada avance se integra primero en develop y posteriormente en main cuando cumpla los criterios de cierre del hito.
