```md
# Caso de Uso: Generar paralelos y proyectar estudiantes por ciclo, año y período

## Identificación

| Campo            | Valor                                                              |
| ---------------- | ------------------------------------------------------------------ |
| Identificador    | CU-ES-01                                                           |
| Nombre           | Generar paralelos y proyectar estudiantes por ciclo, año y período |
| RF asociado      | RF-ES-01                                                           |
| Actor primario   | Planificador Académico                                             |
| Actor secundario | Sistema; Consultor (solo lectura)                                  |

## Descripción

Permite al sistema leer automáticamente desde el módulo de Tasa de Retención los datos necesarios para calcular los paralelos acumulados y proyectar estudiantes por ciclo, año y período.

---

## Flujo Principal — Generar proyección de estudiantes

1. El Planificador Académico accede al módulo de Estudiantes.
2. El sistema muestra carreras con configuración completa.
3. El usuario selecciona carrera.
4. El sistema lee datos desde Tasa de Retención.
5. El sistema calcula paralelos acumulados.
6. El sistema proyecta estudiantes por ciclo y período.
7. El sistema calcula total de estudiantes.
8. El usuario selecciona guardar.
9. El sistema guarda y registra auditoría.
10. El sistema muestra tabla completa.

### Flujos alternos

**3a. Carrera sin configuración**

- El sistema muestra mensaje.
- Retorna al paso 2.

**4a. Error al leer datos**

- El sistema muestra advertencia.
- Retorna al paso 2.

**6a. Estudiantes simulados en cero**

- El sistema muestra advertencia.
- Continúa cálculo.

**2a. No existen carreras configuradas**

- El sistema muestra mensaje.
- El flujo se detiene.

---

## Flujo alterno — Consultar proyección existente

1. El Consultor accede al módulo.
2. El sistema muestra carreras con proyección.
3. El Consultor visualiza datos.
4. No puede modificar.

---

## Precondiciones

- Usuario autenticado.
- Configuración completa en Tasa de Retención.

## Postcondiciones

- Paralelos acumulados calculados.
- Estudiantes proyectados disponibles.
- Totales calculados.
- Datos disponibles para CU-ES-02 y CU-ES-03.
- Auditoría registrada.

## Reglas de negocio

- **RN-56:** Datos de solo lectura desde Tasa de Retención.
- **RN-57:** Paralelos acumulados alternados.
- **RN-58:** Ciclo 1 constante.
- **RN-59:** Recálculo automático.
- **RN-60:** Precisión decimal.
```
