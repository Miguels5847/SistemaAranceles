# Informe Épica 3: Inflación, Proyección y Consumo en Solo Lectura (KAN-10 a KAN-12)

## 1. Resumen Ejecutivo

La Épica 3 quedó completada como la base financiera de proyección del sistema. Se implementó el ciclo completo de inflación anual, su mantenimiento manual y por importación, la proyección automática con dos métodos configurables, y la entrega de inflación como fuente de solo lectura para módulos dependientes.

El resultado funcional es estable y coherente con el resto de la arquitectura: la inflación es editable solo por usuarios autorizados, los módulos consumidores no mutan datos y la lógica de negocio quedó separada entre captura, proyección y lectura auxiliar.

## 2. Objetivo de la Épica

Implementar el módulo de inflación para:

- Registrar inflación anual histórica o estimada.
- Editar y eliminar registros de inflación.
- Importar datos desde archivo Excel y desde el BCE.
- Proyectar inflación futura con método configurable.
- Exponer inflación proyectada en modo solo lectura para módulos dependientes.

## 3. Alcance Planificado vs Ejecutado

| Historia | Estado     | Resultado                                                                                  |
| -------- | ---------- | ------------------------------------------------------------------------------------------ |
| KAN-10   | Completado | CRUD de inflación, validación, importación manual y BCE, limpieza y mantenimiento de datos |
| KAN-11   | Completado | Proyección por regresión lineal y promedio suave, con gráfico en UI                        |
| KAN-12   | Completado | Consumo de inflación proyectada en modo solo lectura para módulos dependientes             |

## 4. Logros Técnicos Implementados

### 4.1 CRUD de inflación anual (KAN-10)

Se implementó el ciclo completo de mantenimiento del catálogo de inflación:

- Crear registros anuales.
- Editar registros existentes.
- Eliminar registros seleccionados.
- Listar y seleccionar registros desde la UI.

El ViewModel de inflación soporta edición directa con validación de entrada, reconsulta automática después de guardar y selección del registro recién creado o actualizado.

### 4.2 Tipificación funcional de fuentes

Los registros de inflación trabajan con tres tipos de fuente funcionales:

- `Dato historico`
- `Estimacion`
- `Ajuste manual`

Esto permite separar claramente lo que viene de datos observados, lo que proviene de cálculos proyectados y lo que fue corregido manualmente por el usuario.

### 4.3 Importación manual y BCE

Se incorporaron dos flujos de importación:

- Importación manual desde archivo Excel.
- Importación desde archivo BCE con tipo de serie configurado.

Ambas rutas actualizan la colección visible en UI, resumen mensajes de resultado y muestran errores detallados sin bloquear la navegación.

### 4.4 Proyección de inflación (KAN-11)

La proyección soporta dos métodos:

- `regresion-lineal` como método por defecto.
- `promedio-suave` como alternativa configurable.

La proyección toma datos históricos, aplica el método configurado y luego persiste o actualiza solo los años proyectados, respetando registros históricos y ajustes manuales.

### 4.5 Solo lectura para dependientes (KAN-12)

Se agregó un use case explícito para entregar inflación a módulos consumidores sin habilitar mutaciones. Ese contrato devuelve un DTO marcado como solo lectura y ordenado por año.

Este diseño evita que módulos operativos como costos, sueldos o demandas escriban sobre la tabla de inflación.

## 5. Detalle por KAN

### 5.1 KAN-10 — CRUD inflación + validación

#### Funcionalidad observada

- Alta, edición y eliminación de registros de inflación anual.
- Validación de año y porcentaje.
- Soporte para fuente y tipo de fuente.
- Mensajería de éxito/error en la vista.
- Relectura de datos tras guardar o eliminar.

#### Comportamiento clave

- Si se intenta guardar un año ya existente, el sistema carga el registro existente para edición.
- El formulario se limpia después de guardar o eliminar.
- La vista mantiene estilos consistentes para filas y selección.

#### Evidencia técnica

- `CrearInflacionAnualUseCase`
- `ActualizarInflacionAnualUseCase`
- `EliminarInflacionAnualUseCase`
- `ListarInflacionAnualUseCase`
- `InflacionViewModel`
- `InflacionView.xaml`

### 5.2 KAN-11 — Regresión lineal + gráfico

#### Funcionalidad observada

- Proyección de inflación mediante regresión lineal.
- Alternativa de promedio suave.
- Gráfico de inflación anual en la UI con OxyPlot.
- Resumen textual de proyección ejecutada.

#### Comportamiento clave

- La proyección exige un mínimo de datos históricos para funcionar.
- Si el método configurado es promedio suave, el motor aplica ese camino; si no, usa regresión lineal.
- Los años históricos y los ajustes manuales se preservan.
- El gráfico se reconstruye al cargar o modificar datos.

#### Evidencia técnica

- `ProyectarInflacionUseCase`
- `InflacionOpciones`
- `InflacionViewModel`
- `InflacionView.xaml`
- `IServicioProyeccion`

### 5.3 KAN-12 — Solo lectura módulos dependientes

#### Funcionalidad observada

- Entrega de inflación proyectada a módulos consumidores.
- DTO de lectura con bandera `SoloLectura = true`.
- Orden por año.
- No expone operaciones de escritura.

#### Evidencia técnica

- `ObtenerInflacionProyectadaParaDependientesUseCase`
- `InflacionDependienteLecturaDto`

## 6. Arquitectura y Separación de Responsabilidades

La Épica 3 quedó organizada en tres capas de responsabilidad:

1. **Captura y mantenimiento**: CRUD manual de inflación.
2. **Proyección**: cálculo automático con configuración de método.
3. **Consumo**: lectura segura para otros módulos.

Esto evita mezclar proyección con edición y evita que los módulos dependientes repitan lógica financiera local.

## 7. Comportamientos Importantes del Código

### 7.1 Compatibilidad de permisos

Durante la transición del permiso de edición de inflación, el ViewModel mantiene compatibilidad temporal con:

- `INF.EDITAR`
- `INF.ED`

La intención funcional es no cortar el acceso de edición mientras se consolida el permiso canónico.

### 7.2 Preservación de datos históricos

La proyección no sobreescribe:

- Datos históricos.
- Ajustes manuales.

Solo crea o actualiza proyecciones derivadas.

### 7.3 Auditoría desacoplada

Las operaciones críticas registran auditoría, pero la auditoría no bloquea ni revierte el caso de uso principal.

### 7.4 Validación y experiencia de usuario

- Validación de año y porcentaje antes de persistir.
- Mensajes separados para importación manual, importación BCE y proyección.
- Gráfico y tabla accesibles desde la misma vista.

## 8. Evidencias de Cumplimiento Funcional

1. Se pueden crear, editar y eliminar registros de inflación. ✅
2. Se pueden importar datos manualmente y desde BCE. ✅
3. Se puede proyectar inflación con método configurable. ✅
4. La proyección conserva datos históricos y ajustes manuales. ✅
5. Otros módulos consumen inflación en modo solo lectura. ✅
6. La interfaz muestra gráfico y tabla de soporte. ✅

## 9. Incidencias Relevantes y Consideraciones

### 9.1 Permiso `INF.ED`

Se identificó compatibilidad temporal entre `INF.ED` e `INF.EDITAR` para no romper la edición en instalaciones o datos previos.

### 9.2 Regla de negocio correcta

La Épica 3 es una base financiera de inflación. No debe usarse para costos, matrícula o proyección académica de estudiantes. Esa separación ya quedó respetada en la implementación y es importante mantenerla en las siguientes épicas.

## 10. Estado Final de la Épica 3

Épica 3 cerrada funcionalmente.

El sistema queda listo para consumir inflación proyectada desde otros módulos, con lectura controlada y sin acoplamiento innecesario a la escritura del catálogo.

## 11. Recomendaciones de Cierre

- Mantener `INF.EDITAR` como permiso canónico y conservar compatibilidad temporal mientras existan datos legados.
- No duplicar la lógica de proyección en otros módulos.
- Usar `ObtenerInflacionProyectadaParaDependientesUseCase` cuando un módulo requiera inflación sin mutación.
- Iniciar la siguiente épica con la misma separación entre dominio financiero y consumidores.
