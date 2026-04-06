```md
# Caso de Uso: Configurar y calcular horas de docencia por semana y por paralelos

## Identificación

| Campo            | Valor                                                              |
| ---------------- | ------------------------------------------------------------------ |
| Identificador    | CU-ES-02                                                           |
| Nombre           | Configurar y calcular horas de docencia por semana y por paralelos |
| RF asociado      | RF-ES-02                                                           |
| Actor primario   | Planificador Académico                                             |
| Actor secundario | Administrador; Sistema; Consultor                                  |

## Descripción

Permite ingresar horas semestrales y calcular automáticamente horas por semana, acumuladas y totales.

---

## Flujo Principal — Ingresar horas y calcular

1. El usuario accede al módulo.
2. El sistema muestra formulario.
3. El usuario ingresa horas semestrales.
4. El Administrador confirma semanas.
5. El usuario confirma.
6. El sistema recupera paralelos.
7. El sistema calcula horas por semana.
8. El sistema calcula acumulados.
9. El sistema calcula totales.
10. El sistema guarda y registra auditoría.
11. El sistema muestra resultados.

### Flujos alternos

**3a. Horas en cero**

- Advertencia informativa.

**3b. Valor negativo**

- Error.
- Retorna al paso 3.

**4a. Semanas = 0**

- Se restaura valor.

**6a. Sin paralelos**

- Mensaje.
- Retorna al inicio.

---

## Flujo alterno — Configurar semanas

1. Administrador accede parámetros.
2. Modifica semanas.
3. Sistema valida.
4. Sistema guarda.
5. Sistema recalcula.

---

## Precondiciones

- Usuario autenticado.
- Paralelos generados.

## Postcondiciones

- Horas registradas.
- Horas calculadas.
- Totales generados.
- Datos para CU-ES-03.
- Auditoría registrada.

## Reglas de negocio

- **RN-61:** Horas ingresadas manualmente.
- **RN-62:** Semanas por defecto 16.
- **RN-63:** Fórmula de cálculo.
- **RN-64:** Suma progresiva.
- **RN-65:** Ceros permitidos.
- **RN-66:** Recalculo automático.
```
