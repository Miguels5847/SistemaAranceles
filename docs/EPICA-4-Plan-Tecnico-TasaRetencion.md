# Épica 4 — Tasa Retención: Plan Técnico Completo

**Versión:** 1.0  
**Fecha:** 2026-04-16  
**Estado:** 🚀 Listo para iniciar  
**Total SP:** 14 SP (KAN-13..16)

---

## 📋 Tabla de Contenidos

1. [Descripción general](#descripción-general)
2. [KANs: Desglose + dependencias](#kans-desglose--dependencias)
3. [Entidades de dominio + BD](#entidades-de-dominio--bd)
4. [Arquitectura y patrones](#arquitectura-y-patrones)
5. [Dependencias externas](#dependencias-externas)
6. [Bloqueadores conocidos](#bloqueadores-conocidos)
7. [Instrucciones para Claude Code](#instrucciones-para-claude-code)

---

## Descripción General

**Objetivo:** Implementar simulación de retención/graduación de estudiantes en cohortes, ciclo a ciclo, con cálculo de indicadores (Ret%, Titu%) y auditoría de cambios.

**Contexto:**

- Depende de: Épica 3 (Inflación) ✅ completada.
- Requiere: Datos de `Carrera`, `EscenarioProyeccion`, `InflacionProyectada`.
- Produce: Tablas `simulacion_retencion`, `detalle_simulacion_retencion`, `trazabilidad_retencion`.

**Fases:**

1. **KAN-13** (4 SP): Crear + editar configuración carrera/cohorte.
2. **KAN-14** (5 SP): Motor simulación (ciclo-a-ciclo) + BD.
3. **KAN-15** (3 SP): Visualización Ret% y Titu%.
4. **KAN-16** (2 SP): Trazabilidad ediciones + validaciones.

---

## KANs: Desglose + Dependencias

### **KAN-13 — Config Carrera y Cohorte** | 4 SP

**RF:** RF-TR-01  
**Tipo:** Backend + Frontend (WPF)  
**Prioridad:** P1 | **Criticidad:** Tier-1

#### Alcance

Crear + editar configuración de carrera y cohorte para simulación de retención.

#### User Stories

```
US-TR-01-01: Como Admin, quiero crear una configuración de retención
             para una carrera específica para simular proyecciones.

US-TR-01-02: Como Admin, quiero editar tasa de retención y tasas de referencia
             para ajustar modelos.

US-TR-01-03: Como Analista, quiero ver configuraciones existentes de carreras
             para seleccionar una para simulación.
```

#### Reqtos Funcionales

- **Crear Config Retención:**
  - Seleccionar carrera (dropdown activas).
  - Seleccionar escenario proyección (dropdown, default = escenario actual).
  - Ingresar total ciclos (int > 0, típicamente 8).
  - Ingresar tasa retención base % (decimal 0–100, default 85%).
  - Ingresar tasa graduación base % (decimal 0–100, default 90%).
  - Ingresar estudiantes período 1 (decimal ≥ 0, max 4 decimales).
  - Ingresar estudiantes período 2 (decimal ≥ 0, max 4 decimales).
  - Ingresar paralelos período 1 (int ≥ 0).
  - Ingresar paralelos período 2 (int ≥ 0).
  - Validación: No duplicar (carrera_id + escenario_id único).

- **Editar Config Retención:**
  - Modificar tasas, estudiantes base, paralelos.
  - Auditoría: registrar cambios con valores_anteriores + valores_nuevos.
  - No permitir editar si hay simulaciones asociadas (estado = bloqueada para edición).

- **Listar Config:**
  - Grid: carrera, escenario, tasa_ret, tasa_grad, estudiantes_p1/p2.
  - Filtros: carrera, escenario (opcional).
  - Soft-delete compatible: mostrar solo `esta_activo=true`.

#### Entidades de Dominio

```csharp
// Ya existen:
ConfiguracionRetencion
  - Id, CarreraId, EscenarioProyeccionId, TotalCiclos
  - TasaRetencionPorcentaje, TasaGraduacionPorcentaje
  - EstudiantesPeriodo1, EstudiantesPeriodo2
  - ParalelosPeriodo1, ParalelosPeriodo2
  - Métodos: ActualizarTasas(), ActualizarBaseEstudiantes()

CriterioReferenciaRetencion
  - Id, ConfiguracionRetencionId
  - MetaRetencionPorcentaje, MetaGraduacionPorcentaje
  - Método: ActualizarMetas()
```

#### Use Cases Necesarios

```
CrearConfiguracionRetencionUseCase
  → validar carrera existe + escenario existe
  → validar porcentajes (0–100)
  → guardar ConfiguracionRetencion
  → auditar CREAR

ActualizarConfiguracionRetencionUseCase
  → verificar no hay simulaciones activas
  → actualizar tasas + base estudiantes
  → auditar ACTUALIZAR

ListarConfiguracionesRetencionUseCase
  → listar carrera + escenario + tasas
  → incluir soft-delete

ObtenerConfiguracionRetencionUseCase
  → por ID, con relaciones (Carrera, Escenario)

EliminarConfiguracionRetencionUseCase
  → soft delete: esta_activo = false

CrearCriterioReferenciaRetencionUseCase
  → crear metas de retención para config existente

ActualizarCriterioReferenciaRetencionUseCase
  → actualizar metas
```

#### DTOs Necesarios

```
CrearConfiguracionRetencionSolicitudDto
  - CarreraId: int
  - EscenarioProyeccionId: int
  - TotalCiclos: int
  - TasaRetencionPorcentaje: decimal
  - TasaGraduacionPorcentaje: decimal
  - EstudiantesPeriodo1: decimal
  - EstudiantesPeriodo2: decimal
  - ParalelosPeriodo1: int
  - ParalelosPeriodo2: int

ConfiguracionRetencionRespuestaDto
  - Id, CarreraId, CarreraNombre, CarreraCodigo
  - EscenarioProyeccionId, EscenarioNombre
  - TotalCiclos, tasas, estudiantes, paralelos
  - CreadoEn, CreadoPorUsuarioId

ActualizarConfiguracionRetencionSolicitudDto
  - TasaRetencionPorcentaje, TasaGraduacionPorcentaje, estudiantes, paralelos

CriterioReferenciaRetencionDto
  - MetaRetencionPorcentaje, MetaGraduacionPorcentaje
```

#### Validaciones y Restricciones

- Tasas entre 0–100%.
- Carrera activa (`esta_activo=true`).
- Escenario activo.
- No duplicar (carrera_id + escenario_id).
- Estudiantes base ≥ 0.
- Total ciclos > 0.

#### BD (Tablas existentes)

```sql
-- Tabla: configuracion_retencion
CREATE TABLE configuracion_retencion (
  id SERIAL PRIMARY KEY,
  carrera_id INT NOT NULL REFERENCES carrera(id),
  escenario_proyeccion_id INT NOT NULL REFERENCES escenario_proyeccion(id),
  total_ciclos INT NOT NULL,
  tasa_retencion_porcentaje DECIMAL(9,4) NOT NULL,
  tasa_graduacion_porcentaje DECIMAL(9,4) NOT NULL,
  estudiantes_periodo1 DECIMAL(14,4) NOT NULL DEFAULT 0,
  estudiantes_periodo2 DECIMAL(14,4) NOT NULL DEFAULT 0,
  paralelos_periodo1 INT NOT NULL DEFAULT 0,
  paralelos_periodo2 INT NOT NULL DEFAULT 0,
  creado_en TIMESTAMPTZ DEFAULT NOW(),
  creado_por_usuario_id INT NOT NULL REFERENCES usuario(id),
  actualizado_en TIMESTAMPTZ,
  actualizado_por_usuario_id INT,
  esta_activo BOOLEAN DEFAULT true,
  eliminado_en TIMESTAMPTZ,
  eliminado_por_usuario_id INT,
  UNIQUE(carrera_id, escenario_proyeccion_id)
);

-- Tabla: criterio_referencia_retencion
CREATE TABLE criterio_referencia_retencion (
  id SERIAL PRIMARY KEY,
  configuracion_retencion_id INT NOT NULL REFERENCES configuracion_retencion(id),
  meta_retencion_porcentaje DECIMAL(9,4) NOT NULL,
  meta_graduacion_porcentaje DECIMAL(9,4) NOT NULL,
  creado_en TIMESTAMPTZ DEFAULT NOW(),
  creado_por_usuario_id INT NOT NULL REFERENCES usuario(id),
  actualizado_en TIMESTAMPTZ,
  actualizado_por_usuario_id INT,
  esta_activo BOOLEAN DEFAULT true
);
```

#### UI (WPF — Views/ViewModels)

```
TasaRetencionView
  ├── TabControl
  │   ├── Tab: "Configuración"
  │   │   ├── Button: Crear Config
  │   │   ├── DataGrid: ListarConfigs
  │   │   │   Cols: Carrera | Escenario | TasaRet% | TasaGrad% | Estuds P1/P2 | Actions (Edit/Delete)
  │   │   ├── Panel (on Edit):
  │   │   │   ├── ComboBox: Carrera (read-only en edit)
  │   │   │   ├── ComboBox: Escenario (read-only en edit)
  │   │   │   ├── Spinner: TotalCiclos
  │   │   │   ├── TextBox: TasaRet%, TasaGrad% (validar 0–100)
  │   │   │   ├── TextBox: Estuds P1/P2 (4 decimales)
  │   │   │   ├── Spinner: Paralelos P1/P2
  │   │   │   ├── Button: Guardar | Button: Cancelar

ConfiguracionRetencionViewModel
  - Commands: CrearConfigCommand, ActualizarConfigCommand, EliminarConfigCommand, ListarConfigsCommand
  - Properties: ListaConfigs, ConfigActual (para editar)
  - Validaciones: llamar use cases + propagar errores a UI

```

#### Permisos Necesarios

- `TRE.CREAR` — crear config
- `TRE.EDITAR` — editar config
- `TRE.VER` — ver lista + detalles
- `TRE.ELIMINAR` — eliminar (soft delete)

#### Dependencias

- **Interna:** IRepositorioCarrera, IRepositorioEscenarioProyeccion, IRepositorioConfiguracionRetencion
- **Externa:** IAuditoriaServicio

---

### **KAN-14 — Simulación Cohorte Ciclo a Ciclo** | 5 SP

**RF:** RF-TR-02  
**Tipo:** Backend (motor + BD)  
**Prioridad:** P1 | **Criticidad:** Tier-1

#### Alcance

Motor de simulación que proyecta retención/graduación ciclo-a-ciclo en cohorte, aplicando tasas inflacionarias si es necesario.

#### User Stories

```
US-TR-02-01: Como Analista, quiero simular una cohorte ciclo-a-ciclo
             para ver proyección de retención y graduación.

US-TR-02-02: Como Analista, quiero que la simulación aplique tasas inflacionarias
             en costos de matrícula para ajustar proyecciones.

US-TR-02-03: Como Admin, quiero que la simulación guarde cada ciclo en BD
             para auditoría y comparación posterior.
```

#### Lógica de Simulación

**Entrada:**

- ConfiguracionRetencionId
- CohorteAño (e.g., 2026)
- EscenarioProyeccionId (heredado de config)

**Proceso:**

```
For ciclo = 1 to TotalCiclos:
  estudiantesInicio = (ciclo == 1) ? EstudiantesPeriodo1 : estudiantesDelCicloAnterior
  retiene = estudiantesInicio * (TasaRetencionPorcentaje / 100)
  graduados = retiene * (TasaGraduacionPorcentaje / 100)
  reprobados = estudiantesInicio - retiene
  estudiantesDelCicloAnterior = retiene (para próximo ciclo)

  % Si aplicar inflación:
  costoMatriculaBaseAño = ObtenerInflacionProyectada(año_ciclo, escenario)
  costoMatriculaProyectado = costoMatriculaBase * (1 + costoMatriculaBaseAño / 100)

  → Guardar en BD: detalle_simulacion_retencion(ciclo, estudiantesInicio, retiene, graduados, costoMatrícula)

Total graduados = sum(graduados por ciclo)
Ret% = (Total retiene / EstudiantesPeriodo1) * 100
Titu% = (Total graduados / EstudiantesPeriodo1) * 100
```

#### Entidades Necesarias

```csharp
// Nuevas:
SimulacionRetencion : EntidadDominioBase
  - Id, ConfiguracionRetencionId
  - CohorteAño: int
  - FechaSimulacion: DateTime
  - RetencionPorcentajeFinal: decimal
  - GraduacionPorcentajeFinal: decimal
  - EstudiantesTotalesInicio: decimal
  - EstudiantesRetenidos: decimal
  - EstudiantesGraduados: decimal
  - CostoMatrículaPromedio: decimal (opcional)
  - Métodos: CalcularIndicadores(), ActualizarTasasFinales()

DetalleSimulacionRetencion : EntidadDominioBase
  - Id, SimulacionRetencionId
  - Ciclo: int
  - EstudiantesInicio: decimal
  - EstudiantesRetenidos: decimal
  - EstudiantesReprobados: decimal
  - EstudiantesGraduados: decimal
  - CostoMatrículaProyectado: decimal
  - AñoAcademico: int
  - Métodos: CalcularTasas()
```

#### Use Cases

```
CrearSimulacionRetencionUseCase
  → validar ConfiguracionRetencion existe
  → iterar ciclos aplicando lógica
  → crear SimulacionRetencion + DetalleSimulacionRetencion
  → guardar en transacción (BEGIN/COMMIT/ROLLBACK)
  → auditar CREAR_SIMULACION

ActualizarSimulacionRetencionUseCase
  → permitir solo si no tiene detalles aún (o eliminar previos)
  → recalcular indicadores
  → auditar ACTUALIZAR_SIMULACION

ListarSimulacionesRetencionUseCase
  → por carrera, escenario, año cohorte
  → incluir indicadores finales

ObtenerSimulacionRetencionUseCase
  → por ID con todos los detalles

EliminarSimulacionRetencionUseCase
  → soft delete SimulacionRetencion + hard delete DetalleSimulacionRetencion
  → (o soft delete ambas si auditoría lo requiere)

LimpiarSimulacionesRetencionUseCase
  → eliminar todas las simulaciones de una config
  → similar a LimpiarInflacionUseCase (transacción explícita)
```

#### DTOs

```
CrearSimulacionRetencionSolicitudDto
  - ConfiguracionRetencionId: int
  - CohorteAño: int
  - AplicarInflacion: bool (default = true)

SimulacionRetencionRespuestaDto
  - Id, ConfiguracionRetencionId
  - CohorteAño, FechaSimulacion
  - RetencionPorcentajeFinal, GraduacionPorcentajeFinal
  - EstudiantesTotalesInicio, EstudiantesRetenidos, EstudiantesGraduados
  - Detalles: List<DetalleSimulacionDto>

DetalleSimulacionRespuestaDto
  - Ciclo, EstudiantesInicio, EstudiantesRetenidos, EstudiantesReprobados
  - EstudiantesGraduados, CostoMatrículaProyectado, AñoAcademico
  - TasaRetencionCiclo%, TasaGraduacionCiclo%
```

#### Validaciones

- ConfiguracionRetencion existe y está activa.
- CohorteAño >= 2012 y <= 2050.
- EstudiantesPeriodo1 > 0.
- TotalCiclos > 0.
- Transacción atómica: si falla un detalle, rollback todo.

#### BD

```sql
CREATE TABLE simulacion_retencion (
  id SERIAL PRIMARY KEY,
  configuracion_retencion_id INT NOT NULL REFERENCES configuracion_retencion(id),
  cohorte_anio INT NOT NULL,
  fecha_simulacion TIMESTAMPTZ DEFAULT NOW(),
  retencion_porcentaje_final DECIMAL(9,4) NOT NULL,
  graduacion_porcentaje_final DECIMAL(9,4) NOT NULL,
  estudiantes_totales_inicio DECIMAL(14,4) NOT NULL,
  estudiantes_retenidos DECIMAL(14,4) NOT NULL,
  estudiantes_graduados DECIMAL(14,4) NOT NULL,
  costo_matricula_promedio DECIMAL(18,2),
  creado_en TIMESTAMPTZ DEFAULT NOW(),
  creado_por_usuario_id INT NOT NULL REFERENCES usuario(id),
  actualizado_en TIMESTAMPTZ,
  actualizado_por_usuario_id INT,
  esta_activo BOOLEAN DEFAULT true,
  UNIQUE(configuracion_retencion_id, cohorte_anio)
);

CREATE TABLE detalle_simulacion_retencion (
  id SERIAL PRIMARY KEY,
  simulacion_retencion_id INT NOT NULL REFERENCES simulacion_retencion(id) ON DELETE CASCADE,
  ciclo INT NOT NULL,
  estudiantes_inicio DECIMAL(14,4) NOT NULL,
  estudiantes_retenidos DECIMAL(14,4) NOT NULL,
  estudiantes_reprobados DECIMAL(14,4) NOT NULL,
  estudiantes_graduados DECIMAL(14,4) NOT NULL,
  costo_matricula_proyectado DECIMAL(18,2),
  anio_academico INT NOT NULL,
  creado_en TIMESTAMPTZ DEFAULT NOW(),
  creado_por_usuario_id INT NOT NULL REFERENCES usuario(id)
);

-- Índices
CREATE INDEX idx_simulacion_retencion_config ON simulacion_retencion(configuracion_retencion_id);
CREATE INDEX idx_simulacion_retencion_cohorte ON simulacion_retencion(cohorte_anio);
CREATE INDEX idx_detalle_simulacion_retencion_sim ON detalle_simulacion_retencion(simulacion_retencion_id);
```

#### Dependencias

- **Interna:** IRepositorioConfiguracionRetencion, IRepositorioSimulacionRetencion, IRepositorioDetalleSimulacionRetencion
- **Externa:** `ObtenerInflacionProyectadaParaDependientesUseCase` (de Épica 3), IAuditoriaServicio

---

### **KAN-15 — Indicadores Ret% y Titu%** | 3 SP

**RF:** RF-TR-02  
**Tipo:** Frontend (WPF) + reportes  
**Prioridad:** P1 | **Criticidad:** Tier-1

#### Alcance

Visualizar indicadores de retención (Ret%) y titulación (Titu%) por carrera/escenario, con gráficos comparativos.

#### User Stories

```
US-TR-02-01: Como Analista, quiero ver Ret% y Titu% calculados en una grid
             para comparar entre carreras y escenarios.

US-TR-02-02: Como Analista, quiero un gráfico de Ret% histórico + proyectado
             para análisis de tendencias.

US-TR-02-03: Como Analista, quiero exportar tabla de indicadores a Excel
             para reportes.
```

#### Lógica de Cálculo

```
Ret% = (Total Retenidos en todos los ciclos) / (Estudiantes Inicial) * 100
Titu% = (Total Graduados en todos los ciclos) / (Estudiantes Inicial) * 100
```

#### UI (WPF)

```
IndicadoresRetencionView
  ├── Panel Superior: Filtros
  │   ├── ComboBox: Carrera (opcional)
  │   ├── ComboBox: Escenario (opcional)
  │   ├── Button: Filtrar | Button: Limpiar
  │
  ├── DataGrid: Indicadores
  │   Cols: Carrera | Escenario | CohorteAño | Ret% | Titu% |
  │          EstudInicio | EstudRet | EstudGrad | CostoPromedio
  │   Acciones: Ver Detalles | Exportar
  │
  ├── Panel Gráficos (Tabs):
  │   ├── Tab: Línea Ret% (histórico + proyectado)
  │   ├── Tab: Línea Titu% (histórico + proyectado)
  │   ├── Tab: Comparativa (barras, carrera vs carrera)

IndicadoresRetencionViewModel
  - Commands: FiltrarCommand, ExportarExcelCommand, VerDetallesCommand
  - Properties: ListaIndicadores, FiltroCarrera, FiltroEscenario
  - Métodos: CargarGráficos(), FormatearDatos()
```

#### Use Cases

```
ObtenerIndicadoresRetencionUseCase
  → listar simulaciones por filtro
  → calcular Ret%, Titu%
  → devolver DTOs con indicadores

ExportarIndicadoresRetencionExcelUseCase
  → generar Excel (ClosedXML) con indicadores
  → incluir gráficos incrustados (opcional)
```

#### DTOs

```
IndicadorRetencionDto
  - CarreraId, CarreraNombre, CarreraCodigo
  - EscenarioProyeccionId, EscenarioNombre
  - CohorteAño
  - RetencionPorcentaje, GraduacionPorcentaje
  - EstudiantesTotalesInicio, EstudiantesRetenidos, EstudiantesGraduados
  - CostoMatrículaPromedio
  - SimulacionId (para deep-link a detalles)

ListaIndicadoresRetencionRespuestaDto
  - List<IndicadorRetencionDto>
  - TotalCarreras, TotalSimulaciones
```

#### Dependencias

- **Interna:** IRepositorioSimulacionRetencion, IRepositorioDetalleSimulacionRetencion
- **Externa:** Toolkit gráficos WPF (OxyPlot o similar), ClosedXML

---

### **KAN-16 — Edición con Trazabilidad** | 2 SP

**RF:** RF-TR-03  
**Tipo:** Backend (BD) + auditoría  
**Prioridad:** P1 | **Criticidad:** Tier-1

#### Alcance

Permitir edición de simulaciones + criterios de referencia con trazabilidad completa de cambios.

#### User Stories

```
US-TR-03-01: Como Admin, quiero editar los criterios de referencia
             (metas Ret% y Titu%) para ajustar modelos de comparación.

US-TR-03-02: Como Auditor, quiero ver quién editó qué, cuándo y con qué valores
             anteriores/nuevos.

US-TR-03-03: Como Admin, quiero tener un registro de ediciones históricas
             para justificar cambios ante autoridades.
```

#### Lógica

```
Editar criterio de referencia:
  1. Cargar criterio actual (MetaRet%, MetaTitu%)
  2. Usuario modifica valores
  3. Validar: 0–100%
  4. Guardar en transacción (valores_anteriores ≠ valores_nuevos → auditar)
  5. Registrar en auditoria_log:
     - modulo: "TasaRetencion"
     - entidad: "CriterioReferenciaRetencion"
     - accion: "ACTUALIZAR_CRITERIO"
     - valores_anteriores_json: {"meta_ret": 85.0, "meta_grad": 90.0}
     - valores_nuevos_json: {"meta_ret": 86.5, "meta_grad": 91.0}
     - ejecutado_por_usuario_id: sesionActual.UsuarioId
```

#### Use Cases

```
ActualizarCriterioReferenciaRetencionUseCase
  → validar existe + usuario tiene permiso
  → comparar valores anteriores vs nuevos
  → guardar + auditar solo si hay cambios
  → devolver evento de cambio

ObtenerHistorialCriterioRetencionUseCase
  → listar entradas auditoria_log para criterio específico
  → ordenar por evento_en DESC
  → filtrar por rango fechas (opcional)

ObtenerTrazabilidadSimulacionRetencionUseCase
  → listar cambios en simulación (si fue editada)
  → mostrar quién, cuándo, qué cambió
```

#### DTOs

```
ActualizarCriterioReferenciaRetencionSolicitudDto
  - MetaRetencionPorcentaje: decimal
  - MetaGraduacionPorcentaje: decimal

CriterioReferenciaConTrazabilidadRespuestaDto
  - Id, ConfiguracionRetencionId
  - MetaRetencionPorcentaje, MetaGraduacionPorcentaje
  - CreadoEn, CreadoPorUsuario
  - ActualizadoEn, ActualizadoPorUsuario
  - Historial: List<EntradaAuditoriaDto>

EntradaAuditoriaDto
  - EventoEn, ModuloNombre, EntidadNombre, AccionNombre
  - ValoresAnterioresJson, ValoresNuevosJson
  - EjecutadoPorUsuario
```

#### Validaciones

- Porcentajes 0–100%.
- No permitir cambios concurrentes (bloqueo optimista con versión/timestamp).
- Solo Admin o quien creó puede editar.

#### BD

```sql
-- No requiere tabla nueva, usa auditoria_log existente
-- índice adicional recomendado:
CREATE INDEX idx_auditoria_log_entidad ON auditoria_log(entidad_nombre, entidad_id, evento_en DESC);
```

#### Dependencias

- **Interna:** IRepositorioCriterioReferenciaRetencion, IAuditoriaServicio
- **Externa:** Nada nuevo

---

## Entidades de Dominio + BD

### Resumen de Tablas (Épica 4)

| Tabla                           | Registros (est.) | Tipo     | Auditoría | Soft-Delete     |
| ------------------------------- | ---------------- | -------- | --------- | --------------- |
| `configuracion_retencion`       | 10–50            | Catálogo | ✅ Sí     | ✅ Sí           |
| `criterio_referencia_retencion` | 10–50            | Catálogo | ✅ Sí     | ✅ Sí           |
| `simulacion_retencion`          | 100–500          | Cálculo  | ✅ Sí     | ✅ Sí           |
| `detalle_simulacion_retencion`  | 1000–5000        | Cálculo  | ❌ No     | ❌ No (CASCADE) |

### Diagrama ER (PlantUML)

```
Carrera ||--o{ ConfiguracionRetencion : "define"
EscenarioProyeccion ||--o{ ConfiguracionRetencion : "aplica a"
ConfiguracionRetencion ||--|| CriterioReferenciaRetencion : "tiene"
ConfiguracionRetencion ||--o{ SimulacionRetencion : "genera"
SimulacionRetencion ||--o{ DetalleSimulacionRetencion : "contiene"
```

### Índices Recomendados

```sql
-- Búsqueda de configs por carrera/escenario
CREATE INDEX idx_configuracion_retencion_carrera
  ON configuracion_retencion(carrera_id) WHERE esta_activo = true;
CREATE INDEX idx_configuracion_retencion_escenario
  ON configuracion_retencion(escenario_proyeccion_id) WHERE esta_activo = true;

-- Búsqueda de simulaciones
CREATE INDEX idx_simulacion_retencion_config
  ON simulacion_retencion(configuracion_retencion_id);
CREATE INDEX idx_simulacion_retencion_cohorte
  ON simulacion_retencion(cohorte_anio);

-- Búsqueda de detalles
CREATE INDEX idx_detalle_sim_ret_ciclo
  ON detalle_simulacion_retencion(simulacion_retencion_id, ciclo);
```

---

## Arquitectura y Patrones

### Estructura de Carpetas (Post-KAN-16)

```
src/
  Application/
    UseCases/
      TasaRetencion/
        ├── KAN-13/
        │   ├── CrearConfiguracionRetencionUseCase.cs
        │   ├── ActualizarConfiguracionRetencionUseCase.cs
        │   ├── ListarConfiguracionesRetencionUseCase.cs
        │   ├── ObtenerConfiguracionRetencionUseCase.cs
        │   ├── EliminarConfiguracionRetencionUseCase.cs
        │   ├── CrearCriterioReferenciaRetencionUseCase.cs
        │   └── ActualizarCriterioReferenciaRetencionUseCase.cs
        ├── KAN-14/
        │   ├── CrearSimulacionRetencionUseCase.cs
        │   ├── ActualizarSimulacionRetencionUseCase.cs
        │   ├── ListarSimulacionesRetencionUseCase.cs
        │   ├── ObtenerSimulacionRetencionUseCase.cs
        │   ├── EliminarSimulacionRetencionUseCase.cs
        │   └── LimpiarSimulacionesRetencionUseCase.cs
        ├── KAN-15/
        │   ├── ObtenerIndicadoresRetencionUseCase.cs
        │   └── ExportarIndicadoresRetencionExcelUseCase.cs
        ├── KAN-16/
        │   ├── ActualizarCriterioConTrazabilidadUseCase.cs
        │   ├── ObtenerHistorialCriterioUseCase.cs
        │   └── ObtenerTrazabilidadSimulacionUseCase.cs
    DTOs/
      TasaRetencion/
        ├── ConfiguracionRetencion*Dto.cs (3–4 DTOs)
        ├── CriterioReferenciaRetencion*Dto.cs (2–3 DTOs)
        ├── SimulacionRetencion*Dto.cs (3–4 DTOs)
        ├── DetalleSimulacionRetencion*Dto.cs (1–2 DTOs)
        ├── IndicadorRetencion*Dto.cs (2 DTOs)
        └── TrazabilidadRetencion*Dto.cs (2 DTOs)
  Domain/
    Entities/
      ├── ConfiguracionRetencion.cs (ya existe)
      ├── CriterioReferenciaRetencion.cs (ya existe)
      ├── SimulacionRetencion.cs (NUEVA)
      └── DetalleSimulacionRetencion.cs (NUEVA)
    Interfaces/
      Persistencia/
        ├── IRepositorioConfiguracionRetencion.cs
        ├── IRepositorioCriterioReferenciaRetencion.cs
        ├── IRepositorioSimulacionRetencion.cs (NUEVA)
        └── IRepositorioDetalleSimulacionRetencion.cs (NUEVA)
  Infrastructure/
    Persistence/
      ├── RepositorioConfiguracionRetencion.cs
      ├── RepositorioCriterioReferenciaRetencion.cs
      ├── RepositorioSimulacionRetencion.cs (NUEVA)
      └── RepositorioDetalleSimulacionRetencion.cs (NUEVA)
    Servicios/
      └── (Nada nuevo; auditoría ya existe)
  Presentation/
    ViewModels/
      ├── ConfiguracionRetencionViewModel.cs
      ├── IndicadoresRetencionViewModel.cs
    Views/
      ├── ConfiguracionRetencionView.xaml
      ├── IndicadoresRetencionView.xaml
    Commands/ (si no existe, MVVM Toolkit auto-genera)
```

### Patrones de Implementación

#### 1. Use Cases (Aplicación Limpia)

```csharp
public class CrearSimulacionRetencionUseCase : IUsoDelSistema
{
    private readonly IRepositorioConfiguracionRetencion _repoConfig;
    private readonly IRepositorioSimulacionRetencion _repoSim;
    private readonly IRepositorioDetalleSimulacionRetencion _repoDetalle;
    private readonly IObtenerInflacionProyectadaParaDependientesUseCase _obtenerInflacion;
    private readonly IAuditoriaServicio _auditoria;
    private readonly IUnidadDeTrabajo _unidadDeTrabajo; // Para transacción

    public async Task<SimulacionRetencionRespuestaDto> EjecutarAsync(
        CrearSimulacionRetencionSolicitudDto solicitud,
        SesionActual sesion)
    {
        // Validar permisos
        sesion.VerificarPermiso("TRE.CREAR");

        // Validar entrada
        var config = await _repoConfig.ObtenerPorIdAsync(solicitud.ConfiguracionRetencionId)
            ?? throw new DominioException($"Configuración {solicitud.ConfiguracionRetencionId} no existe");

        // Lógica principal
        var simulacion = new SimulacionRetencion(
            config.Id,
            solicitud.CohorteAño,
            config.TasaRetencionPorcentaje,
            config.TasaGraduacionPorcentaje,
            config.EstudiantesPeriodo1
        );

        var detalles = new List<DetalleSimulacionRetencion>();
        var estudiantesDelCicloAnterior = config.EstudiantesPeriodo1;

        using (var transaccion = _unidadDeTrabajo.BeginTransaction())
        {
            try
            {
                for (int ciclo = 1; ciclo <= config.TotalCiclos; ciclo++)
                {
                    var estudiantesInicio = estudiantesDelCicloAnterior;
                    var retenidos = estudiantesInicio * (config.TasaRetencionPorcentaje / 100m);
                    var graduados = retenidos * (config.TasaGraduacionPorcentaje / 100m);
                    var reprobados = estudiantesInicio - retenidos;

                    // Aplicar inflación si es necesario
                    decimal costoMatricula = 0;
                    if (solicitud.AplicarInflacion)
                    {
                        var inflacionDto = await _obtenerInflacion.EjecutarAsync(
                            solicitud.CohorteAño + ciclo - 1,
                            config.EscenarioProyeccionId
                        );
                        costoMatricula = inflacionDto?.Valor ?? 0;
                    }

                    var detalle = new DetalleSimulacionRetencion(
                        simulacion.Id,
                        ciclo,
                        estudiantesInicio,
                        retenidos,
                        reprobados,
                        graduados,
                        costoMatricula,
                        solicitud.CohorteAño + ciclo - 1
                    );

                    detalles.Add(detalle);
                    estudiantesDelCicloAnterior = retenidos;
                }

                // Calcular indicadores finales
                var totalRetenidos = detalles.Sum(d => d.EstudiantesRetenidos);
                var totalGraduados = detalles.Sum(d => d.EstudiantesGraduados);
                var retencioFinal = (totalRetenidos / config.EstudiantesPeriodo1) * 100m;
                var graduacionFinal = (totalGraduados / config.EstudiantesPeriodo1) * 100m;

                simulacion.ActualizarTasasFinales(retencioFinal, graduacionFinal);

                // Guardar
                await _repoSim.CrearAsync(simulacion);
                foreach (var detalle in detalles)
                {
                    await _repoDetalle.CrearAsync(detalle);
                }

                // Auditar
                await _auditoria.RegistrarAsync(
                    "TasaRetencion",
                    "SimulacionRetencion",
                    "CREAR_SIMULACION",
                    simulacion.Id,
                    null,
                    JsonConvert.SerializeObject(new
                    {
                        cohorte_anio = solicitud.CohorteAño,
                        ret_final = retencioFinal,
                        grad_final = graduacionFinal
                    }),
                    sesion.UsuarioId
                );

                transaccion.Commit();

                return MapearADto(simulacion, detalles);
            }
            catch (Exception)
            {
                transaccion.Rollback();
                throw;
            }
        }
    }

    private SimulacionRetencionRespuestaDto MapearADto(
        SimulacionRetencion sim,
        List<DetalleSimulacionRetencion> detalles)
    {
        return new SimulacionRetencionRespuestaDto
        {
            Id = sim.Id,
            ConfiguracionRetencionId = sim.ConfiguracionRetencionId,
            CohorteAño = sim.CohorteAño,
            RetencionPorcentajeFinal = sim.RetencionPorcentajeFinal,
            GraduacionPorcentajeFinal = sim.GraduacionPorcentajeFinal,
            EstudiantesRetenidos = sim.EstudiantesRetenidos,
            EstudiantesGraduados = sim.EstudiantesGraduados,
            Detalles = detalles.Select(d => new DetalleSimulacionRespuestaDto
            {
                Ciclo = d.Ciclo,
                EstudiantesInicio = d.EstudiantesInicio,
                EstudiantesRetenidos = d.EstudiantesRetenidos,
                EstudiantesGraduados = d.EstudiantesGraduados,
                CostoMatrículaProyectado = d.CostoMatrículaProyectado
            }).ToList()
        };
    }
}
```

#### 2. Validaciones (FluentValidation)

```csharp
public class CrearConfiguracionRetencionValidator : AbstractValidator<CrearConfiguracionRetencionSolicitudDto>
{
    public CrearConfiguracionRetencionValidator()
    {
        RuleFor(x => x.CarreraId)
            .GreaterThan(0).WithMessage("Carrera requerida");

        RuleFor(x => x.EscenarioProyeccionId)
            .GreaterThan(0).WithMessage("Escenario requerido");

        RuleFor(x => x.TotalCiclos)
            .GreaterThan(0).WithMessage("Total ciclos debe ser mayor a 0");

        RuleFor(x => x.TasaRetencionPorcentaje)
            .InclusiveBetween(0, 100).WithMessage("Tasa retención entre 0–100%");

        RuleFor(x => x.TasaGraduacionPorcentaje)
            .InclusiveBetween(0, 100).WithMessage("Tasa graduación entre 0–100%");

        RuleFor(x => x.EstudiantesPeriodo1)
            .GreaterThanOrEqualTo(0).WithMessage("Estudiantes período 1 no negativo");
    }
}

public class CrearSimulacionRetencionValidator : AbstractValidator<CrearSimulacionRetencionSolicitudDto>
{
    public CrearSimulacionRetencionValidator()
    {
        RuleFor(x => x.ConfiguracionRetencionId)
            .GreaterThan(0);

        RuleFor(x => x.CohorteAño)
            .InclusiveBetween(2012, 2050).WithMessage("Año cohorte 2012–2050");
    }
}
```

#### 3. Mapeo (AutoMapper o manual)

```csharp
// En Program.cs o DI Container
builder.Services.AddAutoMapper(typeof(MapeadorTasaRetencion));

public class MapeadorTasaRetencion : Profile
{
    public MapeadorTasaRetencion()
    {
        CreateMap<ConfiguracionRetencion, ConfiguracionRetencionRespuestaDto>()
            .ForMember(d => d.CarreraNombre, o => o.MapFrom(s => s.Carrera.Nombre))
            .ForMember(d => d.EscenarioNombre, o => o.MapFrom(s => s.EscenarioProyeccion.Nombre))
            .ReverseMap();

        CreateMap<SimulacionRetencion, SimulacionRetencionRespuestaDto>()
            .ForMember(d => d.Detalles, o => o.MapFrom(s => s.Detalles))
            .ReverseMap();
    }
}
```

#### 4. Manejo de Errores

```csharp
[ExceptionFilter(typeof(DominioException), ResultStatusCode = 400)]
[ExceptionFilter(typeof(NoAutorizadoException), ResultStatusCode = 403)]
[ExceptionFilter(typeof(NoEncontradoException), ResultStatusCode = 404)]
public class TasaRetencionController : ControllerBase
{
    [HttpPost("crear-simulacion")]
    public async Task<IActionResult> CrearSimulacion(
        [FromBody] CrearSimulacionRetencionSolicitudDto solicitud,
        [FromServices] CrearSimulacionRetencionUseCase useCase)
    {
        try
        {
            var resultado = await useCase.EjecutarAsync(solicitud, SesionActual);
            return Ok(resultado);
        }
        catch (DominioException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
```

---

## Dependencias Externas

### Nuevas (0)

Ninguna. Reutilizar existentes:

- Auditoria (KAN-09): `IAuditoriaServicio`
- Inflación (KAN-11): `ObtenerInflacionProyectadaParaDependientesUseCase`
- Usuarios: `SesionActual` (sesión autenticada)

### Existentes (Reutilizar)

- **EF Core 8:** Persistencia.
- **FluentValidation:** Validaciones.
- **BCrypt.Net-Next:** Hashing (no usado en retención, pero en sesión).
- **CommunityToolkit.Mvvm:** MVVM WPF.
- **ClosedXML:** Exportación Excel.

### Opcional (Recomendado)

- **OxyPlot:** Gráficos línea/barras en WPF.
- **Serilog:** Logging (ya debe estar implementado).

---

## Bloqueadores Conocidos

### 1. **Tablas no migradas a BD**

**Estado:** ❌ BLOQUEADOR

```
configuracion_retencion, criterio_referencia_retencion,
simulacion_retencion, detalle_simulacion_retencion
```

**Acción:** Crear migración KAN-17 (post-KAN-16) o incluir en KAN-14.  
**Tiempo estimado:** 2 horas (script SQL + migración EF).

### 2. **Falta de use case para obtener inflación**

**Estado:** ✅ RESUELTO
`ObtenerInflacionProyectadaParaDependientesUseCase` existe (KAN-12).

### 3. **Entidades SimulacionRetencion + DetalleSimulacionRetencion no creadas**

**Estado:** ❌ BLOQUEADOR
Necesario antes de KAN-14.  
**Acción:** Crear entidades + validaciones dominio.  
**Tiempo estimado:** 1 hora.

### 4. **Falta de tabla auditoria_log**

**Estado:** ✅ RESUELTO
Existe (KAN-09).

### 5. **Repositorios no creados para nuevas entidades**

**Estado:** ❌ BLOQUEADOR
Necesario antes de KAN-14.  
**Acción:** Implementar `IRepositorioSimulacionRetencion`, `IRepositorioDetalleSimulacionRetencion` + EF mappings.  
**Tiempo estimado:** 2–3 horas.

### 6. **Validaciones de conflictos con épica 5 (Estudiantes)**

**Estado:** ⚠️ ADVERTENCIA
Épica 5 también toca `proyeccion_estudiantes`. Asegurar NO overlap en KAN-17+ (post-16).

### 7. **Permisos RBAC no creados**

**Estado:** ⚠️ ADVERTENCIA
Necesario crear permisos: `TRE.CREAR`, `TRE.EDITAR`, `TRE.VER`, `TRE.ELIMINAR`.  
**Acción:** Seed en KAN-17 o inserción manual post-deploy.  
**Tiempo estimado:** 30 min.

### 8. **Gráficos WPF**

**Estado:** ⚠️ ADVERTENCIA
KAN-15 requiere librería gráfica (OxyPlot, no incluida aún).  
**Acción:** Instalar NuGet `OxyPlot.Wpf`, crear `ChartViewModel` en KAN-15.  
**Tiempo estimado:** 1–2 horas.

---

## Instrucciones para Claude Code

### Flujo de Trabajo (Ramas + Pasos)

#### **Preparación (Pre-KAN-13)**

```bash
git checkout develop
git pull origin develop
```

#### **KAN-13: Configuración (4 SP)**

```bash
git checkout feature/KAN-13-config-carrera-cohorte

# Crear entidades (ya existen en Domain/Entities/)
# - ConfiguracionRetencion.cs (existe)
# - CriterioReferenciaRetencion.cs (existe)

# NUEVO: Crear interfaces + repos
src/Domain/Interfaces/Persistencia/
  ├── IRepositorioConfiguracionRetencion.cs
  ├── IRepositorioCriterioReferenciaRetencion.cs

src/Infrastructure/Persistence/
  ├── RepositorioConfiguracionRetencion.cs
  ├── RepositorioCriterioReferenciaRetencion.cs

# NUEVO: Use cases
src/Application/UseCases/TasaRetencion/KAN-13/
  ├── CrearConfiguracionRetencionUseCase.cs
  ├── ActualizarConfiguracionRetencionUseCase.cs
  ├── ListarConfiguracionesRetencionUseCase.cs
  ├── ObtenerConfiguracionRetencionUseCase.cs
  ├── EliminarConfiguracionRetencionUseCase.cs
  ├── CrearCriterioReferenciaRetencionUseCase.cs
  └── ActualizarCriterioReferenciaRetencionUseCase.cs

# NUEVO: DTOs
src/Application/DTOs/TasaRetencion/
  ├── CrearConfiguracionRetencionSolicitudDto.cs
  ├── ConfiguracionRetencionRespuestaDto.cs
  ├── ActualizarConfiguracionRetencionSolicitudDto.cs
  ├── CriterioReferenciaRetencionSolicitudDto.cs
  └── CriterioReferenciaRetencionRespuestaDto.cs

# NUEVO: Validadores
src/Application/UseCases/TasaRetencion/KAN-13/
  ├── CrearConfiguracionRetencionValidator.cs
  ├── ActualizarConfiguracionRetencionValidator.cs
  ├── CrearCriterioReferenciaValidator.cs
  └── ActualizarCriterioReferenciaValidator.cs

# NUEVO: UI
src/Presentation/ViewModels/
  └── ConfiguracionRetencionViewModel.cs
src/Presentation/Views/
  └── ConfiguracionRetencionView.xaml
src/Presentation/Views/
  └── ConfiguracionRetencionView.xaml.cs

# NUEVO: Registrar en DI (App.xaml.cs, Program.cs)
  - Interfaces + implementaciones
  - Use cases
  - Validadores
  - ViewModels

# Test: dotnet build → 0E/0W
# Commit: "KAN-13: Config carrera y cohorte (4 SP)"
# Push: feature/KAN-13-config-carrera-cohorte → origin
```

#### **KAN-14: Simulación (5 SP)**

```bash
git checkout feature/KAN-14-simulacion-cohorte
# Merge develop (para traer cambios KAN-13)

# NUEVO: Entidades
src/Domain/Entities/
  ├── SimulacionRetencion.cs
  └── DetalleSimulacionRetencion.cs

# NUEVO: Interfaces + repos
src/Domain/Interfaces/Persistencia/
  ├── IRepositorioSimulacionRetencion.cs
  ├── IRepositorioDetalleSimulacionRetencion.cs

src/Infrastructure/Persistence/
  ├── RepositorioSimulacionRetencion.cs
  ├── RepositorioDetalleSimulacionRetencion.cs

# NUEVO: Migración EF Core
src/Infrastructure/Persistence/
  └── ContextoAplicacionModelSnapshot.cs (actualizar)
  → Crear migración KAN-14: `dotnet ef migrations add KAN_14_Retencion`

# NUEVO: Use cases
src/Application/UseCases/TasaRetencion/KAN-14/
  ├── CrearSimulacionRetencionUseCase.cs ⭐ motor
  ├── ActualizarSimulacionRetencionUseCase.cs
  ├── ListarSimulacionesRetencionUseCase.cs
  ├── ObtenerSimulacionRetencionUseCase.cs
  ├── EliminarSimulacionRetencionUseCase.cs
  └── LimpiarSimulacionesRetencionUseCase.cs

# NUEVO: DTOs
src/Application/DTOs/TasaRetencion/
  ├── CrearSimulacionRetencionSolicitudDto.cs
  ├── SimulacionRetencionRespuestaDto.cs
  ├── DetalleSimulacionRespuestaDto.cs
  └── ListaSimulacionesRespuestaDto.cs

# NUEVO: Validadores
src/Application/UseCases/TasaRetencion/KAN-14/
  ├── CrearSimulacionRetencionValidator.cs
  ├── ActualizarSimulacionRetencionValidator.cs
  └── LimpiarSimulacionesValidator.cs

# NUEVO: Mapeos
src/Application/Mapeos/
  └── MapeadorTasaRetencion.cs

# NUEVO: Registrar en DI + migración
  - EF DbSet<SimulacionRetencion>, DbSet<DetalleSimulacionRetencion>
  - Repositories
  - Use cases

# BD: Ejecutar migración
dotnet ef database update

# Test: dotnet build → 0E/0W + prueba uso case con datos fake
# Commit: "KAN-14: Simulación cohorte ciclo-a-ciclo (5 SP)"
# Push: feature/KAN-14-simulacion-cohorte → origin
```

#### **KAN-15: Indicadores (3 SP)**

```bash
git checkout feature/KAN-15-indicadores-retencion
# Merge develop

# NUEVO: Use cases
src/Application/UseCases/TasaRetencion/KAN-15/
  ├── ObtenerIndicadoresRetencionUseCase.cs
  └── ExportarIndicadoresRetencionExcelUseCase.cs

# NUEVO: DTOs
src/Application/DTOs/TasaRetencion/
  ├── IndicadorRetencionDto.cs
  └── ListaIndicadoresRespuestaDto.cs

# NUEVO: UI
src/Presentation/ViewModels/
  └── IndicadoresRetencionViewModel.cs
src/Presentation/Views/
  ├── IndicadoresRetencionView.xaml
  └── IndicadoresRetencionView.xaml.cs

# NUEVO: Gráficos (OxyPlot)
  - Instalar: `dotnet add package OxyPlot.Wpf --version 2.1.2`
  - Crear: ChartViewModel, gráficos línea + barras

# NUEVO: Exportación Excel
  - Usar: ClosedXML (ya existe)
  - Crear: ExcelExportador.cs

# DI: Registrar use cases + VM + exportador

# Test: dotnet build → 0E/0W + UI viajante
# Commit: "KAN-15: Indicadores Ret% y Titu% (3 SP)"
# Push: feature/KAN-15-indicadores-retencion → origin
```

#### **KAN-16: Trazabilidad (2 SP)**

```bash
git checkout feature/KAN-16-edicion-trazabilidad
# Merge develop

# NUEVO: Use cases
src/Application/UseCases/TasaRetencion/KAN-16/
  ├── ActualizarCriterioConTrazabilidadUseCase.cs
  ├── ObtenerHistorialCriterioUseCase.cs
  └── ObtenerTrazabilidadSimulacionUseCase.cs

# NUEVO: DTOs
src/Application/DTOs/TasaRetencion/
  ├── CriterioConTrazabilidadRespuestaDto.cs
  ├── EntradaAuditoriaDto.cs
  └── TrazabilidadSimulacionRespuestaDto.cs

# NUEVA BD (opcional): Vista para facilitar consultas auditoria
CREATE VIEW vw_auditoria_retencion AS
  SELECT * FROM auditoria_log
  WHERE modulo_nombre = 'TasaRetencion'
  ORDER BY evento_en DESC;

# DI: Registrar use cases

# Test: dotnet build → 0E/0W + verificar auditoría funciona
# Commit: "KAN-16: Edición con trazabilidad (2 SP)"
# Push: feature/KAN-16-edicion-trazabilidad → origin
```

#### **Post-KAN-16: Merge a develop + main**

```bash
# Merge a develop (en orden)
git checkout develop
git merge feature/KAN-13-config-carrera-cohorte
git merge feature/KAN-14-simulacion-cohorte
git merge feature/KAN-15-indicadores-retencion
git merge feature/KAN-16-edicion-trazabilidad
git push origin develop

# Merge a main (release)
git checkout main
git merge develop
git tag -a "epica-4-release" -m "Épica 4: Tasa Retención (14 SP)"
git push origin main --tags
```

---

### Checklist por KAN

#### ✅ KAN-13 (4 SP)

- [ ] Repositorios creados (Config + Criterio)
- [ ] Use cases creados (7 total)
- [ ] DTOs mapeados
- [ ] Validadores FluentValidation
- [ ] UI WPF (View + ViewModel)
- [ ] DI registrado
- [ ] Build 0E/0W
- [ ] Commit + push

#### ✅ KAN-14 (5 SP)

- [ ] Entidades SimulacionRetencion + DetalleSimulacionRetencion
- [ ] Repositorios creados
- [ ] Migración EF Core
- [ ] Use cases (motor + CRUD)
- [ ] DTOs
- [ ] Validadores
- [ ] Mapeos AutoMapper
- [ ] DI registrado
- [ ] BD actualizada
- [ ] Build 0E/0W
- [ ] Commit + push

#### ✅ KAN-15 (3 SP)

- [ ] Use cases indicadores
- [ ] DTOs indicadores
- [ ] ViewModel gráficos
- [ ] UI gráficos (OxyPlot)
- [ ] Exportación Excel (ClosedXML)
- [ ] DI registrado
- [ ] Build 0E/0W
- [ ] Commit + push

#### ✅ KAN-16 (2 SP)

- [ ] Use cases trazabilidad
- [ ] DTOs trazabilidad
- [ ] Auditoría integrada
- [ ] DI registrado
- [ ] Vista BD (opcional)
- [ ] Build 0E/0W
- [ ] Commit + push

---

### Comandos Rápidos

```powershell
# Compilar + validar
cd D:\Tesis\SistemaAranceles
dotnet build .\SistemaAranceles.sln

# Crear migración (KAN-14)
dotnet ef migrations add KAN_14_Retencion \
  --project .\src\Infrastructure\SistemaAranceles.Infrastructure.csproj \
  --startup-project .\src\Presentation\SistemaAranceles.Presentation.csproj

# Aplicar migración
dotnet ef database update \
  --project .\src\Infrastructure\SistemaAranceles.Infrastructure.csproj \
  --startup-project .\src\Presentation\SistemaAranceles.Presentation.csproj

# Git status
git status
git log --oneline -5

# Instalar paquete (OxyPlot)
dotnet add .\src\Presentation\SistemaAranceles.Presentation.csproj package OxyPlot.Wpf --version 2.1.2
```

---

## Conclusión

**Estado:** Épica 4 está 100% mapeada, bloqueadores identificados, ramas listas.

**Próximos pasos:**

1. ✅ Ramas creadas (hecho).
2. 🚀 Claude Code inicia KAN-13 en `feature/KAN-13-config-carrera-cohorte`.
3. 📈 Seguir flujo KAN-13 → KAN-14 → KAN-15 → KAN-16.
4. 🔀 Merge a develop post-KAN-16.
5. 🏁 Merge a main como release (post-validación QA).

**Tiempo estimado:** 8–10 días (4–5 SP/semana, 2 desarrolladores).

**Contacto:** Cualquier duda → revisar contexto.md + especificaciones RF-TR-01/02/03.
