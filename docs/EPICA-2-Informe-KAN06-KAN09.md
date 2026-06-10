# Informe Épica 2: Seguridad Operativa, Sesión, Permisos y Auditoría (KAN-06 a KAN-09)

## 1. Resumen Ejecutivo

La Épica 2 consolidó la operación funcional del sistema sobre la base arquitectónica de la Épica 1, incorporando autenticación robusta, control de sesión por inactividad, permisos efectivos por rol/usuario y auditoría consultable de acciones críticas.

Se completó la trazabilidad de eventos de seguridad y administración, con acceso restringido para consulta de auditoría y rendimiento validado en entorno real.

## 2. Objetivo de la Épica

Implementar los componentes transversales de seguridad y gobierno operativo necesarios para habilitar el resto de módulos del sistema:

- Inicio/cierre de sesión y gestión segura de sesión.
- Resolución de permisos efectivos por usuario.
- Registro de auditoría en acciones críticas.
- Consulta de auditoría con filtros y paginación para administrador.

## 3. Alcance Planificado vs Ejecutado

| Historia | Estado     | Resultado                                                                   |
| -------- | ---------- | --------------------------------------------------------------------------- |
| KAN-06   | Completado | Seguridad operativa y persistencia de sesión funcional                      |
| KAN-07   | Completado | Gestión de permisos efectivos por usuario y control por rol                 |
| KAN-08   | Completado | Registro de auditoría en eventos críticos                                   |
| KAN-09   | Completado | Consulta AuditLog con filtros, paginación, solo lectura y restricción admin |

## 4. Logros Técnicos Implementados

### 4.1 Autenticación y sesión

- Flujo de login operativo con validación de credenciales.
- Creación de sesión persistida en base de datos.
- Cierre de sesión manual y expiración por inactividad.
- Mensajería UI para estados de sesión y seguridad.

### 4.2 Permisos y autorización

- Carga de permisos efectivos del usuario autenticado.
- Restricción de acceso por permiso en navegación y acciones.
- Menú dinámico por rol/permisos.

### 4.3 Auditoría de eventos críticos

Se registran eventos críticos de seguridad y administración, incluyendo:

- `LOGIN_EXITOSO`
- `LOGOUT`
- `CREAR`, `ACTUALIZAR`, `ELIMINAR` (usuarios)
- `CAMBIO_ROL`
- `CAMBIO_PERMISOS`

### 4.4 Consulta AuditLog (KAN-09)

Se implementó módulo de consulta con:

- Filtros por usuario, rango de fechas, módulo y acción.
- Paginación con navegación y tamaño de página configurable.
- Vista de solo lectura (sin edición/eliminación).
- Acceso exclusivo para administrador con permiso `AUD.VER`.

## 5. Ajustes de Rendimiento y Estabilidad

Durante KAN-09 se resolvió una incidencia crítica de timeout en consulta de auditoría:

- Se diagnosticó el punto exacto de latencia en ejecución de lectura de datos.
- Se ajustó la estrategia de consulta en repositorio para evitar degradación.
- Se estabilizó la conectividad con Supabase usando pooler en modo sesión (puerto `5432`).
- Se corrigió normalización de cadena de conexión para respetar puerto explícito configurado.

Resultado validado en ejecución:

- Consultas de auditoría en tiempos aproximados de 90-110 ms.
- Carga de páginas de auditoría estable y sin bloqueos.

## 6. Base de Datos y Seguridad Operativa

- Permiso `AUD.VER` creado y asignado a rol Administrador.
- Índices de soporte para búsqueda/paginación de auditoría aplicados.
- Verificación de integridad y tipado de campos de auditoría completada.
- Confirmación de solo lectura para consumo de logs en UI.

## 7. Evidencias de Cumplimiento Funcional

Criterios de aceptación cubiertos:

1. Se registran eventos críticos de auditoría. ✅
2. Consulta de auditoría con filtros funcionales. ✅
3. Acceso restringido a administrador. ✅
4. Logs no editables ni eliminables desde UI. ✅
5. Paginación implementada. ✅
6. Respuesta rápida validada en entorno real. ✅

## 8. Incidencias Relevantes Resueltas

1. Timeout en lectura de AuditLog.

- Diagnóstico por trazas en capas ViewModel/Repositorio.
- Ajuste de consulta y conexión para eliminar bloqueo de lectura.

2. Diferencias entre configuración efectiva y configuración esperada de conexión.

- Se detectó normalización que forzaba puerto no deseado.
- Se aplicó corrección para respetar puerto explícito en configuración local.

3. Ajustes de UX en vista de auditoría.

- Formulario de filtros compacto.
- Listas desplegables para módulo y acción.
- Mejor experiencia de consulta y navegación.

## 9. Estado Final de la Épica 2

Épica 2 cerrada al 100% en objetivos funcionales y técnicos.

La solución queda lista para iniciar Épica 3 (Inflación), contando con:

- Seguridad y sesión operativa.
- Permisos y gobernanza de acceso por rol.
- Auditoría transversal lista para trazabilidad de cambios de negocio.
- Rendimiento de consulta validado en entorno de trabajo.

## 10. Recomendaciones de Cierre (MR)

- Incluir este informe como evidencia de cierre de Épica 2.
- Rotar credenciales si fueron expuestas en configuración local durante pruebas.
- Mantener `appsettings.Local.json` fuera de versionado en repositorio remoto.
- Iniciar Épica 3 con orden sugerido: KAN-10 → KAN-12 → KAN-11.
