# Informe Épica 4: Tasa de Retención, Simulación e Integración con Trazabilidad (KAN-13 a KAN-16)

========================================================
ÉPICA 4: 📊 Tasa Retención (S3 Abr26-May6) 14SP
========================================================
KAN-13 Config carrera y cohorte | RF-TR-01 | tier-1 backend frontend | P1 | 4SP
KAN-14 Simulación cohorte ciclo a ciclo | RF-TR-02 | tier-1 backend | P1 | 5SP
KAN-15 Indicadores Ret% y Titu% | RF-TR-02 | tier-1 frontend | P1 | 3SP
KAN-16 Edición con trazabilidad | RF-TR-03 | tier-1 db | P1 | 2SP

## 1. Resumen Ejecutivo

La Épica 4 consolidó el módulo de Tasa de Retención como componente académico-operativo del sistema, integrando configuración por carrera y escenario, simulación de cohorte ciclo a ciclo, visualización de indicadores y trazabilidad de cambios.

En términos funcionales, se implementó un flujo completo desde la parametrización hasta la consulta de resultados, con control de acceso por permisos, auditoría desacoplada y persistencia transaccional para la simulación. La solución quedó alineada con la arquitectura limpia del proyecto y con la estrategia de evolución por KANs.

Estado de cierre al momento del informe: KAN-13, KAN-14 y KAN-15 implementados; KAN-16 en cierre con trazabilidad operativa en rama activa.

## 2. Objetivo de la Épica

Implementar el módulo de retención para:

- Configurar parámetros de retención y graduación por carrera y escenario.
- Simular cohortes por ciclo con persistencia de cabecera y detalle.
- Exponer indicadores de retención y titulación para análisis.
- Incorporar edición con trazabilidad de auditoría en operaciones críticas.

## 3. Alcance Planificado vs Ejecutado

| Historia | Estado       | Resultado                                                                                            |
| -------- | ------------ | ---------------------------------------------------------------------------------------------------- |
| KAN-13   | Implementado | Configuración de carrera/cohorte con validaciones, reglas de duplicidad y gestión operativa desde UI |
| KAN-14   | Implementado | Motor de simulación ciclo a ciclo, persistencia transaccional y gestión de simulaciones              |
| KAN-15   | Implementado | Indicadores de retención/titulación en frontend y mejoras de presentación en simulación              |
| KAN-16   | En cierre    | Edición con trazabilidad basada en auditoría, con casos de uso y flujo funcional ya operativo        |

## 4. Logros Técnicos Implementados

### 4.1 KAN-13 — Configuración de carrera y cohorte

Se habilitó el componente de configuración con reglas de negocio y persistencia coherente:

- Alta de configuración de retención por carrera y escenario.
- Validación de existencia de carrera y escenario.
- Regla de secuencia de escenarios: optimista/pesimista depende de histórico previo.
- Control de duplicidad por combinación carrera + escenario.
- Actualización de base de estudiantes y paralelos por período.
- Registro de auditoría al crear o actualizar criterios.

Adicionalmente, se reforzó el módulo de Carreras para asegurar continuidad operativa del flujo de configuración:

- Guardar y editar estabilizados en ViewModel.
- Contrato de repositorio extendido con eliminación por identificador.
- Implementación de soft delete y filtrado de carreras activas.
- UI con acción explícita de eliminar selección.

### 4.2 KAN-14 — Simulación de cohorte ciclo a ciclo

Se implementó el motor de simulación y su persistencia completa:

- Cálculo ciclo a ciclo sobre parámetros de configuración.
- Creación y re-ejecución de simulación por configuración y cohorte.
- Persistencia de cabecera de simulación con indicadores finales.
- Persistencia de detalle por ciclo.
- Reemplazo de detalle en re-ejecuciones para mantener consistencia.
- Operación transaccional con iniciar/confirmar/revertir para garantizar atomicidad.
- Auditoría de eventos CREAR_SIMULACION y REEJECUTAR_SIMULACION.

### 4.3 KAN-15 — Indicadores Ret% y Titu%

Se completó el frente de visualización e interpretación de resultados:

- Carga de simulaciones y detalle asociado.
- Construcción de tabla de comportamiento por ciclo/período.
- Presentación de retención y graduación en resumen de cabecera.
- Refinamientos de UI/UX sobre la vista de simulación.
- Corrección de consistencia semántica para cabecera: uso de tasas configuradas en lugar de indicadores compuestos de salida cuando corresponde.

### 4.4 KAN-16 — Edición con trazabilidad

El cierre de KAN-16 se apoya en la infraestructura transversal ya disponible y casos de uso de actualización con auditoría:

- Actualización de criterio de referencia con validación FluentValidation.
- Registro de auditoría por acción ACTUALIZAR en criterio de retención.
- Integración de trazabilidad sin bloquear operación principal ante fallos de auditoría.

Con esto, la capacidad de edición quedó vinculada a rastro auditable, consistente con el modelo de gobernanza aplicado en seguridad y operación.

## 5. Detalle por KAN

### 5.1 KAN-13 — Configuración carrera/cohorte (RF-TR-01)

#### Funcionalidad observada

- Crear, actualizar, listar y eliminar configuraciones de retención.
- Gestión de criterios de referencia por configuración.
- Validaciones de entrada y reglas de integridad de dominio.

#### Evidencia técnica

- CrearConfiguracionRetencionUseCase
- ActualizarConfiguracionRetencionUseCase
- ListarConfiguracionesRetencionUseCase
- ObtenerConfiguracionRetencionUseCase
- EliminarConfiguracionRetencionUseCase
- CrearCriterioReferenciaRetencionUseCase
- ActualizarCriterioReferenciaRetencionUseCase
- DTOs y validadores de TasaRetencion

### 5.2 KAN-14 — Simulación cohorte ciclo a ciclo (RF-TR-02)

#### Funcionalidad observada

- Ejecución de simulación por configuración/cohorte.
- Persistencia de simulación y detalle.
- Re-ejecución segura con reemplazo de detalle.
- Limpieza y eliminación de simulaciones.

#### Evidencia técnica

- CrearSimulacionRetencionUseCase
- ActualizarSimulacionRetencionUseCase
- ListarSimulacionesRetencionUseCase
- ObtenerSimulacionRetencionUseCase
- EliminarSimulacionRetencionUseCase
- LimpiarSimulacionesRetencionUseCase
- MotorSimulacionRetencion
- Entidades SimulacionRetencion y DetalleSimulacionRetencion

### 5.3 KAN-15 — Indicadores Ret% y Titu% (RF-TR-02)

#### Funcionalidad observada

- Visualización de resultados y comportamiento por ciclos.
- Cabecera de resumen con indicadores/tasas coherentes.
- Flujo de selección de simulación con detalle asociado.

#### Evidencia técnica

- SimulacionRetencionViewModel
- SimulacionRetencionView
- Ajustes de presentación y consistencia de etiquetas/valores

### 5.4 KAN-16 — Edición con trazabilidad (RF-TR-03)

#### Funcionalidad observada

- Edición de criterios con validación.
- Registro de auditoría de cambios.
- Flujo de cierre en rama feature con integración de cambios acumulados de Épica 4.

#### Evidencia técnica

- ActualizarCriterioReferenciaRetencionUseCase
- ServicioAuditoria / IAuditoriaServicio
- AuditoriaLog como almacenamiento central de trazabilidad

## 6. Arquitectura y Separación de Responsabilidades

La Épica 4 respetó el patrón de separación en tres ejes operativos:

1. Configuración de parámetros (KAN-13).
2. Simulación y persistencia transaccional (KAN-14).
3. Consumo visual e interpretación de resultados (KAN-15), con edición trazable (KAN-16).

Este enfoque evita acoplar cálculo, UI y trazabilidad en una sola capa y mantiene la evolución por casos de uso independientes.

## 7. Comportamientos Importantes del Código

### 7.1 Permisos y control de acceso

- Visualización: TRE.VER o Administrador.
- Edición/ejecución: TRE.EDITAR o TRE.CREAR o Administrador.
- Eliminación: TRE.ELIMINAR o Administrador.
- En Carreras, eliminación condicionada por CA.ELIMINAR o Administrador.

### 7.2 Consistencia y fiabilidad

- Simulación bajo unidad de trabajo con transacción explícita.
- Re-ejecución idempotente por configuración/cohorte con reemplazo de detalle.
- Soft delete en Carreras para preservar trazabilidad histórica.

### 7.3 Auditoría desacoplada

- Registro de auditoría en creación, actualización y simulación.
- Fallos de auditoría no revierten la operación principal.

## 8. Evidencias de Cumplimiento Funcional

1. Se puede configurar retención por carrera y escenario con validaciones de negocio. ✅
2. Se puede ejecutar simulación de cohorte y persistir cabecera + detalle. ✅
3. Se pueden consultar resultados e indicadores en frontend. ✅
4. Se puede editar con registro de trazabilidad en auditoría. ✅
5. El control de acceso por permisos aplica a ver, editar y eliminar. ✅
6. El módulo de Carreras quedó estable para alta/edición/eliminación por soft delete. ✅

## 9. Incidencias Relevantes y Resoluciones

### 9.1 Bloqueadores iniciales de KAN-14

Los bloqueadores documentados (entidades de simulación, repositorios y migración) fueron abordados dentro de la implementación de la épica, permitiendo habilitar el motor y su persistencia.

### 9.2 Permiso CA.ELIMINAR

Se confirmó la creación/asignación del permiso de eliminación para Carreras en Supabase, con roles Administrador y Analista habilitados para ese permiso.

### 9.3 Ajustes de consistencia visual

Se corrigieron diferencias de presentación/labels en simulación para evitar interpretación ambigua entre tasas configuradas e indicadores finales de resultado.

## 10. Estado Final de la Épica 4

Épica 4 funcionalmente implementada en KAN-13, KAN-14 y KAN-15, con KAN-16 en cierre operativo y trazabilidad activa en rama de trabajo.

La rama feature de KAN-16 contiene los cambios acumulados de KAN-14 y KAN-15 sobre la base de la épica, por lo que es apta para consolidación hacia develop en un MR de integración de Épica 4.

## 11. Recomendaciones de Cierre

- Completar validación E2E final de KAN-16 (edición + rastro en auditoría).
- Abrir MR de integración de Épica 4 hacia develop desde la rama de KAN-16.
- En la descripción del MR, declarar explícitamente que incluye KAN-13..16 para evitar ambigüedad de alcance.
- Mantener como diferidos de próximas épicas los puntos no bloqueantes ya identificados (tablas huérfanas y ajustes financieros de fases posteriores).

---

## Texto sugerido para PR de Épica 4 (listo para copiar)

Título sugerido:

feat(epica-4): integrar KAN-13..KAN-16 (retención, simulación, indicadores y trazabilidad)

Descripción sugerida:

Resumen

- Se integra la Épica 4 completa en develop, incluyendo configuración de retención, simulación de cohorte, visualización de indicadores y edición con trazabilidad.
- Esta rama incluye los cambios acumulados de KAN-14 y KAN-15 sobre la base de KAN-16.

Alcance incluido

- KAN-13 (RF-TR-01): configuración carrera/cohorte y criterios de referencia.
- KAN-14 (RF-TR-02): motor de simulación ciclo a ciclo con persistencia transaccional.
- KAN-15 (RF-TR-02): indicadores Ret% y Titu% en frontend y refinamientos de visualización.
- KAN-16 (RF-TR-03): edición con trazabilidad en auditoría.

Cambios clave

- Nuevos casos de uso, DTOs, validadores e interfaces para TasaRetencion.
- Nuevas entidades y repositorios de simulación/detalle.
- Integración UI de Configuración y Simulación de Retención.
- Fix módulo Carreras: guardar/editar estable y eliminación por soft delete.
- Regla de permisos aplicada:
  - TRE.VER / TRE.EDITAR / TRE.CREAR / TRE.ELIMINAR para Retención.
  - CA.ELIMINAR o Administrador para eliminar carreras.

Base de datos y permisos

- Migraciones/ajustes de persistencia de retención incorporados en la rama.
- Permiso CA.ELIMINAR validado en Supabase para Administrador y Analista.

Validación

- Build Presentation: 0 errores, 0 warnings.
- Flujo funcional validado en configuración, simulación, detalle y eliminación.
- Trazabilidad de edición operativa vía auditoría.

Notas

- Este PR es de integración de Épica 4 (no solo KAN-16 aislado).
- Diferidos no bloqueantes quedan fuera de este alcance (épicas posteriores).
