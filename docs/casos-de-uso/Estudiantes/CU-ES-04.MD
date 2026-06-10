```md
# Caso de Uso: Editar Consumo por Período (override de horas)

## Identificación

| Campo            | Valor                                                                |
| ---------------- | -------------------------------------------------------------------- |
| Identificador    | CU-ES-04                                                             |
| Nombre           | Editar Consumo por Período (override de horas)                       |
| RF asociado      | RF-ES-04                                                             |
| Actor primario   | Planificador Académico                                               |
| Actor secundario | Administrador; Sistema (cascada de recálculo); Auditoría             |

## Descripción

Permite al usuario editar las horas de docencia asistida y/o aplicación práctica de un período específico de una proyección de estudiantes. El override se persiste en la tabla `override_horas_periodo` y prevalece sobre los valores por defecto del consolidador. El cambio dispara recálculo en cascada de Tab 3 (Horas), Tab 2 (Docentes) y aguas abajo en Sueldos y Capital de Trabajo.

---

## Flujo Principal — Editar consumo de un período

1. El usuario abre la pestaña "4. Consumo por período" en el módulo Estudiantes con una proyección seleccionada.
2. El sistema muestra la grilla de períodos × {H. Docencia, H. Práctica} con valores actuales (override si existe, default si no).
3. El usuario edita una o más celdas.
4. El sistema valida cada valor: número positivo (>0), tipo decimal.
5. El sistema persiste el override mediante `EditarConsumoPeriodoUseCase` (upsert por `proyeccion_id + periodo`).
6. El sistema dispara recálculo en cascada:
   - Tab 3 — filas 15/17/19/21 desde el período editado en adelante.
   - Tab 2 — desglose de docentes (PhD/Mgs/MT/TP/Técnico) desde el período editado.
   - Tab 4 — refleja los nuevos valores.
   - Módulos consumidores (Sueldos Facultad, Capital de Trabajo) reciben los nuevos totales en su próxima consulta.
7. El sistema notifica completado y registra auditoría fire-and-forget.

### Flujos alternos

**4a. Validación falla (valor ≤ 0 o no numérico):**

- El sistema muestra mensaje: "El valor debe ser un número positivo".
- Mantiene el valor previo en la celda.
- El flujo retorna al paso 3.

**5a. Restaurar valor original (botón por celda):**

- El usuario selecciona "Restaurar valor original".
- El sistema invoca `RestaurarConsumoPeriodoUseCase` que ejecuta DELETE de la fila override.
- El consolidador vuelve a usar el valor por defecto.
- Se registra auditoría con acción ELIMINAR.

**3a. La proyección no existe o no tiene detalles:**

- El sistema muestra advertencia y desactiva la edición.

---

## Precondiciones

- El usuario ha iniciado sesión y tiene permiso `ES.EDITAR` o es Administrador.
- Existe una `ProyeccionEstudiantes` activa con detalles cargados.

## Postcondiciones

- El override queda persistido en `override_horas_periodo` (o eliminado si fue restaurar).
- Los cálculos downstream reflejan el cambio en su próxima consulta.
- Se registra una entrada en `auditoria_log` con la acción ejecutada.

## Reglas de negocio

- **RN-98:** Validación >0: las horas de docencia/práctica deben ser estrictamente mayores a cero.
- **RN-99:** Granularidad por período: el override es por par `(proyeccion_id, periodo)`. Cada período es independiente; no hay override global.
- **RN-100:** Reset disponible: el usuario puede restaurar el valor original en cualquier momento mediante DELETE de la fila override (no se conserva historial inline; el histórico vive en `auditoria_log`).
- **RN-101:** Override prevalece sobre el cálculo automático del consolidador. Si la fila existe y la columna no es NULL, ese valor se usa. Si la columna es NULL, se usa el default.
- **RN-102:** La auditoría se delega a `IAuditoriaServicio.RegistrarAsync` (fire-and-forget). La tabla override no duplica historial.
- **RN-103:** La cascada de recálculo NO afecta matrícula ni tasa de retención (solo horas → docentes → sueldos → capital de trabajo).
```
