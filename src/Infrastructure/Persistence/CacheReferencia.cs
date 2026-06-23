using System.Collections.Concurrent;
using SistemaAranceles.Domain.Entities;

namespace SistemaAranceles.Infrastructure.Persistence;

/// <summary>
/// Caché en memoria (singleton) de datos de referencia que casi no cambian pero se re-piden en
/// cada query de una carga (DatosInstitucionales vigente, Carrera y EscenarioProyeccion por id).
/// Elimina round-trips redundantes a Supabase por carga (optimización B.1): menos latencia y menos
/// presión sobre el pool de conexiones.
///
/// Correctitud: se invalida explícitamente desde CADA método de mutación de su repositorio
/// (no desde los commands), así ningún flujo de escritura puede "olvidar" invalidar y nunca se
/// sirven datos obsoletos. Las instancias cacheadas se usan SOLO en lectura; los flujos de
/// escritura cargan su propia copia rastreada, por lo que no contaminan la caché.
/// ponytail: caché de proceso, single-user desktop; si en el futuro hubiera multi-instancia,
/// añadir expiración o notificación entre procesos.
/// </summary>
public sealed class CacheReferencia
{
    private volatile DatosInstitucionales? _datosVigente;
    private readonly ConcurrentDictionary<int, Carrera> _carreras = new();
    private readonly ConcurrentDictionary<int, EscenarioProyeccion> _escenarios = new();

    public DatosInstitucionales? DatosVigente => _datosVigente;
    public void GuardarDatosVigente(DatosInstitucionales datos) => _datosVigente = datos;
    public void InvalidarDatos() => _datosVigente = null;

    public Carrera? ObtenerCarrera(int id) => _carreras.TryGetValue(id, out var c) ? c : null;
    public void GuardarCarrera(int id, Carrera carrera) => _carreras[id] = carrera;
    public void InvalidarCarreras() => _carreras.Clear();

    public EscenarioProyeccion? ObtenerEscenario(int id) => _escenarios.TryGetValue(id, out var e) ? e : null;
    public void GuardarEscenario(int id, EscenarioProyeccion escenario) => _escenarios[id] = escenario;
    public void InvalidarEscenarios() => _escenarios.Clear();
}
