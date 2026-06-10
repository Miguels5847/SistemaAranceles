```md
# Caso de Uso: Exportar resultados de cualquier módulo del sistema

## Identificación

| Campo            | Valor                                               |
| ---------------- | --------------------------------------------------- |
| Identificador    | CU-FB-04                                            |
| Nombre           | Exportar resultados de cualquier módulo del sistema |
| RF asociado      | RF-FB-04                                            |
| Actor primario   | Administrador / Planificador Académico / Consultor  |
| Actor secundario | Sistema                                             |

## Descripción

Permite exportar resultados del sistema en formato XLSX o PDF.

---

## Flujo Principal — Exportar módulo individual

1. El usuario accede a un módulo.
2. Selecciona exportar.
3. Elige formato.
4. El sistema genera archivo.
5. El sistema muestra diálogo de guardado.
6. El sistema guarda archivo.
7. El sistema muestra confirmación.

---

## Flujo alterno — Exportación consolidada

1. El usuario accede a exportación general.
2. Selecciona exportar todo.
3. El sistema genera archivo consolidado.
4. El sistema guarda archivo.
5. Confirmación.

### Flujos alternos

**3a. Error al generar archivo**

- El sistema muestra mensaje.
- Permite reintentar.

---

## Precondiciones

- Usuario autenticado.
- Módulo con datos.

## Postcondiciones

- Archivo generado.

## Reglas de negocio

- **RN-187:** XLSX mantiene fórmulas.
- **RN-188:** PDF formato limpio.
- **RN-189:** Exportación individual < 5s.
- **RN-190:** Exportación consolidada < 15s.
```
