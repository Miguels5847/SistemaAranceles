# Trazabilidad — Reglas de Negocio ↔ Excel ↔ Código ↔ Tests

**Propósito:** cerrar el ciclo Requisito → Diseño → Implementación → Verificación que sustenta la tesis. Cada RN modificada en el rediseño MT/TP enlaza su artefacto en cada capa.

**Fuente:** Excel "1 Estudiantes" del docente experto del dominio (espec viva) + acta de la reunión `docs/actas/2026-05-07-acuerdo-mt-tp.md`.

---

## RN-67 — Parámetros configurables solo Administrador

| Aspecto              | Artefacto |
|----------------------|-----------|
| Caso de uso          | [CU-ES-03.MD](../src/Application/UseCases/Estudiantes/CU-ES-03.MD) |
| Espec viva (Excel)   | "1 Estudiantes" celdas `J24=18`, `J30=40` (resto inexistentes en Excel original) |
| Constantes en código | [ConstantesDocentes.cs](../src/Domain/Constantes/ConstantesDocentes.cs) — 7 constantes |
| Líneas de código     | `HorasDocenteTC=18`, `HorasMTSemana=12`, `HorasTPMaxSemana=12`, `UmbralResiduoMT=12`, `PorcentajeMgs=0.60`, `SemanasPorMes=4`, `UmbralPhdMinimoHoras=36` |
| Test de regresión    | implícito (todos los tests usan estas constantes) |

---

## RN-68 — Distribución base PhD/Mgs

| Aspecto              | Artefacto |
|----------------------|-----------|
| Espec viva           | "1 Estudiantes!B26 = +B24*0.6"; "B25 = +B26*0.4" (verbalizada por docente) |
| Algoritmo            | `Mgs = round(tcEntero × 0.60, AwayFromZero); PhD = tcEntero - Mgs` |
| Línea de código      | [ConsolidadorProyeccionEstudiantes.cs:42-44](../src/Application/UseCases/Estudiantes/ConsolidadorProyeccionEstudiantes.cs#L42-L44) |
| Test                 | [CalculoDocentesTests.cs](../tests/Application.Tests/Estudiantes/CalculoDocentesTests.cs) `Desglose_CoincideConExcelDelDocente` (8 inline data) |

---

## RN-70 — Garantizar 1 PhD por carrera (norma CES Ecuador)

| Aspecto              | Artefacto |
|----------------------|-----------|
| Origen               | Acta de reunión 2026-05-07 con docente experto |
| Resolución           | Issue #1 — opción (c) umbral paramétrico |
| Algoritmo            | `if horas ≥ 36 AND tcEntero ≥ 2 AND phd = 0 → phd = 1; mgs = tcEntero - 1` |
| Línea de código      | [ConsolidadorProyeccionEstudiantes.cs:46-52](../src/Application/UseCases/Estudiantes/ConsolidadorProyeccionEstudiantes.cs#L46-L52) |
| Verificación         | con default `UmbralPhdMinimoHoras=36`, los 8 períodos del Excel se preservan (P1 con 18h queda en 0 PhD por estar bajo umbral) |
| Test                 | `Ces_NoAplica_BajoUmbral`, `Ces_RespetaInvariante_EnUmbral` |

---

## RN-71 — `floor + round` (no CEILING)

| Aspecto              | Artefacto |
|----------------------|-----------|
| Verbalización docente | "redondear al inmediato superior (CEILING)" |
| Espec viva real      | tabla esperada de los 8 períodos solo casa con `floor(total) + round(×0.6, AwayFromZero)` |
| Decisión             | manda la tabla — RN reescrita con `floor+round` (Issue #2) |
| Línea de código      | [ConsolidadorProyeccionEstudiantes.cs:36-44](../src/Application/UseCases/Estudiantes/ConsolidadorProyeccionEstudiantes.cs#L36-L44) |
| Verificación CEILING falla | con CEILING estricto: P2 (60h) → ceil(3.33)=4 docentes, ceil(4×0.6)=3 mgs, ceil(3×0.4)=2 phd → suma 5 ≠ 4 ❌ |

---

## RN-72 — Distribución del residuo MT/TP

| Aspecto              | Artefacto |
|----------------------|-----------|
| Espec viva           | "1 Estudiantes!B27 = vacía (causa raíz); B28 = +B24-B26-B25 (todo al parcial = bug)" |
| Algoritmo            | residuo > 12 → +1 MT con 12h fijas; sobrante < 12 → +1 TP variable; sobrante > 12 → primer TP llena 12h |
| Línea de código      | [ConsolidadorProyeccionEstudiantes.cs:54-78](../src/Application/UseCases/Estudiantes/ConsolidadorProyeccionEstudiantes.cs#L54-L78) |
| Test                 | `Residuo_MenorIgualUmbralMT_GeneraSoloTP`, `Residuo_MayorUmbralMT_GeneraMTYTP` |
| Tabla de validación  | 8 casos del Excel — todos cuadran |

---

## RN-73 — Datos enviados a Sueldos con tipo + horas TP

| Aspecto              | Artefacto |
|----------------------|-----------|
| DTO ampliado         | [FilaDocentePeriodoDto](../src/Application/DTOs/Estudiantes/ProyeccionConsolidadaDto.cs) `+ decimal[]? HorasAsignadas` |
| Filas emitidas       | "Horas asignadas Medio Tiempo", "Horas asignadas Tiempo Parcial" |
| Línea de código      | [ConsolidadorProyeccionEstudiantes.cs:160-183](../src/Application/UseCases/Estudiantes/ConsolidadorProyeccionEstudiantes.cs#L160-L183) |
| Display              | [EstudiantesView.xaml:286-293](../src/Presentation/Views/Estudiantes/EstudiantesView.xaml#L286-L293) (DataTriggers `#FFF3E0` cursiva) |

---

## RN-74 (CU-SP-01) — Docentes incluyendo Medio Tiempo

| Aspecto              | Artefacto |
|----------------------|-----------|
| Cambio               | Hoy CU-SP-01 lista PhD/Mgs/Parcial/Técnico → ahora también MT |
| Origen del MT real   | RN-72 (consolidador genera mt[p]) |

---

## RN-76 (CU-SP-01) — Beneficios sociales solo PhD/Mgs/MT/Admin

| Aspecto              | Artefacto |
|----------------------|-----------|
| Cambio               | TP NO recibe DXIII/DXIV/Vacaciones/FR/AP (servicios profesionales) |
| Línea de código      | [GenerarTablaSueldosPeriodoQuery.cs:175-194](../src/Application/UseCases/CargosFacultad/GenerarTablaSueldosPeriodoQuery.cs#L175-L194) (branch TP) |
| Línea de código      | [GenerarResumenSueldosQuery.cs:204-211](../src/Application/UseCases/CargosFacultad/GenerarResumenSueldosQuery.cs#L204-L211) (branch TP) |

---

## RN-79 (CU-SP-02) — Fórmula costo dual: PhD/Mgs/MT vs TP

| Aspecto              | Artefacto |
|----------------------|-----------|
| Variante (a) PhD/Mgs/MT/Admin | `(Sueldo + FR + AP) × 6 + DXIII + DXIV + Vacaciones × N × Peso × Inflación` |
| Variante (b) TP      | `TarifaHora × HorasAsignada × 4 × 6 × N × Peso × Inflación` (sin beneficios) |
| Helper               | [CalculoCargosFacultad.cs:7-26](../src/Application/UseCases/CargosFacultad/CalculoCargosFacultad.cs#L7-L26) `EsTiempoParcial`, `CalcularCostoSemestralTiempoParcial` |
| TODO Fase 4 cerrada  | Override de horas conecta `hTP[p]` real desde el consolidador (vía `_overrideHorasDocencia/Practica` en VM) |

---

## RN-94 — Invariante de horas

| Aspecto              | Artefacto |
|----------------------|-----------|
| Invariante           | `PhD×18 + Mgs×18 + MT×12 + TP×hTP = HorasAsistidas` (igualdad exacta) |
| Verificación         | dentro del test `Desglose_CoincideConExcelDelDocente` se valida `Assert.Equal(horas, horasCubiertas)` |
| Cobertura            | 8/8 períodos del Excel preservan la invariante |
| Línea de código      | [CalculoDocentesTests.cs:36-39](../tests/Application.Tests/Estudiantes/CalculoDocentesTests.cs#L36-L39) |

---

## RN-95 — Constantes configurables por Administrador

| Aspecto              | Artefacto |
|----------------------|-----------|
| Ubicación            | [ConstantesDocentes.cs](../src/Domain/Constantes/ConstantesDocentes.cs) |
| TODO futuro          | exponer estas constantes como UI editable por Administrador (no bloquea Fase 1-6) |

---

## RN-96 (CU-SP-01) — `TarifaHora` para TP

| Aspecto              | Artefacto |
|----------------------|-----------|
| Schema               | columna `tarifa_hora NUMERIC(10,4) NOT NULL DEFAULT 0` en `cargo_facultad` |
| Helper de seed       | [CargoFacultad.cs](../src/Domain/Entities/CargoFacultad.cs) `CambiarTarifaHora` |
| Migración            | [KAN20b_CargoFacultad_TipoContrato_TarifaHora.cs](../src/Infrastructure/Persistence/Migrations/20260510204418_KAN20b_CargoFacultad_TipoContrato_TarifaHora.cs) (idempotente, backfill TP) |
| SQL Supabase         | [sql/KAN20b_CargoFacultad_TipoContrato_TarifaHora.sql](../sql/KAN20b_CargoFacultad_TipoContrato_TarifaHora.sql) — ejecutado, backfill confirmado por usuario |
| Backfill TP          | `tarifa_hora = ROUND(sueldo_base_mensual / 48, 4)` — verificado: TP id=14 → tarifa=9.0000 |

---

## RN-97 (CU-SP-01) — Distinción TP vs resto por `TipoContrato` (no nullable)

| Aspecto              | Artefacto |
|----------------------|-----------|
| Decisión             | Issue #4 — opción no destructiva: ambos campos NOT NULL DEFAULT 0, distinción por enum |
| Enum                 | [TipoContrato.cs](../src/Domain/Enums/TipoContrato.cs) |
| Validación           | si `TipoContrato == TiempoParcial` → `TarifaHora > 0` requerido; caso contrario `SueldoBaseMensual > 0` |

---

## RN-98..103 (CU-ES-04) — Override de horas por período

| RN | Artefacto |
|----|-----------|
| RN-98 (validación >0) | [EstudiantesView.xaml.cs:OnConsumoCellEditEnding](../src/Presentation/Views/Estudiantes/EstudiantesView.xaml.cs) |
| RN-99 (granularidad por período) | UNIQUE (proyeccion_id, periodo) en `override_horas_periodo` |
| RN-100 (reset disponible) | [RestaurarConsumoPeriodoUseCase.cs](../src/Application/UseCases/Estudiantes/RestaurarConsumoPeriodoUseCase.cs) + ContextMenu en Tab 4 |
| RN-101 (override prevalece) | `ConsolidadorProyeccionEstudiantes.Calcular(... horasDocSemestralesOverride: ...)` |
| RN-102 (auditoría delegada) | `IAuditoriaServicio.RegistrarAsync` fire-and-forget en use cases |
| RN-103 (cascada no afecta matrícula) | recálculo solo dispara horas → docentes → sueldos → capital de trabajo |

| Aspecto              | Artefacto |
|----------------------|-----------|
| Caso de uso          | [CU-ES-04.MD](../src/Application/UseCases/Estudiantes/CU-ES-04.MD) |
| Migración            | [KAN_OverrideHorasPeriodo.cs](../src/Infrastructure/Persistence/Migrations/20260510205433_KAN_OverrideHorasPeriodo.cs) |
| SQL Supabase         | [sql/KAN_OverrideHorasPeriodo.sql](../sql/KAN_OverrideHorasPeriodo.sql) |
| Tabla                | `override_horas_periodo` (FK proyeccion_estudiantes, CHECK periodo 1-20, UNIQUE (proyeccion_id, periodo)) |
| Use cases            | `EditarConsumoPeriodoUseCase`, `RestaurarConsumoPeriodoUseCase`, `ListarOverridesHorasPeriodoUseCase` |
| UI                   | [EstudiantesView.xaml](../src/Presentation/Views/Estudiantes/EstudiantesView.xaml) Tab 4 editable + ContextMenu |
| VM cascade           | [EstudiantesViewModel.cs](../src/Presentation/ViewModels/Estudiantes/EstudiantesViewModel.cs) `EditarConsumoAsync`, `RestaurarConsumoAsync`, `ConstruirArreglosOverride` |

---

## Vista Resumen Sueldos — Fase 5

| Aspecto              | Artefacto |
|----------------------|-----------|
| Decisión             | spec doc §4.6 — vista 4 años principal + bloque destacado período final |
| DTO                  | [ResumenSueldosVistaDto](../src/Application/DTOs/CargosFacultad/ResumenSueldosVistaDto.cs) `+ TotalSemestralPeriodoFinal`, `+ EtiquetaPeriodoFinal` |
| Query                | [GenerarResumenSueldosQuery.cs](../src/Application/UseCases/CargosFacultad/GenerarResumenSueldosQuery.cs) computa último período |
| UI                   | [ResumenSueldosWindow.xaml](../src/Presentation/Views/CargosFacultad/ResumenSueldosWindow.xaml) — banner inferior con dos columnas: izq "Sueldo semestral · período final" (+ aproximación mensual), der "Total acumulado 4 años" |
| Anti-duplicación     | el GranTotal previo se renombró a "Total acumulado 4 años" — no hay doble cómputo |
| Pendiente fase futura | "Ratio Sueldos/Ingresos" requiere `IngresosPorPeriodo` no disponible en este DTO; deferido |

---

## Suite de tests de regresión

**Archivo:** [tests/Application.Tests/Estudiantes/CalculoDocentesTests.cs](../tests/Application.Tests/Estudiantes/CalculoDocentesTests.cs)

| Test | Cubre RN |
|------|----------|
| `Desglose_CoincideConExcelDelDocente` (8 inline data) | RN-68, RN-71, RN-72, RN-94 |
| `Desglose_HorasNoPositivas_DevuelveCero` (2 inline data) | guardia de entrada |
| `Ces_NoAplica_BajoUmbral` | RN-70 (negativo) |
| `Ces_RespetaInvariante_EnUmbral` | RN-70 + RN-94 |
| `Residuo_MenorIgualUmbralMT_GeneraSoloTP` | RN-72 (camino TP solo) |
| `Residuo_MayorUmbralMT_GeneraMTYTP` | RN-72 (camino MT+TP) |

**Resultado actual:** 14/14 OK · `dotnet test` sin warnings.

---

## Resumen ejecutivo para sustentación

1. **Fuente del requisito:** docente experto del dominio (acta `docs/actas/2026-05-07-acuerdo-mt-tp.md`).
2. **Espec viva:** Excel "1 Estudiantes" — los 8 períodos son la prueba.
3. **Diseño:** `docs/DECISIONES_CALCULO_DOCENTES_MT_TP.md` (este documento + el de decisiones forman el bundle).
4. **Implementación:** 6 fases ejecutadas (Fundamentos → Algoritmo → Cargo TP → Override → Resumen → Trazabilidad).
5. **Verificación:** suite xUnit (14 tests) + 4 issues abiertos cerrados con justificación documentada.
6. **Trazabilidad:** este archivo enlaza cada RN ↔ celda Excel ↔ línea de código ↔ test.

Cualquier RN puede auditarse end-to-end siguiendo su fila en este documento.
