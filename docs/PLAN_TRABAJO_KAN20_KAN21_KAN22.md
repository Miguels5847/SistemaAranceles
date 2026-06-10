# Plan de Trabajo Integral — KAN-20, KAN-21, KAN-22

**Fecha:** 2026-05-11  
**Estado:** ✅ KAN-20 COMPLETADO | KAN-21/22 Pendiente  
**Basado en:**

- `DECISIONES_CALCULO_DOCENTES_MT_TP.md` (Fases 1-6 completadas, Fase 5 = hTP real futuro)
- Requerimientos del docente experto (bloques: Sueldos, Horas, Malla, Resumen)
- Checklist técnico de pendientes (hTP reales, DTOs, tests, BD)

---

## 0. Síntesis: Lo que el docente pidió

### Núcleo 1: Lógica de contratación real (Tiempo Completo / Medio Tiempo / Tiempo Parcial)

- **TC:** 40h contratadas, puede dictar <40h (ej. 18h), pago fijo.
- **MT:** 20h contratadas, dicta ~12h, pago fijo.
- **TP:** pago por hora, máx 12h/semana, absorbe residuo.
- **Regla CES:** 1 PhD mínimo cuando hay capacidad (≥2 TC).
- **Redondeo:** internamente decimal, pero contratación entero hacia arriba (CEILING).
- **Cobertura:** el total de docentes × horas debe = horas asistidas (invariante).

### Núcleo 2: Edición manual de horas (Tab 4, Consumo por período)

- Horas de docencia y práctica editables por período.
- Override persiste en BD y recalcula cascada.
- Dos modos: automático (desde malla) y manual editable (usuario autorizado).

### Núcleo 3: Malla curricular como fuente de verdad

- Valores base (horas clase, semanas, factor práctica).
- Afecta: Estudiantes → Consumo → Horas → Docentes → Sueldos.
- Versionado por carrera/período.

### Núcleo 4: Visibilidad por rol

- Datos sensibles solo para administrador.
- Edición restringida por autorización.

### Núcleo 5: Resumen final desde período final (Pn)

- No promedio, no P1, sí último período.
- Refleja estado final real de la carrera.

---

## 1. KAN-20: Completar backend de Cargos + Tarifa TP (RAMA: main)

**Estado FINAL:** ✅ COMPLETADO

### 1.0 Resumen de Implementación Completada

**Fase de Codificación:**

1. ✅ DTOs extendidos: `CrearCargoFacultadDto` y `ActualizarCargoFacultadDto` con `TipoContrato` y `TarifaHora`
2. ✅ Entidad `CargoFacultad`: constructor ampliado con parámetros opcionales para nuevo tipo/tarifa (compatible backward)
3. ✅ Comandos actualizados:
   - `AgregarCargoFacultadCommand`: pasa nuevos parámetros + validación RN-96/97
   - `ActualizarCargoFacultadCommand`: setters + validación RN-96/97
4. ✅ Validación de contrato por tipo:
   - **TP**: `TarifaHora > 0` AND `SueldoBaseMensual = 0`
   - **No-TP**: `SueldoBaseMensual > 0` AND `TarifaHora = 0`

**Tests Creados (12/12 PASADOS):**

- ✅ `PagoTiempoParcialsTests.cs` (6 tests):
  - Base sin inflación: 10 × 6h × 4 × 6 = 1440 ✓
  - Con inflación 5%: 1512 ✓
  - Con peso 0.5: 720 ✓
  - Con 2 personas: 2880 ✓
  - Con inflación + peso: 1209.6 ✓
  - Zero horas: 0 ✓

- ✅ `ValidacionContratoPorTipoTests.cs` (6 tests):
  - TP sin TarifaHora → excepción ✓
  - TP con SueldoBase ≠ 0 → excepción ✓
  - TC/Admin sin SueldoBase → excepción ✓
  - TC/Admin con TarifaHora → excepción ✓
  - TP válido → éxito ✓
  - TC válido → éxito ✓

**Compilación:**

- ✅ Build completo sin errores
- ✅ No hay warnings

**Base de Datos:**

- ✅ Migración `KAN20b_CargoFacultad_TipoContrato_TarifaHora` ya aplicada
- ✅ Backfill confirmado: TP id=14 con tarifa_hora=9.0000

**Documentación CU/RN:**

- ✅ RN-96 agregada: validación TP con TarifaHora > 0 y SueldoBaseMensual = 0
- ✅ RN-97 agregada: validación no-TP con SueldoBaseMensual > 0 y TarifaHora = 0

**Nota sobre hTP real (TODO Fase 5):**
Las queries `GenerarTablaSueldosPeriodoQuery` y `GenerarResumenSueldosQuery` aún usan `HorasTPMaxSemana` como fallback. El consolidador requiere parámetros adicionales (paralelos, tasas) que no están disponibles en contexto query. Implementar hTP real requiere refactorización mayor de acceso al consolidador → **Fase 5 (post-KAN-22)**.

---

### DEPRECATED CONTENT BELOW — Mantener para referencia histórica

**Descripción anterior:** Migración BD aplicada, Entity actualizada, helpers en `CalculoCargosFacultad`, queries branching por TP. **Pendiente:** exponer `TipoContrato` + `TarifaHora` en DTOs/comandos, consumir `hTP` real, tests, validaciones.

### 1.1 Tareas de código

#### 1.1.1 Extender DTOs de CrearCargoFacultad y ActualizarCargoFacultad

**Archivo:** `src/Application/DTOs/CargosFacultad/CrearCargoFacultadDto.cs`

```csharp
public sealed class CrearCargoFacultadDto
{
    public int CarreraId { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public string TipoCargo { get; init; } = string.Empty;
    public decimal SueldoBaseMensual { get; init; }
    public bool EsCargoDocente { get; init; }
    public decimal CantidadDefault { get; init; } = 1m;
    // NUEVO:
    public TipoContrato TipoContrato { get; init; } = TipoContrato.Administrativo;
    public decimal TarifaHora { get; init; } = 0m;
}
```

**Archivo:** `src/Application/DTOs/CargosFacultad/ActualizarCargoFacultadDto.cs`

```csharp
public sealed class ActualizarCargoFacultadDto
{
    public int Id { get; init; }
    public int CarreraId { get; init; }
    public string NombreCargo { get; init; } = string.Empty;
    public string TipoCargo { get; init; } = string.Empty;
    public decimal SueldoBaseMensual { get; init; }
    public bool EsCargoDocente { get; init; }
    public decimal CantidadDefault { get; init; } = 1m;
    // NUEVO:
    public TipoContrato TipoContrato { get; init; } = TipoContrato.Administrativo;
    public decimal TarifaHora { get; init; } = 0m;
}
```

#### 1.1.2 Actualizar comandos para usar nuevas propiedades

**Archivo:** `src/Application/UseCases/CargosFacultad/AgregarCargoFacultadCommand.cs`

- Cambiar: pasar `TipoContrato` y `TarifaHora` desde DTO al constructor de `CargoFacultad`.

**Archivo:** `src/Application/UseCases/CargosFacultad/ActualizarCargoFacultadCommand.cs`

- Cambiar: agregar llamadas `cargo.CambiarTipoContrato(dto.TipoContrato)` y `cargo.CambiarTarifaHora(dto.TarifaHora)`.

#### 1.1.3 Validación: TipoContrato + TarifaHora / SueldoBaseMensual

**Dónde:** agregador en los comandos o factory en `CargoFacultad`:

```csharp
// En comando, después de obtener/crear cargo:
if (dto.TipoContrato == TipoContrato.TiempoParcial)
{
    if (dto.TarifaHora <= 0m)
        throw new ArgumentException("TP requiere TarifaHora > 0.");
    if (dto.SueldoBaseMensual != 0m)
        throw new ArgumentException("TP debe tener SueldoBaseMensual = 0.");
}
else
{
    if (dto.SueldoBaseMensual <= 0m)
        throw new ArgumentException($"{dto.TipoContrato} requiere SueldoBaseMensual > 0.");
}
```

#### 1.1.4 Consumir hTP real en GenerarTablaSueldosPeriodoQuery y GenerarResumenSueldosQuery

**Archivo:** `src/Application/UseCases/CargosFacultad/GenerarTablaSueldosPeriodoQuery.cs`

**Cambio (línea ~180, en método `Calcular`):**

- Pasar `ProyeccionConsolidadaDto` como parámetro adicional (ya obtenido en `EjecutarAsync`).
- Extraer `hTPArr = proyeccion.DocentesPorPeriodo.FirstOrDefault(f => f.Tipo == "Horas asignadas Tiempo Parcial")?.HorasAsignadas`.
- Cambiar `CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(..., ConstantesDocentes.HorasTPMaxSemana, ...)` → `(..., hTPArr?[periodIndex] ?? ConstantesDocentes.HorasTPMaxSemana, ...)`.

**Archivo:** `src/Application/UseCases/CargosFacultad/GenerarResumenSueldosQuery.cs`

**Cambio (línea ~210, en método `CalcularTotalSemestre`):**

- Similar: extraer `hTPArr` del DTO.
- Pasar índice correcto del período para obtener `hTP[p]`.

**Nota importante:** `ProyeccionConsolidadaDto` ya contiene `hTPArr` en `FilaDocentePeriodoDto` (Fase 2 ✅), así que solo hay que pasarlo y consumirlo.

#### 1.1.5 Tests: pago TP por hora y impact de override

**Archivo nuevo:** `tests/Application.Tests/CargosFacultad/PagoTiempoParcialsTests.cs`

```csharp
[Fact]
public void CalcularCostoSemestralTiempoParcial_DaResultadoEsperado()
{
    // Arrange: TarifaHora=10, hTP=6, personas=1, peso=1.0, inflación=1.0
    // Esperado: 10 * 6 * 4 * 6 = 1440 (6 horas/semana × 4 semanas × 6 meses)

    // Act
    var costo = CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
        tarifaHora: 10m,
        horasAsignadasSemana: 6m,
        personas: 1m,
        pesoProporcional: 1m,
        factorInflacion: 1m);

    // Assert
    Assert.Equal(1440m, costo);
}

[Fact]
public void CalcularCostoSemestralTiempoParcial_ConInflacion()
{
    // Arrange: TarifaHora=10 × 1.05 inflación = 10.5
    // Esperado: 10.5 * 6 * 4 * 6 = 1512

    // Act
    var costo = CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
        tarifaHora: 10m,
        horasAsignadasSemana: 6m,
        personas: 1m,
        pesoProporcional: 1m,
        factorInflacion: 1.05m);

    // Assert
    Assert.Equal(1512m, costo);
}

[Fact]
public void GenerarResumenSueldosQuery_UsaHTPFromProyeccion()
{
    // Arrange: mock proyección con hTP=[0, 6, 8, 2, ...] para varios períodos
    // Verificar que costo TP período[1] usa hTP=6, período[2] usa hTP=8, etc.

    // Act
    var resumen = await query.EjecutarAsync(...);

    // Assert
    // Verificar valores específicos del cargo TP en cada columna
    var cargoTP = resumen.Filas.First(f => f.NombreCargo == "Tiempo Parcial");
    Assert.Equal(expectedValue1, cargoTP.ValoresPorPeriodo[1]); // período 2
    Assert.Equal(expectedValue2, cargoTP.ValoresPorPeriodo[2]); // período 3
}
```

**Archivo nuevo:** `tests/Application.Tests/CargosFacultad/OverrideTablaSueldosImpactTests.cs`

```csharp
[Fact]
public async Task EditarOverrideHoras_ImpactaTotalSueldosTP()
{
    // Arrange: proyección con cargos, 1 TP con tarifa=9.0, período con hTP=12 por defecto
    // Usuario edita Tab4: cambia hTP de 12 a 6

    // Act
    await editarConsumoPeriodoUseCase.EjecutarAsync(new EditarConsumoPeriodoUseCase.Command
    {
        ProyeccionId = proyeccionId,
        Periodo = 2,
        HorasDocencia = 18, // sin cambio
        HorasPractica = null // sin cambio
        // (El override real sería en otra colección si es para MT/TP específicamente)
    });

    // Re-generar consolidado
    var nuevoConsolidado = ConsolidadorProyeccionEstudiantes.Calcular(...);

    // Act: re-generar tabla sueldos
    var nuevoResumen = await query.EjecutarAsync(...);

    // Assert: verificar que costo TP período 2 cambió
    var cargoTP = nuevoResumen.Filas.First(f => f.NombreCargo == "Tiempo Parcial");
    Assert.Equal(expectedNewValue, cargoTP.ValoresPorPeriodo[1]); // período 2
}
```

### 1.2 Tareas de CU/RN

**CU-SP-01** — Configurar cargos (ya actualizado en Fase 3, **verificar**):

- ✅ RN-74: incluir Medio Tiempo
- ✅ RN-76: aclarar sin beneficios para TP
- ❓ RN-96/97: NUEVAS — validar TipoContrato + TarifaHora (revisar si están en CU-SP-01.MD)

**Cambio necesario en CU-SP-01.MD:**

```markdown
### 4.2. Validación de campos por TipoContrato

**RN-96:** Cargo con `TipoContrato == TiempoParcial`:

- Requiere `TarifaHora > 0`.
- `SueldoBaseMensual` debe ser `0m` (no aplicable).

**RN-97:** Cargo con `TipoContrato ∈ {PhD, Mgs, MedioTiempo, Administrativo, Tecnico}`:

- Requiere `SueldoBaseMensual > 0`.
- `TarifaHora` debe ser `0m` (no aplicable).

**RN-98:** Helper de inicialización para TP:
```

Si usuario ingresa sueldo equivalente mensual para TP:
TarifaHora = SueldoEquivalenteMensual / (HorasTPSemana × SemanasPorMes)
SueldoBaseMensual = 0

```

```

**CU-SP-02** — Calcular sueldos facultad (ya actualizado en Fase 3, **verificar**):

- ✅ RN-79: dividida en (a) y (b)
- ✅ RN-83: disparadores de recálculo
- ❓ TODO Fase 4 comment en código (buscar "TODO Fase 4: hTP[p] real desde consolidador")

**Cambio necesario en código:**

```csharp
// Línea ~210 en GenerarTablaSueldosPeriodoQuery.cs
// Cambiar comentario:
// ANTES: // TODO Fase 4: hTP[p] real desde consolidador via override; por ahora HorasTPMaxSemana.
// DESPUÉS: // RN-79b: hTP[p] del consolidador via override; fallback HorasTPMaxSemana si no existe.
```

### 1.3 Tareas de base de datos

**Verificación (ejecutar en Supabase):**

```sql
-- Confirmar migración aplicada
SELECT version FROM __efmigrationshistory
WHERE migration_id LIKE '%KAN20b%';
-- Esperado: 20260510204418_KAN20b_CargoFacultad_TipoContrato_TarifaHora

-- Confirmar backfill
SELECT id, nombre_cargo, tipo_contrato, sueldo_base_mensual, tarifa_hora
FROM cargo_facultad
WHERE id = 14; -- Tiempo Parcial
-- Esperado: tipo_contrato='TiempoParcial', tarifa_hora > 0, sueldo_base_mensual=0
```

**Backup y estado actual:**

- Migración: ✅ KAN20b_CargoFacultad_TipoContrato_TarifaHora — aplicada en BD
- Backfill: ✅ TP id=14 tiene tarifa_hora=9.0000, sueldo=0

### 1.4 Deliverables de KAN-20

| Entregable             | Archivo                                                               | Estado                       |
| ---------------------- | --------------------------------------------------------------------- | ---------------------------- |
| DTO Crear + Actualizar | `CrearCargoFacultadDto.cs`, `ActualizarCargoFacultadDto.cs`           | ⏳ Pendiente                 |
| Comandos + validación  | `AgregarCargoFacultadCommand.cs`, `ActualizarCargoFacultadCommand.cs` | ⏳ Pendiente                 |
| hTP real consumo       | `GenerarTablaSueldosPeriodoQuery.cs`, `GenerarResumenSueldosQuery.cs` | ⏳ Pendiente                 |
| Tests TP pago          | `PagoTiempoParcialsTests.cs`, `OverrideTablaSueldosImpactTests.cs`    | ⏳ Pendiente                 |
| Actualización CU       | `CU-SP-01.MD`, `CU-SP-02.MD`                                          | ⏳ Pendiente (revisar notas) |
| Verificación BD        | Script SQL confirmación                                               | ⏳ Ejecutar                  |

---

## 2. KAN-21: Peso proporcional + Inflación (RAMA: feature/KAN-21-Peso-Inflacion)

**Estado actual:** rama creada, lista para trabajo.  
**Objetivo:** implementar peso proporcional por período en cargos administrativos y aplicar inflación encadenada en sueldos.

### 2.1 Tareas de código

#### 2.1.1 Implementar peso proporcional en CalculoCargosFacultad

**Archivo:** `src/Application/UseCases/CargosFacultad/CalculoCargosFacultad.cs`

**Ya existe:**

```csharp
public static decimal CalcularPeso(
    CargoFacultad cargo,
    decimal estudiantesCarreraPeriodo,
    decimal estudiantesUnidadAcademica)
{
    if (EsPesoFijo(cargo))
        return 1m;

    var denominador = estudiantesUnidadAcademica + estudiantesCarreraPeriodo;
    if (denominador <= 0m)
        return 0m;

    return Math.Round(estudiantesCarreraPeriodo / denominador, 4);
}
```

**Verificar:**

- ✅ Métodos `EsPesoFijo()` cubriendo docentes y "Director de Carrera".
- ✅ Cálculo para administrativos: `peso = est_carrera / (est_UA + est_carrera)`.
- ✅ Redondeo a 4 decimales.

**Cambio esperado:** Nada, ya está correcto. Pero hay que verificar que las queries lo usen por período, no solo P1.

#### 2.1.2 Verificar inflación encadenada en queries

**Archivo:** `src/Application/UseCases/CargosFacultad/GenerarTablaSueldosPeriodoQuery.cs`

**Ya existe (líneas ~130-160):**

```csharp
private async Task<(decimal Factor, decimal PorcentajePeriodo)> CalcularFactorEncadenadoAsync(...)
{
    // Factor = ∏ (1 + infl_y/100) para y desde anioBase hasta anioPeriodo-1,
    // y se multiplica por (1 + infl_anioPeriodo/100) si numeroPeriodo >= 2.
}
```

**Verificar:**

- ✅ Lógica: anios previos 100%, año actual 100% si P1, 100% si P2+.
- ✅ Redondeo a 6 decimales.
- ✅ Fallback: factor < 1 → 1.

**Cambio esperado:** Nada, ya correcto. Pero hay que confirmar que se use en todas las ramas.

#### 2.1.3 Actualizar GenerarResumenSueldosQuery para usar peso por período

**Archivo:** `src/Application/UseCases/CargosFacultad/GenerarResumenSueldosQuery.cs`

**Cambio (línea ~125, en loop de períodos):**

ANTES:

```csharp
var peso = CalculoCargosFacultad.CalcularPeso(
    cargo, estCarrera, parametros.EstudiantesUnidadAcademica);

var total = CalcularTotalSemestre(cargo, parametros);
valores[i] = total;
```

DESPUÉS:

```csharp
var peso = CalculoCargosFacultad.CalcularPeso(
    cargo, estCarrera, parametros.EstudiantesUnidadAcademica);

var total = CalcularTotalSemestreConPeso(cargo, parametros, peso);
valores[i] = total;
```

**Nueva función (agregar a GenerarResumenSueldosQuery.cs):**

```csharp
private static decimal CalcularTotalSemestreConPeso(
    CargoFacultad cargo,
    ParametrosCalculoCargoFacultadDto parametros,
    decimal peso)
{
    if (CalculoCargosFacultad.EsTiempoParcial(cargo))
    {
        var tarifaAjustada = Math.Round(cargo.TarifaHora * parametros.FactorInflacion, 4);
        return CalculoCargosFacultad.CalcularCostoSemestralTiempoParcial(
            tarifaAjustada, 12m, cargo.CantidadDefault, peso, 1m); // peso ya en parámetro
    }

    var sueldoMensualAjustado = Math.Round(cargo.SueldoBaseMensual * parametros.FactorInflacion, 2);
    // ... resto igual, pero multiplicar por peso al final
    return Math.Round(costoBaseSemestral * cargo.CantidadDefault * peso, 2);
}
```

#### 2.1.4 Tests: peso proporcional e inflación

**Archivo nuevo:** `tests/Application.Tests/CargosFacultad/PesoProporcionalTests.cs`

```csharp
[Fact]
public void CalcularPeso_ParaDocenteEsUno()
{
    // Arrange: cargo con EsCargoDocente=true
    var cargo = new CargoFacultad(carreraId, "PhD", "PhD", 3000, esCargoDocente: true);

    // Act
    var peso = CalculoCargosFacultad.CalcularPeso(cargo, 100, 50);

    // Assert
    Assert.Equal(1m, peso);
}

[Fact]
public void CalcularPeso_ParaAdminstrativoEsProporcional()
{
    // Arrange: cargo administrativo, est_carrera=100, est_UA=200
    // Esperado: peso = 100 / (200 + 100) = 0.3333
    var cargo = new CargoFacultad(carreraId, "Coordinador", "Administrativo", 2000, esCargoDocente: false);

    // Act
    var peso = CalculoCargosFacultad.CalcularPeso(cargo, 100, 200);

    // Assert
    Assert.Equal(0.3333m, peso, precision: 4);
}

[Fact]
public void CalcularPeso_ConEstudiantes_Cero_RetornaCero()
{
    // Arrange
    var cargo = new CargoFacultad(carreraId, "Secretario", "Administrativo", 1000, esCargoDocente: false);

    // Act
    var peso = CalculoCargosFacultad.CalcularPeso(cargo, 0, 0);

    // Assert
    Assert.Equal(0m, peso);
}
```

**Archivo nuevo:** `tests/Application.Tests/CargosFacultad/InflacionEncadenada​Tests.cs`

```csharp
[Fact]
public void CalcularFactorEncadenado_UnAño_SinInflacion()
{
    // Arrange: 2026A (P1), inflación 2026 = 2%
    // P1 no aplica inflación actual → factor = 1.0
    var registros = new List<InflacionAnual>
    {
        new() { Anio = 2026, PorcentajeInflacion = 2m, TipoFuente = "oficial" }
    };

    // Act
    var (factor, pct) = CalcularFactorEncadenadoAsync(anioBase: 2026, anioPeriodo: 2026, numeroPeriodo: 1, registros).Result;

    // Assert
    Assert.Equal(1.0m, factor);
    Assert.Equal(2m, pct);
}

[Fact]
public void CalcularFactorEncadenado_UnAño_ConInflacionP2()
{
    // Arrange: 2026B (P2), inflación 2026 = 2%
    // P2 sí aplica inflación → factor = 1 + 0.02 = 1.02
    var registros = new List<InflacionAnual>
    {
        new() { Anio = 2026, PorcentajeInflacion = 2m, TipoFuente = "oficial" }
    };

    // Act
    var (factor, pct) = CalcularFactorEncadenadoAsync(anioBase: 2026, anioPeriodo: 2026, numeroPeriodo: 2, registros).Result;

    // Assert
    Assert.Equal(1.02m, factor);
}

[Fact]
public void CalcularFactorEncadenado_DosAños_Completo()
{
    // Arrange: 2027A (P3), infl 2026=2%, infl 2027=3%
    // factor = (1 + 0.02) × (1 + 0.03) = 1.0506
    var registros = new List<InflacionAnual>
    {
        new() { Anio = 2026, PorcentajeInflacion = 2m },
        new() { Anio = 2027, PorcentajeInflacion = 3m }
    };

    // Act
    var (factor, pct) = CalcularFactorEncadenadoAsync(anioBase: 2026, anioPeriodo: 2027, numeroPeriodo: 1, registros).Result;

    // Assert
    Assert.Equal(1.0506m, factor, precision: 4);
}
```

### 2.2 Tareas de CU/RN

**CU-SP-02** — Calcular sueldos facultad (**ya actualizado**, verificar):

- ✅ RN-79: fórmula con peso
- ❓ RN-81/82: NUEVAS — inflación encadenada por período (revisar si están documentadas)

**Cambio necesario en CU-SP-02.MD (si no está):**

```markdown
### 5. Inflación encadenada por período (RN-81, RN-82)

**RN-81:** Factor de inflación encadenado
```

factor = ∏\_{y=anioBase}^{anioPeriodo-1} (1 + infl_y / 100)
× (1 + infl_anioPeriodo / 100) si numeroPeriodo >= 2

```

**RN-82:** Aplicación a sueldos:
- Sueldo mensual ajustado = Sueldo base × factor
- Beneficios ajustados por factor
- Total semestral = costo base × factor × peso × personas
```

### 2.3 Deliverables de KAN-21

| Entregable                               | Archivo                                                  | Estado                       |
| ---------------------------------------- | -------------------------------------------------------- | ---------------------------- |
| Verificar CalcularPeso                   | `CalculoCargosFacultad.cs`                               | ✅ Verificado                |
| Verificar inflación encadenada           | `CalculoCargosFacultad.cs` + queries reutilizando helper | ✅ Verificado                |
| Actualizar GenerarResumen con peso por p | `GenerarResumenSueldosQuery.cs`                          | ✅ Implementado              |
| Tests peso                               | `Kan21PesoInflacionTests.cs`                             | ✅ Implementado              |
| Tests inflación                          | `Kan21PesoInflacionTests.cs`                             | ✅ Implementado              |
| Actualización CU                         | `CU-SP-02.MD`                                            | ⏳ Pendiente (revisar notas) |
| Integración e2e                          | confirmar end-to-end desde estudiantes hasta sueldos     | ⏳ Pendiente                 |

---

## 3. KAN-22: Datos Institucionales + Aporte a Planta Central (RAMA: feature/KAN-22-PlantaCentral)

**Estado actual:** rama creada, lista para implementación.  
**Objetivo:** dejar de modelar el detalle de cargos de planta central y pasar a un modelo agregado con inputs institucionales manuales, para calcular cuánto aporta la carrera a cubrir el costo administrativo institucional.

### 3.0 Decisión funcional cerrada (alcance KAN-22)

1. NO migrar ni reconstruir las 436 filas del Excel de "Planta central".
2. SÍ crear una captura manual de totales institucionales en una nueva vista "Datos Institucionales".
3. SÍ calcular el aporte de la carrera por período semestral usando prorrateo por matrícula.
4. SÍ restringir acceso del módulo a roles `Administrador` y `Administrador general`.
5. SÍ conservar "Planta central - detalle" solo como respaldo histórico externo al sistema.

### 3.1 Modelo de negocio final

#### 3.1.1 Datos Institucionales (input manual)

Inputs requeridos por período vigente:

- N° estudiantes universidad.
- N° docentes universidad.
- N° personas planta central.
- Sueldo básico total mensual.
- Funcional.
- Fondo de reserva.
- Beneficio social XIV.
- Beneficio social XIII.
- Aporte patronal.
- Varios.

Campos calculados (solo lectura):

- Masa salarial mensual = suma de rubros monetarios.
- Total mensual planta central = masa salarial mensual.
- Total anual planta central = total mensual x 12.
- Personal administrativo por docente = personas planta central / docentes universidad.
- Costo planta central por estudiante anual = total anual / estudiantes universidad.

#### 3.1.2 Planta Central (cálculo de aporte de carrera)

Sin inputs manuales propios. Toma:

- Totales desde Datos Institucionales vigentes.
- Matrícula proyectada de la carrera desde Estudiantes.

Fórmula semestral por período:

```text
AporteCarrera = (TotalMensualPlantaCentral / EstudiantesUniversidad) x AlumnosCarreraPeriodo x 6
```

Salidas:

- Aporte por período (8 semestres).
- Porcentaje por período sobre total anual de planta central.
- Aporte acumulado 4 años.
- Aporte promedio anual.
- Porcentaje promedio de contribución.

### 3.2 Diseño técnico (sin endpoints)

> Nota: por lineamiento del proyecto, este plan omite detalle de API/endpoint y se enfoca en dominio, aplicación, persistencia y UI.

#### 3.2.1 Entidad nueva: DatosInstitucionales

**Archivo nuevo:** `src/Domain/Entities/DatosInstitucionales.cs`

```csharp
public sealed class DatosInstitucionales : EntidadDominioBase
{
    private DatosInstitucionales() { }

    public DatosInstitucionales(
        string periodo,
        int numeroEstudiantesUniversidad,
        int numeroDocentesUniversidad,
        int numeroPersonasPlantaCentral,
        decimal sueldoBasico,
        decimal funcional,
        decimal fondoReserva,
        decimal beneficioXiv,
        decimal beneficioXiii,
        decimal aportePatronal,
        decimal varios,
        int actualizadoPorUsuarioId,
        string? fuenteNotas)
    {
        CambiarPeriodo(periodo);
        CambiarPoblacion(numeroEstudiantesUniversidad, numeroDocentesUniversidad, numeroPersonasPlantaCentral);
        CambiarRubros(sueldoBasico, funcional, fondoReserva, beneficioXiv, beneficioXiii, aportePatronal, varios);
        CambiarAuditoria(actualizadoPorUsuarioId, fuenteNotas);
    }

    public string Periodo { get; private set; } = string.Empty;
    public int NumeroEstudiantesUniversidad { get; private set; }
    public int NumeroDocentesUniversidad { get; private set; }
    public int NumeroPersonasPlantaCentral { get; private set; }

    public decimal SueldoBasico { get; private set; }
    public decimal Funcional { get; private set; }
    public decimal FondoReserva { get; private set; }
    public decimal BeneficioXiv { get; private set; }
    public decimal BeneficioXiii { get; private set; }
    public decimal AportePatronal { get; private set; }
    public decimal Varios { get; private set; }

    public DateTimeOffset FechaActualizacion { get; private set; }
    public int ActualizadoPorUsuarioId { get; private set; }
    public string? FuenteNotas { get; private set; }

    public decimal MasaSalarialMensual => SueldoBasico + Funcional + FondoReserva + BeneficioXiv + BeneficioXiii + AportePatronal + Varios;
    public decimal TotalMensualPlantaCentral => MasaSalarialMensual;
    public decimal TotalAnualPlantaCentral => Math.Round(TotalMensualPlantaCentral * 12m, 2);
    public decimal RatioAdminPorDocente => NumeroDocentesUniversidad <= 0 ? 0m : Math.Round((decimal)NumeroPersonasPlantaCentral / NumeroDocentesUniversidad, 4);
    public decimal CostoPlantaCentralPorEstudianteAnual => NumeroEstudiantesUniversidad <= 0 ? 0m : Math.Round(TotalAnualPlantaCentral / NumeroEstudiantesUniversidad, 4);
}
```

#### 3.2.2 Se retiran del alcance KAN-22

- `CargoPlantaCentral`.
- `DistribucionPlantaCentral` persistida.
- Repositorios por cargo individual de planta central.

El cálculo de aporte se ejecuta en memoria al vuelo y no se persiste por fila/cargo.

### 3.3 Persistencia y migración

#### 3.3.1 Tabla nueva

**Archivo nuevo:** `src/Infrastructure/Persistence/Configuraciones/ConfiguracionDatosInstitucionales.cs`

Tabla sugerida: `datos_institucionales`

Columnas:

- `id`
- `periodo`
- `n_estudiantes_universidad`
- `n_docentes_universidad`
- `n_personas_planta_central`
- `sueldo_basico`
- `funcional`
- `fondo_reserva`
- `beneficio_xiv`
- `beneficio_xiii`
- `aporte_patronal`
- `varios`
- `fecha_actualizacion`
- `actualizado_por_usuario_id`
- `fuente_notas`

#### 3.3.2 Migración

**Archivo nuevo:** `src/Infrastructure/Persistence/Migrations/YYYYMMDDHHMMSS_KAN22_DatosInstitucionales.cs`

Incluye:

- Creación de tabla `datos_institucionales`.
- Índice único por `periodo`.
- FK a usuario en `actualizado_por_usuario_id`.
- Semilla opcional con valores de referencia iniciales (solo si se aprueba).

### 3.4 Use cases KAN-22

#### 3.4.1 Configurar Datos Institucionales

**Archivo nuevo:** `src/Application/UseCases/SueldosPlantaCentral/ConfigurarDatosInstitucionalesCommand.cs`

Responsabilidades:

- Crear período vigente.
- Actualizar registro existente.
- Validar reglas de entrada.
- Registrar fecha y usuario de actualización.
- Permitir notas de fuente documental.

#### 3.4.2 Obtener Datos Institucionales Vigentes

**Archivo nuevo:** `src/Application/UseCases/SueldosPlantaCentral/ObtenerDatosInstitucionalesVigentesQuery.cs`

Responsabilidades:

- Entregar el registro vigente para módulos consumidores.
- Exponer campos calculados para UI.

#### 3.4.3 Calcular Aporte Planta Central de la Carrera

**Archivo nuevo:** `src/Application/UseCases/SueldosPlantaCentral/CalcularAportePlantaCentralCarreraQuery.cs`

Responsabilidades:

- Leer matrícula proyectada por período semestral de la carrera.
- Leer total mensual y anual de Datos Institucionales vigentes.
- Calcular vector de aporte por período.
- Calcular porcentajes, acumulados y promedios.
- No persistir resultado.

### 3.5 DTOs nuevos

**Archivo nuevo:** `src/Application/DTOs/SueldosPlantaCentral/DatosInstitucionalesDto.cs`

Incluye:

- Todos los inputs manuales.
- Campos calculados read-only (masa mensual, total anual, ratios).
- Metadatos de actualización.

**Archivo nuevo:** `src/Application/DTOs/SueldosPlantaCentral/AportePlantaCentralCarreraDto.cs`

Incluye:

- `Periodos[]`.
- `AlumnosCarreraPorPeriodo[]`.
- `AportesPorPeriodo[]`.
- `PorcentajeSobreTotalAnualPorPeriodo[]`.
- `AporteAcumulado`.
- `AportePromedioAnual`.
- `PorcentajePromedioSobreTotalAnual`.

### 3.6 UI KAN-22

#### 3.6.1 Ventana Datos Institucionales (solo admin)

Secciones:

- Población institucional.
- Planta central (totales mensuales).
- Panel de métricas calculadas en vivo.

Elementos clave:

- Botón Guardar.
- Etiqueta "última actualización" (usuario + fecha).
- Opción de duplicar período anterior (si se habilita histórico en UI).

#### 3.6.2 Ventana Aporte a Planta Central (solo admin)

Tabla de salida por período con:

- Alumnos de carrera.
- Aporte monetario semestral.
- % sobre total anual institucional.

Tarjetas resumen:

- Aporte acumulado.
- Aporte promedio anual.
- % promedio.

Texto interpretativo:

- "La carrera [X] aporta en promedio [Y] anuales a Planta Central, equivalente al [Z]% del costo administrativo institucional".

### 3.7 Integración con módulos existentes

Consumidores obligatorios de `DatosInstitucionales` vigente:

- `5 Demanda`: estudiantes y docentes universidad.
- `8 Mantenimiento`: estudiantes universidad.
- `9 Inv. Vinculación Becas`: estudiantes y docentes universidad.
- `10 Costos y Gastos`: aporte de carrera por período en la línea Administración Central.

Regla de integración:

- No duplicar parámetros institucionales en cada módulo.
- Recalcular aguas abajo cuando cambie Datos Institucionales.

### 3.8 Permisos

| Rol                   | Ver Datos Institucionales | Editar Datos Institucionales | Ver Aporte Planta Central |
| --------------------- | ------------------------- | ---------------------------- | ------------------------- |
| Usuario normal        | No                        | No                           | No                        |
| Administrador         | Sí                        | Sí                           | Sí                        |
| Administrador general | Sí                        | Sí                           | Sí                        |

### 3.9 Reglas de validación

1. Todos los rubros monetarios >= 0.
2. N° estudiantes universidad > 0.
3. N° docentes universidad > 0.
4. N° personas planta central >= 0.
5. Mostrar warning si masa salarial mensual cambia mas de +/-20% respecto al período anterior.

### 3.10 CU/RN (actualización)

**Archivo nuevo:** `src/Application/UseCases/SueldosPlantaCentral/CU-SP-03.MD`

Nuevo enfoque CU-SP-03:

- "Configurar Datos Institucionales y Calcular Aporte de la Carrera a Planta Central".

RN propuestas:

- **RN-101:** Datos Institucionales se cargan manualmente por administrador.
- **RN-102:** El detalle de cargos individuales de planta central no forma parte del sistema.
- **RN-103:** Aporte semestral = (total mensual PC / estudiantes universidad) x alumnos carrera x 6.
- **RN-104:** El cálculo de aporte es por período, no global.
- **RN-105:** El resultado de aporte de carrera se calcula al vuelo y no se persiste.
- **RN-106:** Solo Administrador y Administrador general pueden acceder al módulo.
- **RN-107:** Cambios en Datos Institucionales deben propagar recálculo a módulos dependientes.

### 3.11 Tests KAN-22

**Archivo nuevo:** `tests/Application.Tests/SueldosPlantaCentral/Kan22DatosInstitucionalesTests.cs`

Casos mínimos:

- Cálculo correcto de masa salarial mensual.
- Cálculo correcto de total anual.
- Validación de estudiantes/docentes > 0.
- Ratio admin/docente correcto.
- Warning cuando variación mensual supera +/-20%.

**Archivo nuevo:** `tests/Application.Tests/SueldosPlantaCentral/Kan22AportePlantaCentralTests.cs`

Casos mínimos:

- Aporte semestral coincide con fórmula esperada.
- Porcentaje sobre total anual se calcula correctamente.
- Acumulado y promedio anual correctos.
- Recalcula cuando cambian Datos Institucionales.

### 3.12 Deliverables de KAN-22 (redefinidos)

| Entregable                             | Archivo(s)                                                                                | Estado       |
| -------------------------------------- | ----------------------------------------------------------------------------------------- | ------------ |
| Entidad agregada institucional         | `DatosInstitucionales.cs`                                                                 | ⏳ Pendiente |
| Configuración EF + migración           | `ConfiguracionDatosInstitucionales.cs`, `KAN22_DatosInstitucionales.cs`                   | ⏳ Pendiente |
| Casos de uso de configuración/consulta | `ConfigurarDatosInstitucionalesCommand.cs`, `ObtenerDatosInstitucionalesVigentesQuery.cs` | ⏳ Pendiente |
| Caso de uso de aporte por carrera      | `CalcularAportePlantaCentralCarreraQuery.cs`                                              | ⏳ Pendiente |
| DTOs institucionales y de aporte       | `DatosInstitucionalesDto.cs`, `AportePlantaCentralCarreraDto.cs`                          | ⏳ Pendiente |
| UI Datos Institucionales               | Ventana + ViewModel                                                                       | ⏳ Pendiente |
| UI Aporte a Planta Central             | Ventana + ViewModel                                                                       | ⏳ Pendiente |
| Permisos por rol                       | capa aplicación/presentación                                                              | ⏳ Pendiente |
| Tests unitarios KAN-22                 | tests de dominio y aplicación                                                             | ⏳ Pendiente |

### 3.13 Fuera de alcance explícito de KAN-22

1. Migración del detalle de 436 filas de cargos.
2. Construir ABM por cargo de planta central.
3. Persistir por fila de aporte.
4. Exponer detalle de APIs en este plan (se documentará aparte si se requiere).

---

## 4. KAN-23: Mejoras Consolidado Sueldos (RAMA: main, sin feature branch)

**Estado actual:** básico implementado (§5 en Fase 5 de DECISIONES_CALCULO_DOCENTES_MT_TP.md).  
**Pendiente:** integrar KAN-20, KAN-21, KAN-22 en la vista; mejoras UI; validaciones.

### 4.1 Cambios en ResumenSueldosVistaDto

**Archivo:** `src/Application/DTOs/CargosFacultad/ResumenSueldosVistaDto.cs`

```csharp
public sealed class ResumenSueldosVistaDto
{
    public int CarreraId { get; init; }
    public string CarreraNombre { get; init; } = string.Empty;
    public List<PeriodoDisponibleSueldosDto> Periodos { get; init; } = [];
    public List<FilaResumenSueldosDto> Filas { get; init; } = [];
    public decimal[] TotalesPorPeriodo { get; init; } = [];
    public decimal GranTotal { get; init; }
    public decimal TotalSemestralPeriodoFinal { get; init; }
    public string EtiquetaPeriodoFinal { get; init; } = string.Empty;

    // NUEVO: incluir planta central
    public DistribucionPlantaCentralVistaDto? PlantaCentralDistribucion { get; init; }

    // NUEVO: validaciones
    public decimal TotalSueldosMásPlantaCentral { get; init; }
    public decimal RatioSueldosIngresos { get; init; } // Sueldos / Ingresos totales
}
```

### 4.2 Integración en GenerarResumenSueldosQuery

**Cambio:** incluir llamada a `CalcularDistribucionPlantaCentralQuery` y combinar totales.

### 4.3 Deliverables de KAN-23

| Entregable                              | Estado                                  |
| --------------------------------------- | --------------------------------------- |
| Incluir PlantaCentral en ResumenSueldos | ⏳ Pendiente                            |
| Validar totales (Facultad + PC)         | ⏳ Pendiente                            |
| Ratio Sueldos/Ingresos                  | ⏳ Pendiente (requiere módulo Ingresos) |
| UI polish (labels, colores, export)     | ⏳ Pendiente                            |

---

## 5. Matriz de Dependencias

```
KAN-20 (backend TP + hTP real)
  ↓
  ├─→ KAN-21 (peso proporcional + inflación)
  │    ↓
  │    └─→ KAN-23 (integración en ResumenSueldos)
  │
  └─→ KAN-22 (datos institucionales + aporte) [paralelo a KAN-21]
       ↓
      └─→ KAN-23 (incluir aporte institucional en consolidado)
```

**Conclusión:** KAN-20 es bloqueador para KAN-21/KAN-22. KAN-21 y KAN-22 pueden ejecutarse en paralelo.

---

## 6. Resumen de Cambios en CU y RN

### CU modificados

- **CU-SP-01.MD:** Agregar RN-96/97 (validación TipoContrato/TarifaHora)
- **CU-SP-02.MD:** Verificar RN-79, RN-81/82 (inflación), actualizar TODO comentarios

### CU nuevos

- **CU-SP-03.MD:** Configurar Datos Institucionales y calcular aporte de carrera (KAN-22)

### Archivos CU/RN a actualizar

```
docs/
├── Trazabilidad_RN_Excel.md (agregar RN-96..107)
└── DECISIONES_CALCULO_DOCENTES_MT_TP.md (Fase 7: KAN-20 completar + KAN-21 + KAN-22 + KAN-23)
```

---

## 7. Cronograma sugerido

| Tarea                                        | Rama           | Estimado | Prioridad  |
| -------------------------------------------- | -------------- | -------- | ---------- |
| **KAN-20: Completar backend TP**             | main           | 2-3 días | 🔴 CRÍTICA |
| DTOs + Comandos                              | main           | 1 día    | 🔴         |
| hTP real consumo                             | main           | 1 día    | 🔴         |
| Tests + verificación BD                      | main           | 1 día    | 🔴         |
| Merge a develop                              | main           | —        | —          |
| **KAN-21: Peso + Inflación**                 | feature/KAN-21 | 2-3 días | 🟠 ALTA    |
| Verificar código                             | feature/KAN-21 | 0.5 día  | 🟠         |
| Tests                                        | feature/KAN-21 | 1 día    | 🟠         |
| Integración e2e                              | feature/KAN-21 | 1 día    | 🟠         |
| PR review + merge                            | —              | 1 día    | —          |
| **KAN-22: Datos Institucionales + Aporte**   | feature/KAN-22 | 3-4 días | 🟠 ALTA    |
| Entidad agregada + configuración EF          | feature/KAN-22 | 1 día    | 🟠         |
| Use cases + DTOs + permisos                  | feature/KAN-22 | 1.5 días | 🟠         |
| Migración BD + CU-SP-03                      | feature/KAN-22 | 1 día    | 🟠         |
| Tests + integración con módulos consumidores | feature/KAN-22 | 1 día    | 🟠         |
| PR review + merge                            | —              | 1 día    | —          |
| **KAN-23: Consolidado Sueldos**              | main           | 2 días   | 🟡 MEDIA   |
| Integración KAN-20/21/22                     | main           | 1.5 días | 🟡         |
| Validaciones + UI polish                     | main           | 0.5 días | 🟡         |

---

## 8. Checklist final antes de cerrar cada KAN

### KAN-20 Checklist

- [ ] DTOs extendidos (`CrearCargoFacultadDto`, `ActualizarCargoFacultadDto`)
- [ ] Comandos actualizados con `TipoContrato` y `TarifaHora`
- [ ] Validación: TP → TarifaHora > 0, SueldoBaseMensual = 0
- [ ] hTP real consumido en `GenerarTablaSueldosPeriodoQuery` y `GenerarResumenSueldosQuery`
- [ ] Tests: pago TP por hora, impact override
- [ ] CU-SP-01.MD actualizado con RN-96/97
- [ ] Verificación BD: migración aplicada, backfill confirmado
- [ ] Build ✅, tests ✅, no warnings

### KAN-21 Checklist

- [ ] Peso proporcional verificado en `CalculoCargosFacultad.CalcularPeso`
- [ ] Inflación encadenada verificada en `GenerarTablaSueldosPeriodoQuery`
- [ ] `GenerarResumenSueldosQuery` usa peso por período
- [ ] Tests: peso proporcional, inflación encadenada
- [ ] CU-SP-02.MD con RN-81/82
- [ ] Integración e2e: estudiantes → sueldos con peso e inflación
- [ ] Build ✅, tests ✅, no warnings

### KAN-22 Checklist

- [ ] Entidad `DatosInstitucionales` implementada
- [ ] Configuración EF + migración `KAN22_DatosInstitucionales`
- [ ] Use cases: configurar/obtener vigentes + calcular aporte carrera
- [ ] DTOs: `DatosInstitucionalesDto` y `AportePlantaCentralCarreraDto`
- [ ] Ventana `Datos Institucionales` (solo admin/admin general)
- [ ] Ventana `Aporte a Planta Central` (solo admin/admin general)
- [ ] CU-SP-03.MD actualizado con nuevo alcance y RN-101..107
- [ ] Integración: 5 Demanda, 8 Mantenimiento, 9 Inv. Vinculación Becas, 10 Costos y Gastos
- [ ] Validación warning de variación mensual +/-20%
- [ ] Build ✅, tests ✅, no warnings

### KAN-23 Checklist

- [ ] `ResumenSueldosVistaDto` incluye planta central
- [ ] `GenerarResumenSueldosQuery` integra KAN-20/21/22
- [ ] Validaciones: totales coherentes
- [ ] UI: labels, colores, bloques destacados
- [ ] Tests e2e: consolidado con facultad + planta central
- [ ] Build ✅, tests ✅, no warnings

---

**Documento preparado para seguimiento e implementación fase a fase.**
