```md
# Caso de Uso: Generar informe CES y reportes por dirección

## Identificación

| Campo            | Valor                                        |
| ---------------- | -------------------------------------------- |
| Identificador    | CU-FB-03                                     |
| Nombre           | Generar informe CES y reportes por dirección |
| RF asociado      | RF-FB-03                                     |
| Actor primario   | Administrador                                |
| Actor secundario | Planificador Académico / Consultor           |

## Descripción

El sistema genera automáticamente el cuadro resumen CES y reportes filtrados por dirección institucional.

---

## Flujo Principal — Generar informe CES

1. El Administrador accede a reportes CES.
2. El sistema consolida datos.
3. El sistema genera cuadro CES.
4. El sistema genera cuadro INF CES.
5. El sistema muestra resultados.

---

## Flujo alterno — Generar reporte por dirección

1. El usuario selecciona dirección.
2. El sistema filtra información.
3. El sistema muestra reporte.
4. El usuario puede exportar.

### Flujos alternos

**2a. Datos incompletos**

- Se genera informe parcial.
- Se marca como pendiente.

---

## Precondiciones

- Módulos con datos.

## Postcondiciones

- Informe CES generado.
- Reportes disponibles.

## Reglas de negocio

- **RN-184:** Formato CES configurable.
- **RN-185:** Consolidación automática.
- **RN-186:** Reportes filtrados por dirección.
```
