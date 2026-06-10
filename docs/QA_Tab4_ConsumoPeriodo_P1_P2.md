# QA Manual - Tab 4 Consumo por periodo (P1 y P2)

## Objetivo

Validar el nuevo flujo en Proyeccion de Estudiantes > 4. Consumo por periodo:

- Seleccion de fila
- Boton "Editar horas malla"
- Panel "Editando horas malla"
- Guardado de H. Docencia/H. Practica
- Recalculo y sumas

## Precondiciones

1. Existe al menos una proyeccion generada con periodos P1 y P2.
2. Usuario con permisos de edicion en Estudiantes (Administrador recomendado).
3. Abrir modulo Estudiantes y seleccionar una proyeccion.

## Caso P1 - Editar periodo 1

1. Ir a pestana 4. Consumo por periodo.
2. Seleccionar la fila del periodo 1.
3. Confirmar que el boton "Editar horas malla" se habilita.
4. Hacer click en "Editar horas malla".
5. Confirmar que aparece el panel con titulo "Editando horas malla".
6. En H. Docencia ingresar 377.
7. En H. Practica ingresar 188.
8. Hacer click en "Guardar cambios".

Resultado esperado:

- La grilla muestra P1 con H. Docencia = 377 y H. Practica = 188.
- El panel se cierra automaticamente.
- Se muestra mensaje de exito.
- Se recalcula Tab 3 Horas por periodo y dependencias.
- Totales inferiores se actualizan:
  - Total H. Docencia
  - Total H. Practica
  - Total horas combinadas

## Caso P2 - Editar periodo 2

1. En la misma proyeccion, seleccionar la fila del periodo 2.
2. Click en "Editar horas malla".
3. Cambiar H. Docencia a 355.
4. Cambiar H. Practica a 210.
5. Guardar cambios.

Resultado esperado:

- P2 refleja nuevos valores.
- P1 mantiene sus valores ya guardados.
- Totales vuelven a recalcularse.
- La cascada de calculo se mantiene consistente.

## Validaciones de errores

1. Abrir panel y escribir texto no numerico en cualquier campo.
2. Intentar guardar.

Resultado esperado:

- Se muestra error de validacion numerica.
- No se guardan cambios.

3. Escribir 0 o valor negativo en cualquier campo.
4. Intentar guardar.

Resultado esperado:

- Se muestra error: valores mayores a 0.
- No se guardan cambios.

## Evidencia recomendada

- Captura antes y despues para P1.
- Captura antes y despues para P2.
- Captura de bloque de totales actualizado.
