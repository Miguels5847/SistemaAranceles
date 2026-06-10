# Acta — Acuerdo sobre cálculo de docentes MT/TP, override de horas y tarifa horaria

**Fecha:** 2026-05-07
**Participantes:**
- Docente experto del dominio (referente académico/Excel "1 Estudiantes")
- Equipo de desarrollo (tesis SistemaAranceles)

**Contexto:** revisión del modelo financiero de la carrera. Se identificaron tres problemas en la hoja "1 Estudiantes" del Excel del docente:

1. El cálculo de docentes requeridos arroja decimales que no se pueden contratar (no existe "0.33 de docente"). El residuo decimal cae completo al Tiempo Parcial pagado como sueldo fijo, generando descuadres documentados (`J26 = 56.8 ≠ 60` horas; `L27 = -9.6` para 2023).
2. La fila "Medio Tiempo" (B27) está vacía — todo el residuo fluye al Parcial.
3. Las horas semestrales hardcoded (`B16:I16`, `B20:I20`) deberían ser editables por período.

---

## Acuerdos cerrados

### A. Algoritmo MT/TP (CU-ES-03 RN-68..72, RN-94)

- **Total docentes** = `floor(horas_asistidas / 18)` (no CEILING).
  - El docente verbalizó "redondear al inmediato superior" pero su tabla esperada de los 8 períodos solo casa con `floor + round`. **Manda la tabla.**
- **Mgs** = `round(tcEntero × 0.60, AwayFromZero)`.
- **PhD** = `tcEntero − Mgs` (residual entero).
- **Regla CES (1 PhD obligatorio):** aplica si `horas ≥ 36 AND tcEntero ≥ 2 AND phd_calc = 0`. Por debajo del umbral no se fuerza para no romper la invariante de horas.
  - Default: `UmbralPhdMinimoHoras = 36` (= 2 docentes TC).
- **Residuo de horas** = `(totalDec − tcEntero) × 18`.
  - Si `residuo > 12` → +1 Medio Tiempo con 12h fijas.
  - Sobrante `< 12` → +1 Tiempo Parcial con horas variables.
  - Sobrante `> 12` → primer TP llena 12h, siguientes TP toman el resto (no se reparte por igual).
- **Invariante:** `PhD×18 + Mgs×18 + MT×12 + TP×hTP = HorasAsistidas` (igualdad exacta, validada en test).

### B. Tarifa horaria para Tiempo Parcial (CU-SP-01 RN-96, RN-97)

- TP se contrata por **servicios profesionales** — NO recibe DXIII / DXIV / Vacaciones / Fondo Reserva / Aporte Patronal.
- Costo TP = `TarifaHora × HorasAsignadasSemana × SemanasPorMes × 6`.
- Inicialización: `TarifaHora = SueldoMensualTP / (HorasTPMaxSemana × SemanasPorMes)` (12 × 4 = 48). Tras inicializar, `SueldoBaseMensual = 0` para TP.
- Distinción TP vs no-TP por enum `TipoContrato`, NO por `null`. Ambos campos NOT NULL DEFAULT 0 (decisión no destructiva — Issue #4).

### C. Edición de Tab 4 "Consumo por período" (CU-ES-04)

- El docente puede editar las celdas `H. Docencia` y `H. Práctica` en la pestaña "4. Consumo por período".
- El cambio se persiste en tabla `override_horas_periodo` (FK a `proyeccion_estudiantes`).
- La cascada recalcula automáticamente: Tab 3 (Horas) → Tab 2 (Docentes) → Sueldos → Capital de Trabajo.
- **NO afecta:** matrícula, tasa de retención, períodos previos al editado.
- Botón "Restaurar valor original" (click derecho) elimina el override → vuelve al cálculo automático.
- Auditoría delegada a `IAuditoriaServicio.RegistrarAsync` (fire-and-forget). NO se duplica historial en la tabla override.

### D. Vista Resumen Sueldos (CU-SP-02 / Fase 5)

- La vista por defecto muestra el pivot completo de los 4 años (todos los períodos).
- El último período (P_n) aparece en un bloque destacado en el footer (sueldo semestral + aproximación mensual).
- "GRAN TOTAL" se renombró a "Total acumulado 4 años (suma todos los períodos × 6 meses)" para evitar ambigüedad.

---

## Issues que se cerraron en esta reunión

| # | Conflicto | Decisión |
|---|-----------|----------|
| 1 | CES "1 PhD mínimo" rompe invariante en P1 (18h, tcEntero=1) | Opción (c): umbral paramétrico `UmbralPhdMinimoHoras = 36` por default |
| 2 | "CEILING" verbalizado vs "floor+round" de la tabla | Manda la tabla (ROUND) — RN-71 documenta la nota |
| 3 | Auditoría inline vs delegada | Delegar a `auditoria_log` general (fire-and-forget). No tabla auxiliar |
| 4 | `SueldoBaseMensual` nullable vs no-nullable | No-nullable + agregar `TarifaHora` no-nullable (no rompe `GuardiaDominio`) |

---

## Datos de validación entregados por el docente (8 períodos del Excel)

| Período | Horas | PhD | Mgs | MT | TP | hMT | hTP |
|---|---|---|---|---|---|---|---|
| P1 (2023A) | 18  | 0 | 1 | 0 | 0 |  0 | 0 |
| P2 (2023B) | 60  | 1 | 2 | 0 | 1 |  0 | 6 |
| P3 (2024A) | 80  | 2 | 2 | 0 | 1 |  0 | 8 |
| P4 (2024B) | 122 | 2 | 4 | 1 | 1 | 12 | 2 |
| P5 (2025A) | 143 | 3 | 4 | 1 | 1 | 12 | 5 |
| P6 (2025B) | 183 | 4 | 6 | 0 | 1 |  0 | 3 |
| P7 (2026A) | 202 | 4 | 7 | 0 | 1 |  0 | 4 |
| P8 (2026B) | 234 | 5 | 8 | 0 | 0 |  0 | 0 |

Estos 8 valores son la espec viva. Cualquier desviación del algoritmo respecto a esta tabla es un bug.

---

## Próximos pasos

1. Implementar el algoritmo siguiendo este acta (Fases 1-6 del plan en `docs/DECISIONES_CALCULO_DOCENTES_MT_TP.md`).
2. Generar suite de tests con los 8 períodos como inline data.
3. Migrar la BD Supabase con SQL idempotente.
4. Cerrar el ciclo de trazabilidad en `docs/Trazabilidad_RN_Excel.md`.

---

**Firma de cierre:** acta acordada y validada con el docente. Implementación autorizada para iniciar Fase 1.
