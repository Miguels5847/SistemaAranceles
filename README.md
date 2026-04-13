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

Notas tecnicas recientes:

- Se aplicaron scripts de soporte para Supabase y sincronizacion de migraciones EF Core.
- Se ejecuto seed inicial de roles, permisos y usuario administrador.
- Se corrigio la rehidratacion de Id en creacion de usuario antes de asignar rol.

## Roadmap inmediato

1. Publicar ramas de Epic 2 a origin y abrir PRs secuenciales hacia develop (KAN-06 -> KAN-07 -> KAN-08 -> KAN-09).
2. Validar pruebas funcionales de usuarios/login/menu/auditoria sobre base Supabase.
3. Cerrar merges de Epic 2 y preparar inicio de Epic 3.

## Estrategia de ramas

Se aplicara una estrategia GitFlow simple:

- main: rama estable.
- develop: rama de integracion.
- feature/KAN-xx: desarrollo por historia tecnica.

Cada avance se integra primero en develop y posteriormente en main cuando cumpla los criterios de cierre del hito.
