# Caso de Uso: Registrar activos fijos y proyectar necesidades futuras

## Identificación

| Campo            | Valor                                                                                                                                 |
| ---------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Identificador    | CU-RD-01                                                                                                                              |
| Nombre           | Registrar activos fijos y proyectar necesidades futuras                                                                               |
| RF asociado      | RF-RD-01                                                                                                                              |
| Actor primario   | Planificador Académico                                                                                                                |
| Actor secundario | Administrador (configura vida útil y valor residual por categoría); Sistema (cálculo automático, auditoría); Consultor (solo lectura) |

## Descripción

Permite al Planificador Académico registrar los activos fijos de inversión inicial (año 0) organizados por categoría, calcular el valor total por artículo y los subtotales por categoría, y planificar las necesidades futuras en unidades por período. Algunos activos futuros varían automáticamente con los estudiantes proyectados; otros son ingresados manualmente. Los totales alimentan directamente al módulo Inversión Inicial.

---

## Flujo Principal — Registrar activos de inversión inicial (año 0)

1. El Planificador Académico accede al módulo de Recursos Físicos y Depreciación y selecciona la carrera.
2. El sistema muestra el formulario de inversión inicial con las categorías disponibles.
3. El Planificador Académico registra cada activo del año 0.
4. El sistema calcula automáticamente el valor total por artículo.
5. El sistema calcula subtotal por categoría y total general.
6. El Planificador Académico confirma el registro.
7. El sistema guarda los activos y registra auditoría.
8. El sistema muestra la tabla de inversión inicial.

### Flujos alternos

**3a. Valor unitario inválido**

- El sistema muestra mensaje: "El valor unitario debe ser mayor a 0."
- Retorna al paso 3.

**3b. Cantidad inválida**

- El sistema muestra mensaje: "La cantidad debe ser mayor a 0."
- Retorna al paso 3.

**3c. Categoría no seleccionada**

- El sistema no permite guardar.
- Retorna al paso 3.

**6a. Cancelación**

- El sistema descarta cambios.

---

## Flujo alterno — Registrar necesidades futuras

1. El Planificador accede a necesidades futuras.
2. El sistema muestra tabla por períodos.
3. El Planificador ingresa cantidades.
4. El sistema sugiere valores vinculados a estudiantes.
5. El Planificador confirma.
6. El sistema guarda y registra auditoría.

---

## Precondiciones

- Usuario autenticado.
- Carrera registrada.
- Proyección de estudiantes opcional.

## Postcondiciones

- Activos registrados por categoría.
- Necesidades futuras guardadas.
- Totales disponibles para inversión inicial.
- Registro de auditoría generado.

## Reglas de Negocio

- **RN-94:** Los activos se organizan en categorías definidas.
- **RN-95:** Valor_total = Cantidad × Valor_unitario.
- **RN-96:** Necesidades futuras pueden ser manuales o automáticas.
- **RN-97:** Vida útil configurable por Administrador.
- **RN-98:** Valores por defecto según normativa ecuatoriana.
