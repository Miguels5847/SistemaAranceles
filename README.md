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
- Entity Framework Core + SQLite
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
- KAN-03 Diseno de base de datos y migraciones: pendiente.
- KAN-04 Entidades de dominio base: pendiente.
- KAN-05 DbContext y repositorio generico: pendiente.

## Roadmap inmediato

1. KAN-03: definir modelo de datos relacional y primera migracion.
2. KAN-04: modelar entidades nucleares del dominio (Usuario, Rol, Carrera, Inflacion, Retencion).
3. KAN-05: implementar DbContext, contratos de repositorio y primera version de acceso a datos.
4. Iniciar casos de uso del modulo de Usuarios con validaciones y trazabilidad RF a implementacion.

## Estrategia de ramas

Se aplicara una estrategia GitFlow simple:

- main: rama estable.
- develop: rama de integracion.
- feature/KAN-xx: desarrollo por historia tecnica.

Cada avance se integra primero en develop y posteriormente en main cuando cumpla los criterios de cierre del hito.
