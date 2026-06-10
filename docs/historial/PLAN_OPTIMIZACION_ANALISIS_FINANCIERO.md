# Plan de ejecución — Optimizar tiempos de Análisis Financiero (cambios seguros)

> Estado: **EJECUTADO** (jun-2026). Build 0 warnings · 114 + 2 tests verde. Alcance: **solo cambios seguros** (sin paralelizar).
> Garantía: **result-preserving** — no cambia ningún número (VAN, TIR, TMR, arancel óptimo, P&G, Flujo, Punto de Equilibrio, CES, arancel referencial, becas/descuentos). Solo reduce round-trips y DDL redundante.

---

## 1. Contexto y diagnóstico

La pantalla **Análisis Financiero** es lenta porque `RefrescarAsync`
([AnalisisFinancieroViewModel.cs:460-532](src/Presentation/ViewModels/AnalisisFinanciero/AnalisisFinancieroViewModel.cs#L460-L532))
ejecuta ~14 queries **secuencialmente** sobre un único scope/`DbContext`. El cuello de botella son los
**round-trips a Supabase** (session pooler, `Maximum Pool Size=5`, ~0.1-0.3 s c/u), no el cómputo. Ya se aplicó
antes la convención de **precalculados**; este plan ataca lo que quedó.

### Hallazgo dominante: DDL self-healing en CADA lectura
- `datos institucionales` se lee **~15-20×** por refresco. Cada `ObtenerVigenteAsync` envía un batch
  `ALTER TABLE … ADD COLUMN IF NOT EXISTS / UPDATE / ALTER COLUMN`
  ([RepositorioDatosInstitucionales.cs:54-124](src/Infrastructure/Persistence/Repositories/RepositorioDatosInstitucionales.cs#L54-L124)).
- Otros 3 repos corren `CREATE TABLE IF NOT EXISTS` en cada lectura:
  `RepositorioConfiguracionArancelCarrera`, `RepositorioDescuentoArancelCiclo`, `RepositorioRatioMaterialDemanda`.
- **Total: ~25-30 batches DDL redundantes por refresco** (innecesarios tras la 1ª vez); además toman locks `ACCESS EXCLUSIVE`.

### Cargas de datos duplicadas
- **Proyección de estudiantes** (DTO pesado, todos los `Detalles`) cargada **3×**: demanda, matriz e ingresos.
- **Arancel** cargado **2×**: para `ArancelVigente` y dentro de `EstadoPG → Ingresos`.
- `repositorioConfiguracionRetencion.ListarDtoAsync` cargado 2× (demanda + matriz).

### Restricciones del entorno (confirmadas)
- Connection string: `Maximum Pool Size=5`, `Multiplexing=false`, session pooler (`appsettings.Local.json`).
- `DbContext` = **Scoped**; todas las queries = **Transient** (`InfrastructureExtensions.cs`, `App.xaml.cs`).
- Repos comparten el `DbContext` del scope → hoy todo es forzosamente secuencial (EF no es thread-safe).

---

## 2. Parte 1 — DDL self-healing UNA vez por proceso  *(impacto mayor, riesgo mínimo)*

Convertir cada método `Asegurar*Async` de "corre en cada llamada" a "corre una vez por proceso" con un guard
estático thread-safe (double-checked + `SemaphoreSlim`). Preserva el self-healing (sigue creando tabla/columnas
en el 1er uso tras un deploy) y lo saca del hot path de lectura.

### Patrón a aplicar (idéntico en los 4 repos)
```csharp
// Campos estáticos (uno por repo; el DDL se materializa una vez por proceso).
private static readonly SemaphoreSlim _gateEsquema = new(1, 1);
private static bool _esquemaListo;

private async Task AsegurarParametrosInversionAsync(CancellationToken ct)   // o AsegurarTablaAsync
{
    if (_esquemaListo) return;                 // fast-path: 0 round-trips
    await _gateEsquema.WaitAsync(ct);
    try
    {
        if (_esquemaListo) return;
        await contexto.Database.ExecuteSqlRawAsync("""
            … MISMO DDL ACTUAL, SIN CAMBIOS …
            """, ct);
        _esquemaListo = true;                  // setear solo tras éxito → reintenta si falla
    }
    finally { _gateEsquema.Release(); }
}
```
> Nota: hoy estos métodos son expression-bodied (`=> contexto.Database.ExecuteSqlRawAsync(...)`). Pasar a cuerpo
> de método con el guard. El **texto del DDL no se modifica**; solo cambia *cuándo* corre.

### Archivos (cada uno con su propio par estático `_gateEsquema` / `_esquemaListo`)
- [ ] [RepositorioDatosInstitucionales.cs](src/Infrastructure/Persistence/Repositories/RepositorioDatosInstitucionales.cs) → `AsegurarParametrosInversionAsync` *(el de mayor impacto: lo llaman 4 métodos de lectura)*.
- [ ] [RepositorioConfiguracionArancelCarrera.cs](src/Infrastructure/Persistence/Repositories/RepositorioConfiguracionArancelCarrera.cs) → `AsegurarTablaAsync`.
- [ ] [RepositorioDescuentoArancelCiclo.cs](src/Infrastructure/Persistence/Repositories/RepositorioDescuentoArancelCiclo.cs) → `AsegurarTablaAsync`.
- [ ] [RepositorioRatioMaterialDemanda.cs](src/Infrastructure/Persistence/Repositories/RepositorioRatioMaterialDemanda.cs) → `AsegurarTablaAsync`.

### Riesgo / comportamiento
- Mínimo. Idéntico salvo que el DDL no se re-ejecuta dentro del mismo proceso (un reinicio lo vuelve a correr →
  auto-heal preservado). Si el batch falla, el flag no se setea y reintenta en la siguiente llamada.
- El guard estático es process-wide (los repos son Transient, pero el `static` se comparte): correcto para
  "una vez por proceso" y beneficia a **toda la app** (Costos y Gastos también).

---

## 3. Parte 2 — Eliminar cargas duplicadas con precalculados  *(result-preserving)*

Todos son **parámetros opcionales nuevos** que por defecto (`null`) mantienen el comportamiento actual; solo el
orquestador (`RefrescarAsync`) los aprovecha. Mismo patrón que `demandaPrecalculada` / `costosPrecalculados` ya existentes.

### 2a. Proyección de estudiantes cargada 3× → cargar 1×
Hoy `ObtenerIdPorCarreraYEscenarioAsync` + `ObtenerDtoPorIdAsync` (todos los `Detalles`) se ejecutan en:
- [ObtenerDemandaProyectadaQuery.cs:38-46](src/Application/UseCases/DemandaIngresos/ObtenerDemandaProyectadaQuery.cs#L38-L46)
- [ObtenerMatrizCostosGastosQuery.ObtenerProyeccionAsync](src/Application/UseCases/CostosGastos/ObtenerMatrizCostosGastosQuery.cs#L255-L264)
- [CalcularIngresosProyectadosQuery.cs:69-77](src/Application/UseCases/DemandaIngresos/CalcularIngresosProyectadosQuery.cs#L69-L77)

Pasos:
- [ ] Agregar param opcional `ProyeccionEstudiantesDto? proyeccionPrecalculada = null` a esas 3 queries. Cuando
      viene, saltar la carga (id + DTO) y usar la pasada.
- [ ] En `RefrescarAsync`: resolver `IRepositorioProyeccionEstudiantes`, cargar la proyección **una sola vez**
      (id + DTO) y pasarla como `proyeccionPrecalculada` a demanda, matriz e ingresos.
- [ ] Cuidar el caso "sin proyección" (id `null`/sin detalles): mantener los mismos mensajes de advertencia que hoy.

### 2b. Arancel cargado 2× + Ingresos recomputado dentro de P&G
- `EstadoPG` llama a `CalcularIngresosProyectadosQuery` fresco
  ([ObtenerEstadoPerdidasGananciasQuery.cs:41](src/Application/UseCases/AnalisisFinanciero/ObtenerEstadoPerdidasGananciasQuery.cs#L41)),
  que recarga arancel; el orquestador ya calcula `ArancelVigente`
  ([AnalisisFinancieroViewModel.cs:508](src/Presentation/ViewModels/AnalisisFinanciero/AnalisisFinancieroViewModel.cs#L508)).

Pasos:
- [ ] Agregar `ArancelEfectivoDto? arancelPrecalculado = null` a
      [CalcularIngresosProyectadosQuery](src/Application/UseCases/DemandaIngresos/CalcularIngresosProyectadosQuery.cs)
      (si viene, no llama a `obtenerArancelEfectivoQuery`; usa `arancel.ArancelEfectivo`, `MatriculaEfectiva`, `PorcentajeMatriculaAplicado`).
- [ ] Agregar `IngresosProyectadosDto? ingresosPrecalculados = null` a
      [ObtenerEstadoPerdidasGananciasQuery](src/Application/UseCases/AnalisisFinanciero/ObtenerEstadoPerdidasGananciasQuery.cs)
      (si viene, no recomputa ingresos).
- [ ] En `RefrescarAsync`: reordenar para calcular `ArancelVigente` **antes** de `EstadoPG`; luego computar
      `ingresos` una vez (con `arancelPrecalculado: ArancelVigente` + `proyeccionPrecalculada` de 2a); pasar
      `ingresosPrecalculados` a `EstadoPG`. `ArancelVigente` ya se asignaba después → moverlo arriba.

### 2c. (Opcional, solo si es trivial) `ListarDtoAsync` de configuración de retención
- Dedup entre demanda y `ObtenerMatrizCostosGastosQuery.ObtenerConsolidadoAsync`. Si entrelaza, **omitir**
  (bajo retorno).

### Explícitamente fuera de Parte 2 (no tocar)
- **No** unificar el cálculo de inflación de `ObtenerIndicadoresFinancierosQuery` vs
  `ObtenerArancelOptimoBiseccionQuery`: usan rangos de años distintos y alimentan TMR → VAN. Unificarlos
  arriesga mover un número. Se dejan independientes tal cual.

---

## 4. Parte 3 — Medición (no invasiva)

- [ ] En `RefrescarAsync`, envolver cada bloque con `Stopwatch` y emitir por
      `System.Diagnostics.Debug.WriteLine` / `Trace` (ventana Output del depurador). Ej.: `AF: matriz=1234ms`,
      `AF: arancelOptimo=…`, `AF: TOTAL=…`. Sin UI nueva ni dependencias.
- Permite comparar: refresco 1 (frío, corre DDL una vez) vs refresco 2 (caliente), y antes/después del plan.

---

## 5. Verificación

1. **Build + tests**
   - App abierta bloquea el `.exe` → compilar a temp: `dotnet build -p:OutDir=$env:TEMP/SAOptBuild/`.
   - `dotnet test` → la suite actual debe seguir **verde** (estos cambios no alteran resultados).
2. **Resultados idénticos (lo crítico)**: para una carrera/escenario con datos, comparar antes/después:
   VAN, TIR, TMR, arancel óptimo, P&G, Flujo de Fondos, Punto de Equilibrio, CES, arancel referencial →
   **byte-idénticos**.
3. **Tiempo**: con la instrumentación de Parte 3, medir el total de refresco antes/después; el **2º refresco**
   (DDL ya hecho) debe bajar marcadamente. Repetir cambiando de escenario.
4. **Self-healing intacto**: en un entorno limpio, el primer refresco tras abrir la app crea/heala
   columnas/tablas igual que hoy (sin errores de "columna inexistente").
5. **Regresión cruzada**: Costos y Gastos sigue funcionando (comparte repos/queries modificados).

---

## 6. Fuera de alcance / no tocar

- **Paralelización** (scopes/Tasks concurrentes): descartada en este pase (riesgo + pool=5). Posible follow-up futuro.
- Fórmulas financieras: VAN, TIR, TMR, inflación, arancel óptimo, becas/blindaje, descuentos por ciclo — sin cambios de lógica.
- El **texto** de los batches DDL (solo cambia su frecuencia de ejecución).
- Supabase real: nada nuevo; el self-healing sigue corriendo en runtime (ahora una vez por proceso).
- **Sin commit** salvo pedido textual.

---

## 7. Orden de ejecución sugerido

1. Parte 3 (medición) primero — para tener números base.
2. Parte 1 (DDL once) — el grueso de la ganancia; medir de nuevo.
3. Parte 2a y 2b (precalculados) — afinar; medir de nuevo.
4. Verificación completa (sección 5).
