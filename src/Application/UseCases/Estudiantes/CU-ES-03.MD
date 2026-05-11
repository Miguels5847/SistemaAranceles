```md
# Caso de Uso: Calcular docentes requeridos y desglosar por tipo

## Identificación

| Campo            | Valor                                             |
| ---------------- | ------------------------------------------------- |
| Identificador    | CU-ES-03                                          |
| Nombre           | Calcular docentes requeridos y desglosar por tipo |
| RF asociado      | RF-ES-03                                          |
| Actor primario   | Planificador Académico                            |
| Actor secundario | Administrador; Sistema; Consultor                 |

## Descripción

Calcula automáticamente docentes requeridos y los desglosa por tipo.

---

## Flujo Principal — Calcular docentes

1. El usuario accede al módulo.
2. El sistema verifica horas.
3. El Administrador confirma parámetros.
4. El usuario selecciona calcular.
5. El sistema calcula docentes totales.
6. El sistema distribuye por tipo.
7. El sistema calcula técnicos.
8. El sistema aplica redondeo.
9. El sistema guarda y registra auditoría.
10. El sistema muestra tabla.

### Flujos alternos

**2a. No existen horas**

- Mensaje.
- Retorna al inicio.

**3a. Horas docente = 0**

- Se restaura valor.

**3b. Horas técnico = 0**

- Se restaura valor.

**3c. Porcentajes ≠ 100%**

- Advertencia.

**8a. Mgs = 0**

- Ajuste automático.

---

## Flujo alterno — Configurar parámetros

1. Administrador accede parámetros.
2. Modifica valores.
3. Sistema valida.
4. Sistema guarda.
5. Sistema recalcula.

---

## Precondiciones

- Usuario autenticado.
- Horas calculadas.

## Postcondiciones

- Docentes calculados.
- Datos disponibles para sueldos.
- Auditoría registrada.

## Reglas de negocio

- **RN-67:** Solo el Administrador puede modificar los parámetros configurables: `HorasDocenteTC` (18), `HorasMTSemana` (12), `HorasTPMaxSemana` (12), `UmbralResiduoMT` (12), `PorcentajeMgs` (0.60), `UmbralPhdMinimoHoras` (36) y `TarifaHora` por cargo Tiempo Parcial.

- **RN-68:** Distribución base por período: `Mgs = round(tcEntero × 0.60, AwayFromZero)`; `PhD = tcEntero − Mgs` (residual entero, no porcentaje sobre el total). El cálculo del Excel del docente con porcentaje 40% sobre Mgs queda subsumido por esta fórmula.

- **RN-69:** Fórmulas de cálculo en cascada por período `p`:
  - `totalDec = horasAsistidas[p] / HorasDocenteTC`
  - `tcEntero = floor(totalDec)`
  - `residuoH = (totalDec − tcEntero) × HorasDocenteTC`
  - Distribución MT/TP cubre el residuo (ver RN-72).

- **RN-70:** Garantizar 1 Mgs siempre que `tcEntero ≥ 1`. **Garantizar 1 PhD cuando `horasAsistidas ≥ UmbralPhdMinimoHoras` Y `tcEntero ≥ 2` Y `phd` calculado = 0** (norma CES Ecuador). En ese caso se redistribuye dentro de `tcEntero` (`mgs = tcEntero − 1`), preservando la invariante de horas. Por debajo del umbral la regla CES no aplica para no inflar horas asignadas en períodos iniciales.

- **RN-71:** `tcEntero = floor(horasAsistidas / 18)`. `Mgs = round(tcEntero × 0.60, AwayFromZero)`. `PhD = tcEntero − Mgs`. **NOTA:** la verbalización original del docente fue "redondear al inmediato superior (CEILING)", pero la tabla esperada de los 8 períodos del Excel solo casa con esta combinación `floor + round`; **manda la tabla** (validada por test de regresión).

- **RN-72:** Distribución del residuo de horas:
  - Si `residuoH > UmbralResiduoMT (12)` → `+1 Medio Tiempo` con `HorasMTSemana (12)` fijas.
  - Sobrante = `residuoH − HorasMT`.
  - Si sobrante `> 0` y `≤ HorasTPMaxSemana (12)` → `+1 Tiempo Parcial` con horas variables.
  - Si sobrante `> HorasTPMaxSemana` → primer TP llena 12h, siguientes TP toman el resto (no se reparte por igual).

- **RN-73:** Personas (PhD, Mgs, MT, TP) se envían al módulo Sueldos con su `TipoContrato`. Para TP se envía además `HorasAsignadas` (decimal) por período, que se usa para pago `TarifaHora × HorasAsignadas × SemanasPorMes × 6` (sin beneficios sociales).

- **RN-94:** Invariante de horas: `PhD×18 + Mgs×18 + MT×12 + TP×hTP = HorasAsistidas` (igualdad exacta). Validada por test de regresión contra los 8 períodos del Excel del docente experto.

- **RN-95:** Las constantes `HorasMTSemana`, `HorasTPMaxSemana`, `UmbralResiduoMT`, `PorcentajeMgs` y `UmbralPhdMinimoHoras` viven en `Domain/Constantes/ConstantesDocentes.cs` y son configurables únicamente por el Administrador (RN-67).
```
