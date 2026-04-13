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

- **RN-67:** Parámetros configurables solo Administrador.
- **RN-68:** Distribución 40% / 60%.
- **RN-69:** Fórmulas cálculo.
- **RN-70:** Garantizar 1 Mgs.
- **RN-71:** Redondeo techo.
- **RN-72:** Parcial residual.
- **RN-73:** Datos enviados a sueldos.
```
