# Decisiones — Cálculo de Docentes (PhD/Mgs/MT/TP) y Override de Horas

**Estado:** ✅ Fases 1-6 implementadas · 14/14 tests OK · 0 warnings · BD migrada
**Fuente:** sesiones con docente experto del dominio + cruce con Excel "1 Estudiantes"
**Trazabilidad:** Excel = espec viva · este doc = contrato · [`docs/Trazabilidad_RN_Excel.md`](Trazabilidad_RN_Excel.md) = mapa RN ↔ código ↔ test · acta en [`docs/actas/2026-05-07-acuerdo-mt-tp.md`](actas/2026-05-07-acuerdo-mt-tp.md)
**Última actualización:** 2026-05-10 (Fases 5 y 6 completadas)

---

## 1. Alcance

Tres cambios encadenados:

1. **Algoritmo MT/TP** en proyección de docentes — corrige el descuadre actual donde el residuo decimal cae todo al Tiempo Parcial pagado como sueldo fijo.
2. **Override de horas por período** (Tab 4 "Consumo por período") — UI editable + persistencia.
3. **Tarifa $/h para Tiempo Parcial** — diferenciar pago fijo vs por hora en `CargoFacultad` y propagar a `CalculoCargosFacultad`, `GenerarTablaSueldosPeriodoQuery`, `GenerarResumenSueldosQuery`.

---

## 2. Mapa Excel ↔ Código

| Excel | Código |
|---|---|
| `1 Estudiantes` | `Estudiantes` module · [ConsolidadorProyeccionEstudiantes.cs](../src/Application/UseCases/Estudiantes/ConsolidadorProyeccionEstudiantes.cs) · [EstudiantesView.xaml](../src/Presentation/Views/Estudiantes/EstudiantesView.xaml) |
| `1 Estudiantes` filas 24-30 (docentes) | Tab 2 — `FilaDocentePeriodoDto` (a extender) |
| `1 Estudiantes` filas 15-21 (horas) | Tab 3 — `FilaHorasDto` |
| `1 Estudiantes` filas 16/20 (consumo editable) | Tab 4 — `FilaConsumoPeriodicDto` (a extender + persistencia) |
| `1 Estudiantes!J24=18` | `HorasDocenteSemana` constante |
| `1 Estudiantes!J30=40` | `HorasTecnicoSemana` constante |
| celda inexistente | `HorasMTSemana=12` (nueva) |
| celda inexistente | `HorasTPMaxSemana=12` (nueva) |
| celda inexistente | `UmbralResiduoMT=12` (nueva) |
| celda inexistente | `PorcentajeMgs=0.60m` (nueva, hoy hardcoded en fórmula) |
| celda inexistente | `UmbralPhdMinimoHoras=36m` (nueva, regla CES — ver §4.7) |
| `5 Demanda` | `DemandaIngresos` module — solo CU docs, sin código |
| `7 Sueldos` (Facultad) | [CargosFacultad/](../src/Application/UseCases/CargosFacultad/) · [CalculoCargosFacultad.cs](../src/Application/UseCases/CargosFacultad/CalculoCargosFacultad.cs) |
| `7 Sueldos` (Planta Central) | `SueldosPlantaCentral` — solo CU docs, sin código |
| `7 Sueldos!B193:E210` (resumen) | [ResumenSueldosWindow.xaml](../src/Presentation/Views/CargosFacultad/ResumenSueldosWindow.xaml) |
| celda inexistente (`TarifaHora` TP) | `CargoFacultad.TarifaHora` (nueva columna NOT NULL DEFAULT 0) |

---

## 3. Algoritmo final (validado contra los 8 períodos del Excel)

### Constantes

```text
HorasDocenteTC        = 18m     // J24 del Excel
HorasMTSemana         = 12m
HorasTPMaxSemana      = 12m
UmbralResiduoMT       = 12m
PorcentajeMgs         = 0.60m
SemanasPorMes         = 4m
UmbralPhdMinimoHoras  = 36m     // CES: 1 PhD obligatorio cuando horas ≥ 36h/sem (≈ 2 docentes TC)
```

Ubicación propuesta: `Domain/Constantes/ConstantesDocentes.cs` (nueva). Todas son configurables por Administrador en una iteración futura.

### Lógica

```text
totalDec  = horasAsistidas / HorasDocenteTC
tcEntero  = floor(totalDec)
residuoH  = (totalDec - tcEntero) × HorasDocenteTC

# Distribución base
mgs = round(tcEntero × 0.60, AwayFromZero)
phd = tcEntero - mgs

# Regla CES (1 PhD mínimo cuando hay capacidad estructural)
# Solo se activa si la carrera ya soporta ≥2 docentes TC,
# para no romper la invariante de horas en períodos iniciales pequeños.
if horasAsistidas >= UmbralPhdMinimoHoras AND tcEntero >= 2 AND phd == 0:
    phd = 1
    mgs = tcEntero - 1   # se redistribuye dentro de tcEntero — no se inflan horas

# Medio Tiempo y Tiempo Parcial (cubren residuo)
if residuoH > UmbralResiduoMT:
    mt = 1; hMT = HorasMTSemana
else:
    mt = 0; hMT = 0

residuoDespuesMT = residuoH - hMT
if residuoDespuesMT > 0:
    if residuoDespuesMT <= HorasTPMaxSemana:
        tp = 1; hTP = residuoDespuesMT
    else:
        tp = ceil(residuoDespuesMT / HorasTPMaxSemana)   # primer TP llena 12h, segundo el resto
        hTP = residuoDespuesMT
else:
    tp = 0; hTP = 0

INVARIANTE: phd*18 + mgs*18 + hMT + hTP == horasAsistidas
```

### Tabla de validación (8 períodos del Excel = 8 tests de regresión)

| Período | Horas | PhD | Mgs | MT | TP | hMT | hTP | Verifica invariante |
|---|---|---|---|---|---|---|---|---|
| P1 (2023A) | 18  | 0 | 1 | 0 | 0 |  0 | 0 | 0+18+0+0 = **18** ✓ |
| P2 (2023B) | 60  | 1 | 2 | 0 | 1 |  0 | 6 | 18+36+0+6 = **60** ✓ |
| P3 (2024A) | 80  | 2 | 2 | 0 | 1 |  0 | 8 | 36+36+0+8 = **80** ✓ |
| P4 (2024B) | 122 | 2 | 4 | 1 | 1 | 12 | 2 | 36+72+12+2 = **122** ✓ |
| P5 (2025A) | 143 | 3 | 4 | 1 | 1 | 12 | 5 | 54+72+12+5 = **143** ✓ |
| P6 (2025B) | 183 | 4 | 6 | 0 | 1 |  0 | 3 | 72+108+0+3 = **183** ✓ |
| P7 (2026A) | 202 | 4 | 7 | 0 | 1 |  0 | 4 | 72+126+0+4 = **202** ✓ |
| P8 (2026B) | 234 | 5 | 8 | 0 | 0 |  0 | 0 | 90+144+0+0 = **234** ✓ |

**Verificación CES con `UmbralPhdMinimoHoras=36`:**
- P1 (18h < 36) → CES no aplica → `(0 PhD, 1 Mgs)` se preserva ✓
- P2..P8 ya tienen `phd ≥ 1` por la fórmula base → CES no necesita disparar
- Invariante se mantiene en los 8 casos ✓

### Desempate múltiples TP

Si por configuración del usuario `residuoDespuesMT > HorasTPMaxSemana`:
**el primer TP toma 12h, el segundo el resto**. No se reparte por igual.

---

## 4. Decisiones de diseño

### 4.1. Persistencia del override de horas (Tab 4 editable)

**Decisión:** tabla hija `override_horas_periodo` en BD (NO memoria, NO `decimal[]`, NO JSONB).

```sql
CREATE TABLE override_horas_periodo (
    id              SERIAL PRIMARY KEY,
    proyeccion_id   INT NOT NULL REFERENCES proyeccion_estudiantes(id) ON DELETE CASCADE,
    periodo         SMALLINT NOT NULL CHECK (periodo BETWEEN 1 AND 20),
    horas_docencia  DECIMAL(10,2),    -- NULL = usar default
    horas_practica  DECIMAL(10,2),    -- NULL = usar default
    actualizado_por INT REFERENCES usuario(id),
    actualizado_en  TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE (proyeccion_id, periodo)
);
```

**Notas:**
- FK por `proyeccion_id` → override **por proyección** (no global por carrera). Cada escenario tiene su propio set.
- Periodo va `1..20` (no fijo en 8) para soportar carreras de >4 años.
- `NULL` por columna permite override solo de docencia o solo de práctica.
- Migración EF Core nueva: `KAN_XX_OverrideHorasPeriodo`.
- **Auditoría:** se delega al `auditoria_log` general (fire-and-forget vía `IAuditoriaServicio.RegistrarAsync`). NO se duplica en la tabla del override — mantiene la tabla limpia y consistente con el patrón del resto del sistema (KAN-06..09).

**Cascada de recálculo al editar:**
```
Override H.Docencia/H.Práctica P[k]
└─ Tab 3 filas 15/17/19/21 (P[k] en adelante)
   └─ Tab 2 docentes (P[k] en adelante)
      └─ Tab 4 consumo (P[k])
         └─ 7 Sueldos / CargosFacultad (P[k] en adelante)
            └─ 6 Capital de Trabajo
               └─ 8 Estado de Resultados
NO afecta: Matrícula, Tasa Retención, períodos previos.
```

### 4.2. `CargoFacultad` — `SueldoBaseMensual` y `TarifaHora` ambos NOT NULL DEFAULT 0

**Decisión (no destructiva):** dejar `SueldoBaseMensual` como está (no nullable) y agregar `TarifaHora decimal` no nullable con default `0m`. La fuente de verdad por tipo se distingue por `TipoContrato` (§4.3), NO por `null`.

| Tipo | SueldoBaseMensual | TarifaHora | Validación |
|---|---|---|---|
| PhD / Mgs / MT | **> 0** (verdad) | `0m` (no usado) | `SueldoBaseMensual > 0` requerido |
| TP | `0m` (no usado) | **> 0** (verdad) | `TarifaHora > 0` requerido |
| Administrativo / Técnico | **> 0** | `0m` | `SueldoBaseMensual > 0` requerido |

**Por qué esta decisión y no `nullable`:**

| Opción | Impacto |
|---|---|
| ❌ `decimal?` nullable | Rompe `GuardiaDominio.DecimalNoNegativo` en [CargoFacultad.cs:51](../src/Domain/Entities/CargoFacultad.cs#L51). Hay que reescribir el guard, actualizar todas las queries, validators, seeds. Cambio invasivo. |
| ✅ `decimal` not null DEFAULT 0 + validación en `TipoContrato` | No toca `GuardiaDominio`. Migración aditiva. Cero riesgo de regresión en código existente. La invariante "el campo correcto > 0 según tipo" se valida en el factory/command, no en el guard. |

**Migración (aditiva, no destructiva):**

```sql
ALTER TABLE cargo_facultad
    ADD COLUMN tarifa_hora DECIMAL(10,4) NOT NULL DEFAULT 0,
    ADD COLUMN tipo_contrato VARCHAR(30) NOT NULL DEFAULT 'Administrativo';

-- Backfill: clasificar cargos existentes según TipoCargo string
UPDATE cargo_facultad SET tipo_contrato = 'PhD'
    WHERE LOWER(tipo_cargo) LIKE '%phd%';
UPDATE cargo_facultad SET tipo_contrato = 'Mgs'
    WHERE LOWER(tipo_cargo) LIKE '%mgs%' OR LOWER(tipo_cargo) LIKE '%magister%';
UPDATE cargo_facultad SET tipo_contrato = 'MedioTiempo'
    WHERE LOWER(tipo_cargo) LIKE '%medio tiempo%';
UPDATE cargo_facultad SET tipo_contrato = 'Tecnico'
    WHERE LOWER(tipo_cargo) LIKE '%tecnico%';
UPDATE cargo_facultad SET tipo_contrato = 'TiempoParcial'
    WHERE LOWER(tipo_cargo) LIKE '%parcial%';

-- Backfill tarifa_hora para TP existentes desde el sueldo mensual actual
UPDATE cargo_facultad
SET tarifa_hora = sueldo_base_mensual / (12 * 4),
    sueldo_base_mensual = 0
WHERE tipo_contrato = 'TiempoParcial';
```

**Helper de inicialización (no propiedad calculada):**
```csharp
public static decimal CalcularTarifaInicial(decimal sueldoMensual, decimal horasSemana)
    => sueldoMensual / (horasSemana * SemanasPorMes);   // 4
```

**UI:** al crear/editar un cargo TP, el formulario sugiere `TarifaHora = SueldoMensual / (12 × 4)` como default editable. Se muestran ambos campos (sueldo equivalente + tarifa) para que el docente vea coherencia.

**Cómo lee el código:**
```csharp
// CalculoCargosFacultad
var costoBase = cargo.TipoContrato == TipoContrato.TiempoParcial
    ? cargo.TarifaHora * horasAsignadasSemana * SemanasPorMes * 6      // sin beneficios
    : CostoBaseSemestralCompleto(cargo.SueldoBaseMensual);              // PhD/Mgs/MT/Admin
```

### 4.3. `TipoContrato` enum

**Decisión:** agregar enum `TipoContrato` en `Domain/Enums/`:

```csharp
public enum TipoContrato
{
    Administrativo = 0,   // default histórico — backfill seguro
    PhD            = 1,
    Mgs            = 2,
    MedioTiempo    = 3,
    TiempoParcial  = 4,
    Tecnico        = 5
}
```

**Reemplaza** la distinción frágil actual (`EsCargoDocente bool` + `TipoCargo string`).
**No elimina** los campos viejos en esta fase — se mantienen por compatibilidad y se marcarán `[Obsolete]` en una fase posterior.

> **Razón:** sin enum, distinguir TP de Mgs requiere parsear strings, lo cual hace que `CalculoCargosFacultad` no sepa cuándo usar `TarifaHora` vs `SueldoBaseMensual`.

### 4.4. `FilaDocentePeriodoDto` — extender, no reemplazar

**Decisión:** agregar series paralelas en el mismo DTO en lugar de aplanarlo:

```csharp
public sealed class FilaDocentePeriodoDto
{
    public string  Tipo     { get; init; }   // se conserva para no romper la vista
    public int[]   Periodos { get; init; }   // personas
    public int     Total    { get; init; }
    public decimal[]? HorasAsignadas { get; init; }  // NEW — solo MT y TP usan
}
```

La vista actual ([EstudiantesView.xaml:259-307](../src/Presentation/Views/Estudiantes/EstudiantesView.xaml#L259-L307)) itera por filas. Mantenemos ese patrón y agregamos 2 filas nuevas debajo:
- "Horas asignadas Medio Tiempo"
- "Horas asignadas Tiempo Parcial"

### 4.5. TP — pago sin beneficios sociales

**Decisión:** TP se contrata por **servicios profesionales** (no relación de dependencia).

Costo TP por período:
```
costoSemestralTP = TarifaHora × HorasAsignadasSemana × SemanasPorMes × 6
                 = TarifaHora × hTP × 4 × 6
                 = TarifaHora × hTP × 24
```

**No** se aplican: DXIII, DXIV, Vacaciones, Fondo Reserva, Aporte Patronal.

**Impacto en RN-79:** la fórmula única deja de aplicar a TP. Hay que dividir RN-79 en dos variantes (ver §5).

### 4.6. ResumenSueldosWindow — vista por defecto = "Resumen 4 años"

**Decisión:** la vista principal muestra el **acumulado de los 4 años (todos los períodos)**. El último período (P_n) aparece como sub-bloque destacado, no como vista exclusiva.

Layout:
```
┌─ Encabezado: "Resumen Sueldos — Carrera X — 4 años (P1..Pn)"
│
├─ Tabla principal (15 cargos × n períodos)        ← evolución completa
│   [igual al pivot actual de ResumenSueldosWindow]
│
├─ Footer:
│   - Totales por período
│   - Total acumulado 4 años (suma de los n períodos)        ← NUEVO
│   - Bloque destacado: "Sueldo mensual Período Final P[n]"  ← NUEVO
│   - Ratio Sueldos/Ingresos por período                     ← NUEVO
```

> El pivot actual ya muestra evolución; lo que se agrega es el bloque destacado del último período + ratio + total acumulado explícito.

### 4.7. Mínimo 1 PhD por carrera (norma CES Ecuador) — con umbral paramétrico

**Decisión:** aplicar la regla CES **solo cuando la carrera tiene capacidad estructural para ≥2 docentes TC**. Esto preserva la invariante de horas y respeta el comportamiento real del Excel.

**Constante nueva:** `UmbralPhdMinimoHoras = 36m` (default) — equivale a "carrera con ≥2 docentes TC requeridos".

**Lógica (ya integrada en §3):**
```
if horasAsistidas >= UmbralPhdMinimoHoras AND tcEntero >= 2 AND phd == 0:
    phd = 1
    mgs = tcEntero - 1   # redistribución interna; NO infla horas asignadas
```

**Por qué umbral paramétrico (opción c) y no `Math.Max(1, phd)` puro:**

| Opción | P1 (18h, tcEntero=1, phd=0) | Invariante |
|---|---|---|
| Sin CES | (0 PhD, 1 Mgs) | 18 = 18 ✓ |
| `Math.Max(1, phd)` directo | (1 PhD, 1 Mgs) → 36h asignadas | 36 ≠ 18 ❌ |
| **Con umbral 36 + tcEntero≥2** | no aplica → (0 PhD, 1 Mgs) | 18 = 18 ✓ |

**Cuando sí aplica** (ej. carrera muy pequeña con 36-54h asistidas y phd calculado=0):
- tcEntero = 2 → forzar phd=1, mgs=1 → 18+18 = 36h ✓ (sigue siendo válido)
- tcEntero = 3 → mgs ya es 2 por round, phd=1 → no se necesita forzar
- En la práctica, con `round(AwayFromZero)` para mgs, **`phd = 0` solo ocurre cuando `tcEntero = 1`** (carrera de 18h-35h). El umbral protege ese caso.

**Configurable por Administrador:** la constante puede subirse (ej. UmbralPhdMinimoHoras=60) si la institución decide que CES aplica solo desde 60h asistidas/semana (≈ 3 docentes TC).

**Nota regulatoria:** la norma CES exige PhD por carrera de pregrado. El docente confirmó que aplica, pero los datos del Excel para P1 muestran 0 PhD — lo cual se interpreta como "fase de implementación inicial donde la carrera aún no opera con planta completa". El umbral formaliza esa excepción.

---

## 5. Casos de Uso a actualizar

> Nomenclatura aclarada: el otro chat usó "CU-71/72/73" pero esos son **RN**, no CU. Los CU son CU-ES-01..03 + CU-SP-01..04. Las RN-71/72/73 ya viven dentro de CU-ES-03 como una línea de placeholder; se expanden aquí.

### CU-ES-02 — Configurar y calcular horas (modificar)

| RN | Estado | Cambio |
|---|---|---|
| RN-61 | expandir | "Horas semestrales son **editables por período** (no único valor)" |
| RN-66 | sin cambio | recálculo automático sigue igual |
| **RN nueva** | agregar | "El override por período se persiste en `override_horas_periodo` y prevalece sobre los defaults" |

### CU-ES-03 — Calcular docentes requeridos (reescribir reglas)

| RN | Hoy dice | Cambia a |
|---|---|---|
| RN-67 | "Parámetros configurables solo Administrador" | igual + lista expandida (HorasMTSemana, HorasTPMaxSemana, UmbralResiduoMT, PorcentajeMgs, UmbralPhdMinimoHoras, TarifaHora) |
| RN-68 | "Distribución 40%/60%" | "Mgs = round(tcEntero × 60%, AwayFromZero); PhD = tcEntero − Mgs (residual)" |
| RN-70 | "Garantizar 1 Mgs" | "Garantizar 1 Mgs siempre que tcEntero ≥ 1. **Garantizar 1 PhD cuando horasAsistidas ≥ UmbralPhdMinimoHoras Y tcEntero ≥ 2 Y phd calculado = 0** (norma CES). En ese caso se redistribuye dentro de tcEntero (mgs = tcEntero − 1), preservando la invariante de horas." |
| RN-71 | "Redondeo techo" | "tcEntero = floor(horasAsistidas / 18). Mgs = round(tcEntero × 0.60, AwayFromZero). PhD = residual entero. **NOTA: aunque la verbalización original del docente fue 'CEILING', la tabla esperada de los 8 períodos solo casa con esta combinación floor + round; manda la tabla.**" |
| RN-72 | "Parcial residual" | "Residuo de horas = (totalDec − tcEntero) × 18. Si residuo > UmbralResiduoMT (12) → +1 MT con HorasMTSemana (12) fijas. El sobrante < HorasTPMaxSemana (12) → +1 TP con horas variables. Si sobrante > HorasTPMaxSemana → primer TP llena 12h, siguientes TP toman el resto." |
| RN-73 | "Datos enviados a sueldos" | "Personas (PhD, Mgs, MT, TP) se envían a Sueldos con su tipo. Para TP se envía además HorasAsignadas (decimal) por período." |
| **RN nueva** | — | "Invariante de horas: PhD×18 + Mgs×18 + MT×12 + TP×hTP = HorasAsistidas (igualdad exacta). Validada por test contra los 8 períodos del Excel." |
| **RN nueva** | — | "Constantes HorasMTSemana=12, HorasTPMaxSemana=12, UmbralResiduoMT=12, UmbralPhdMinimoHoras=36 son configurables por Administrador." |

### CU-ES-04 — Editar Consumo por Período (NUEVO)

Crear archivo [src/Application/UseCases/Estudiantes/CU-ES-04.MD](../src/Application/UseCases/Estudiantes/CU-ES-04.MD) con:

```text
Identificador:    CU-ES-04
Nombre:           Editar Consumo por Período (override de horas)
Actor primario:   Planificador Académico
Precondición:     Existe ProyeccionEstudiantes en estado "Generada"

Flujo principal:
1. Usuario abre Tab "4. Consumo por período"
2. Sistema muestra grilla n períodos × {H.Docencia, H.Práctica}
3. Usuario edita celda(s)
4. Sistema valida: número positivo, ≤ tope físico (paralelos × horas/paralelo)
5. Sistema persiste override en override_horas_periodo
6. Sistema dispara recálculo en cascada (Tab 3, Tab 2, Sueldos, Capital de Trabajo)
7. Sistema notifica completado

Flujo alterno:
4a. Validación falla → Sistema muestra mensaje, mantiene valor previo
5a. Usuario "Restaurar valor original" → DELETE de la fila override → recalcula con default

Postcondición:
- Override persistido
- Cálculos downstream actualizados
- Histórico registrado en auditoria_log (fire-and-forget vía IAuditoriaServicio)

RN nuevas:
- RN-XX: Validación >0
- RN-XX: Granularidad por período (no global)
- RN-XX: Reset disponible para volver a defaults
- RN-XX: Override prevalece sobre cálculo automático desde malla
- RN-XX: Auditoría se delega al servicio general; no se duplica en la tabla override
```

### CU-SP-01 — Configurar cargos (modificar)

| RN | Cambio |
|---|---|
| RN-74 | agregar Medio Tiempo a la lista de tipos provenientes de Estudiantes |
| RN-76 | aclarar: "fórmulas de beneficios aplican **solo a PhD/Mgs/MT/Administrativo**. TP no recibe DXIII/DXIV/Vacaciones/FR/AP" |
| **RN nueva** | "Cargo con `TipoContrato == TiempoParcial` usa `TarifaHora` (DECIMAL NOT NULL DEFAULT 0). Helper de inicialización: `TarifaHora = SueldoMensual / (HorasTPSemana × 4)`. Tras inicializar, `SueldoBaseMensual` se setea a 0 para TP." |
| **RN nueva** | "Validación por tipo: si `TipoContrato == TiempoParcial` → `TarifaHora > 0` requerido y `SueldoBaseMensual = 0` permitido. Caso contrario → `SueldoBaseMensual > 0` requerido y `TarifaHora = 0` permitido." |

### CU-SP-02 — Calcular sueldos facultad (modificar)

| RN | Cambio |
|---|---|
| RN-79 | dividir en dos variantes: <br>**(a) PhD/Mgs/MT/Admin:** `Total = N × Peso × Sueldo + FR + AP + 6×DXIII + DXIV + Vacaciones` (igual a hoy)<br>**(b) TP:** `Total = HorasAsignadas × TarifaHora × 24` (sin beneficios) |
| RN-83 | añadir Medio Tiempo y horas TP a los disparadores de recálculo |

### CU-SP-04 — Consolidado (modificar)

| RN | Cambio |
|---|---|
| RN-90 | "Los **5** tipos de docentes (PhD, Mgs, MT, TP, Técnico) son Costos por Servicios" (hoy dice 4) |

---

## 6. Trazabilidad para tesis

Crear `docs/Trazabilidad_RN_Excel.md` con tabla por cada RN modificada:

```markdown
## RN-71 — Cálculo de Docentes por Período (versión 2026-XX-XX)

| Aspecto              | Artefacto |
|----------------------|-----------|
| Fuente del requisito | Reunión con docente experto, [fecha]. Acta en docs/actas/[fecha].md |
| Espec viva           | Hoja Excel "1 Estudiantes" rangos B24:I33 |
| Caso de uso          | CU-ES-03 en src/Application/UseCases/Estudiantes/CU-ES-03.MD |
| Algoritmo            | ConsolidadorProyeccionEstudiantes.Calcular |
| Línea de código      | src/Application/UseCases/Estudiantes/ConsolidadorProyeccionEstudiantes.cs:149-218 |
| Test de regresión    | tests/.../CalculoDocentesTests.cs::Docentes_CoincideConExcel |
| Validación cobertura | Invariante: phd*18 + mgs*18 + h_MT + h_TP == horas_asistidas |

### Datos de validación
[la tabla §3 de este doc]
```

Esto cierra el ciclo **Requisito → Diseño → Implementación → Verificación** que el tribunal académico valora.

---

## 7. Decisiones cerradas (eran issues abiertos)

### Decisión #1 — Conflicto CES vs invariante de horas → **Opción (c) umbral paramétrico**

| Opción evaluada | Resultado |
|---|---|
| (a) Ignorar CES en P1 (fase ramping-up) | Funciona pero deja la regla CES sin formalizar; queda implícita |
| (b) `Math.Max(1, phd)` siempre | Rompe la invariante de horas en P1 (asigna 36h cuando se requieren 18h) |
| **(c) Umbral paramétrico — ELEGIDA** | Aplica CES solo cuando `horasAsistidas ≥ UmbralPhdMinimoHoras` Y `tcEntero ≥ 2`. Preserva invariante. Permite ajuste futuro por norma. |

**Implementación:** ya integrada en §3 (algoritmo) y §4.7. Constante `UmbralPhdMinimoHoras = 36m` por default, configurable por Administrador.

**Por qué 36 como default:** equivale a "carrera con ≥2 docentes TC requeridos". Por debajo de eso, exigir 1 PhD obligatorio rompe la matemática (forzaría tcEntero = 0 y mgs negativo). Es el límite estructural mínimo donde la regla CES tiene sentido sin distorsionar el modelo.

### Decisión #2 — CEILING vs ROUND → **Manda la tabla**

El docente verbalizó "redondear al inmediato superior (CEILING)" pero su tabla esperada de 8 períodos solo casa con `floor(total) + round(×0.6, AwayFromZero)`. Verificación:

| Período | Si fuera CEILING estricto | Tabla esperada |
|---|---|---|
| P2 (60h) | `ceil(3.33)=4` Total, `ceil(4×0.6)=3` Mgs, `ceil(3×0.4)=2` PhD → suma 5 ≠ 4 ❌ | Total 3, Mgs 2, PhD 1 ✓ |

**Decisión:** la tabla de los 8 períodos es la espec viva. RN-71 se redacta con `floor + round` y se documenta la nota: "verbalización original fue CEILING; manda la tabla". Los tests son la prueba.

### Decisión #3 — Auditoría del override → **Delegar a `auditoria_log` general**

| Opción | Decisión |
|---|---|
| Inline (`valor_anterior`, `motivo_cambio` en la tabla override) | ❌ Duplica responsabilidad. Si en el futuro se cambia el formato de auditoría hay que tocar 2 lugares. |
| **Delegar a `auditoria_log` general (fire-and-forget)** | ✅ Consistente con el patrón del resto del sistema (KAN-06..09). Una sola fuente de verdad para auditoría. La tabla override queda limpia con solo los datos del estado actual. |

**Implementación:** al guardar/eliminar un override, el use case llama `IAuditoriaServicio.RegistrarAsync(...)` con el contexto suficiente (proyeccion_id, periodo, valor_nuevo, valor_anterior dentro del payload del log). El `actualizado_por` y `actualizado_en` en la tabla override quedan solo para resolución rápida de "quién fue el último", no como historial.

### Decisión #4 — `SueldoBaseMensual` nullable → **Mantener no-nullable + agregar `TarifaHora` no-nullable**

| Opción | Riesgo |
|---|---|
| ❌ `decimal?` nullable | Rompe `GuardiaDominio.DecimalNoNegativo` ([CargoFacultad.cs:51](../src/Domain/Entities/CargoFacultad.cs#L51)). Hay que reescribir el guard, todos los queries (`GenerarTablaSueldosPeriodoQuery`, `GenerarResumenSueldosQuery`), validators y seeds. **Cambio invasivo, alto riesgo de regresión.** |
| ✅ `SueldoBaseMensual decimal NOT NULL` (sin cambio) + `TarifaHora decimal NOT NULL DEFAULT 0` | **No toca código existente.** Migración aditiva. La invariante "el campo correcto > 0 según tipo" se valida en el `CargoFacultad` factory/command, no en el guard. Cero regresión. |

**Implementación:** ya documentada en §4.2. La distinción TP vs no-TP se hace por `TipoContrato` (enum, §4.3), no por `null`. Para TP existentes, el seed/migración hace `tarifa_hora = sueldo_base_mensual / (12*4)` y `sueldo_base_mensual = 0`.

---

## 8. Plan de implementación — ESTADO DE EJECUCIÓN

### ✅ Fase 1 — Fundamentos (completada)
1. ✅ [ConstantesDocentes.cs](../src/Domain/Constantes/ConstantesDocentes.cs) — 7 constantes
2. ✅ [TipoContrato.cs](../src/Domain/Enums/TipoContrato.cs) — enum (Administrativo, PhD, Mgs, MedioTiempo, TiempoParcial, Tecnico)
3. ✅ [CU-ES-03.MD](../src/Application/UseCases/Estudiantes/CU-ES-03.MD) — RN-67..73 reescritas + RN-94/RN-95 nuevas (renumerado a la cola por colisión con RN-74 de CU-SP-01)

### ✅ Fase 2 — Algoritmo MT/TP (completada · 14/14 tests OK)
4. ✅ [ConsolidadorProyeccionEstudiantes.DesglosarDocentesPorPeriodo](../src/Application/UseCases/Estudiantes/ConsolidadorProyeccionEstudiantes.cs) — helper público estático + integración en `Calcular`. Fix decimal precision: residuo se calcula sin división (`horas - tcEntero × 18`)
5. ✅ Fila "Medio Tiempo" emitida con `mtArr[]` real (no más ceros)
6. ✅ [FilaDocentePeriodoDto](../src/Application/DTOs/Estudiantes/ProyeccionConsolidadaDto.cs) extendido con `decimal[]? HorasAsignadas`
7. ✅ [CalculoDocentesTests.cs](../tests/Application.Tests/Estudiantes/CalculoDocentesTests.cs) — 8 períodos del Excel + 6 casos de borde (CES, residuos, invariante)
8. ✅ [EstudiantesView.xaml](../src/Presentation/Views/Estudiantes/EstudiantesView.xaml) Tab 2 — 2 DataTriggers nuevos (#FFF3E0 cursiva) para destacar filas de horas

### ✅ Fase 3 — CargoFacultad + tarifa horaria (completada)
9. ✅ [Migración EF idempotente](../src/Infrastructure/Persistence/Migrations/20260510204418_KAN20b_CargoFacultad_TipoContrato_TarifaHora.cs) + [SQL Supabase](../sql/KAN20b_CargoFacultad_TipoContrato_TarifaHora.sql) — `tipo_contrato VARCHAR(30)` + `tarifa_hora NUMERIC(10,4)` ambas NOT NULL DEFAULT, backfill incluido. **Ejecutado en BD** (verificado: TP id=14 → tarifa=9.0000, sueldo=0)
10. ✅ [CargoFacultad](../src/Domain/Entities/CargoFacultad.cs) Domain entity con `TipoContrato`, `TarifaHora`, setters
11. ✅ [SemillaCapitalTrabajoService.cs](../src/Presentation/Services/SemillaCapitalTrabajoService.cs) — clasifica los 15 cargos con `TipoContrato`; TP usa `TarifaHora = 432/(12×4) = 9.00` y `SueldoBaseMensual = 0`
12. ✅ [CalculoCargosFacultad.cs](../src/Application/UseCases/CargosFacultad/CalculoCargosFacultad.cs) — `EsTiempoParcial` + `CalcularCostoSemestralTiempoParcial` (sin beneficios)
13. ✅ [GenerarTablaSueldosPeriodoQuery.cs](../src/Application/UseCases/CargosFacultad/GenerarTablaSueldosPeriodoQuery.cs) y [GenerarResumenSueldosQuery.cs](../src/Application/UseCases/CargosFacultad/GenerarResumenSueldosQuery.cs) — branch TP via `EsTiempoParcial`
14. ✅ [CU-SP-01.MD](../src/Application/UseCases/SueldosPlantaCentral/CU-SP-01.MD) RN-74/76 + RN-96/97 nuevas; [CU-SP-02.MD](../src/Application/UseCases/SueldosPlantaCentral/CU-SP-02.MD) RN-79 dividida en (a)/(b), RN-83 expandida

### ✅ Fase 4 — Override de horas (completada)
15. ✅ [Tabla override_horas_periodo](../src/Infrastructure/Persistence/Migrations/20260510205433_KAN_OverrideHorasPeriodo.cs) + [SQL Supabase](../sql/KAN_OverrideHorasPeriodo.sql) — FK a `proyeccion_estudiantes`, CHECK periodo 1-20, UNIQUE (proyeccion_id, periodo)
16. ✅ Domain [OverrideHorasPeriodo.cs](../src/Domain/Entities/OverrideHorasPeriodo.cs) + Infra entity + EF config + [RepositorioOverrideHorasPeriodo.cs](../src/Infrastructure/Persistence/Repositories/RepositorioOverrideHorasPeriodo.cs) + 3 use cases ([Editar](../src/Application/UseCases/Estudiantes/EditarConsumoPeriodoUseCase.cs), [Restaurar](../src/Application/UseCases/Estudiantes/RestaurarConsumoPeriodoUseCase.cs), [Listar](../src/Application/UseCases/Estudiantes/ListarOverridesHorasPeriodoUseCase.cs))
17. ✅ [EstudiantesView.xaml](../src/Presentation/Views/Estudiantes/EstudiantesView.xaml) Tab 4 — H. Docencia / H. Práctica editables (resto IsReadOnly=True), `OnConsumoCellEditEnding` valida >0 y dispara `EditarConsumoAsync`. Cascada en VM: edit → use case (BD + auditoría) → re-fetch overrides → `RecalcularConsolidado()` → propaga a Tab 3 / Tab 2 / Tab 4 / módulos consumidores
18. ✅ [CU-ES-04.MD](../src/Application/UseCases/Estudiantes/CU-ES-04.MD) — RN-98..103
19. ✅ ContextMenu "Restaurar valor original" → `RestaurarConsumoPeriodoUseCase` (DELETE)
20. ✅ Auditoría delegada a `IAuditoriaServicio.RegistrarAsync` (fire-and-forget) — sin tabla auxiliar

### ✅ Fase 5 — Resumen Sueldos final (completada)
21. ✅ [ResumenSueldosVistaDto](../src/Application/DTOs/CargosFacultad/ResumenSueldosVistaDto.cs) extendido con `TotalSemestralPeriodoFinal` y `EtiquetaPeriodoFinal`. [ResumenSueldosWindow.xaml](../src/Presentation/Views/CargosFacultad/ResumenSueldosWindow.xaml) — banner inferior con dos columnas: izq "Sueldo semestral · período final" (+ aprox. mensual = ÷6), der "Total acumulado 4 años (suma todos los períodos × 6 meses)"
22. ✅ Anti-duplicación: el `GranTotal` previo se renombró explícitamente a "Total acumulado 4 años" en el banner — es el mismo número, ahora con label inequívoco. **Pendiente fase futura:** Ratio Sueldos/Ingresos (requiere `IngresosPorPeriodo` no disponible en este DTO, deferido)

### ✅ Fase 6 — Trazabilidad tesis (completada)
23. ✅ [`docs/Trazabilidad_RN_Excel.md`](Trazabilidad_RN_Excel.md) — tabla por cada RN modificada (RN-67..73, 94, 95, 96, 97, 98..103) con artefacto en cada capa (CU → Excel → código → test). Resumen ejecutivo para sustentación incluido.
24. ✅ [`docs/actas/2026-05-07-acuerdo-mt-tp.md`](actas/2026-05-07-acuerdo-mt-tp.md) — acta firmada con el docente experto (acuerdos A-D, issues #1-#4 cerrados, tabla de validación de los 8 períodos)

---

## 9. Resumen de decisiones (lookup rápido)

| Tema | Decisión final |
|---|---|
| Algoritmo PhD/Mgs | `floor(total)` + `round(×0.6, AwayFromZero)` + `phd = tc - mgs` |
| Umbral MT | `residuo > 12h` → +1 MT (12h fijas) |
| Algoritmo TP | residuo restante → 1 TP (horas variables hasta 12); >12 → primer TP=12, segundo=resto |
| 1 PhD mínimo CES | umbral paramétrico: aplica si `horasAsistidas ≥ 36 AND tcEntero ≥ 2 AND phd=0` → redistribuye dentro de tcEntero (no infla horas). Default `UmbralPhdMinimoHoras=36m`, configurable. |
| CEILING vs ROUND | manda la tabla esperada (ROUND); RN-71 documenta la nota |
| TP beneficios sociales | NO (servicios profesionales / honorarios) |
| Tarifa $/h | `CargoFacultad.TarifaHora decimal NOT NULL DEFAULT 0` (no nullable, no rompe nada). Helper de inicialización solo en seed. |
| `SueldoBaseMensual` | sin cambio (queda no-nullable). Para TP se setea a 0 en seed/migración. |
| Distinción TP vs resto | por `TipoContrato` enum, no por `null` |
| Override horas | tabla `override_horas_periodo` por proyección |
| Auditoría override | delegada a `auditoria_log` general (fire-and-forget) — no hay tabla auxiliar de historial |
| Edición Tab 4 | recalcula horas+docentes+sueldos+capital. NO matrícula ni retención |
| DTO docentes | extender `FilaDocentePeriodoDto` con `HorasAsignadas decimal[]?` |
| ResumenSueldosWindow | vista 4 años principal + bloque destacado P_final + ratio |
| TipoContrato | enum nuevo (Administrativo, PhD, Mgs, MedioTiempo, TiempoParcial, Tecnico) |
| CU nuevo | CU-ES-04 separado (no flujo alterno de CU-ES-02) |
| Trazabilidad | `docs/Trazabilidad_RN_Excel.md` por cada RN modificada |

---

## 10. Principio rector que guió las decisiones

**Cero regresión sobre código existente.** Cada vez que había dos opciones — una limpia pero invasiva, otra menos elegante pero aditiva — se eligió la aditiva:

- `decimal NOT NULL DEFAULT 0` en lugar de `decimal?` nullable (no toca `GuardiaDominio`)
- Migración aditiva con backfill en lugar de alterar columnas existentes
- Enum nuevo en paralelo a `EsCargoDocente`/`TipoCargo` (no se eliminan en esta fase)
- Auditoría delegada al servicio existente (no se crea tabla auxiliar)
- Algoritmo CES con umbral (no rompe los 8 casos validados del Excel)

Esto garantiza que cada fase del §8 es independientemente reversible y que la app sigue compilando entre fases. La trazabilidad para la tesis se preserva porque cada cambio queda enlazado a una RN, a una celda del Excel, y a un test.
