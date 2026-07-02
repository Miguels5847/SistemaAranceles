# Guía de Usuario — Sistema de Aranceles Universitarios

> Guía completa de uso, pensada para usuarios no técnicos.
> Cada sección indica **qué botón pulsar, qué hace y por qué**.
> Los recuadros `📷 [Captura N]` marcan dónde insertar las capturas de pantalla.

---

## Índice

1. [Antes de empezar](#1-antes-de-empezar)
2. [Inicio de sesión](#2-inicio-de-sesión)
3. [Pantalla principal: menú, sesión y ayudas](#3-pantalla-principal)
4. [El flujo de trabajo en 9 pasos](#4-el-flujo-de-trabajo-en-9-pasos)
   - [Paso 1 — Carreras](#paso-1--carreras)
   - [Paso 2 — Tasa de Retención y Graduación](#paso-2--tasa-de-retención-y-graduación)
   - [Paso 3 — Proyección de Estudiantes](#paso-3--proyección-de-estudiantes)
   - [Paso 4 — Recursos y Depreciación](#paso-4--recursos-y-depreciación)
   - [Paso 5 — Mantenimiento e Inversión](#paso-5--mantenimiento-e-inversión)
   - [Paso 6 — Demanda e Ingresos](#paso-6--demanda-e-ingresos)
   - [Paso 7 — Costos y Gastos](#paso-7--costos-y-gastos)
   - [Paso 8 — Análisis Financiero](#paso-8--análisis-financiero)
   - [Paso 9 — Reportes](#paso-9--reportes)
5. [Módulos de apoyo](#5-módulos-de-apoyo)
6. [Módulos del administrador](#6-módulos-del-administrador)
7. [Preguntas frecuentes y problemas comunes](#7-preguntas-frecuentes)
8. [Glosario](#8-glosario)

---

## 1. Antes de empezar

### ¿Qué hace este sistema?

Calcula **cuánto debe costar el arancel de una carrera universitaria** para que sea
financieramente viable. Para eso proyecta estudiantes, docentes, costos, inversiones e
ingresos, y entrega indicadores financieros (VAN, TIR) y reportes listos para presentar.

### Instalación

1. Descomprime `SistemaAranceles-win64.zip` en cualquier carpeta.
2. Ejecuta `instalar.bat`: copia la aplicación a tu perfil de usuario y crea el acceso
   directo **"Sistema de Aranceles"** en el Escritorio. No necesita permisos de administrador
   ni instalar nada más (el programa ya incluye todo).
3. El archivo de conexión a la base de datos (`appsettings.Local.json`) lo entrega el
   administrador del sistema. Sin él la aplicación no puede conectarse.

> 📷 [Captura 1: carpeta descomprimida con instalar.bat resaltado]

### Credenciales

El usuario y la contraseña los crea el **administrador** en el módulo Gestión de Usuarios.
Si no tienes credenciales, solicítalas antes de continuar.

---

## 2. Inicio de sesión

1. Abre **Sistema de Aranceles** desde el acceso directo del Escritorio.
2. Escribe tu **correo institucional** y tu **contraseña**.
3. Pulsa **"Iniciar sesión"**.

> 📷 [Captura 2: ventana de login con los dos campos y el botón señalados]

**Detalles útiles:**

- El ícono de ojo del campo contraseña muestra/oculta lo escrito.
- Si la contraseña es incorrecta el sistema lo indica debajo del botón; espera unos
  segundos y vuelve a intentar.
- La sesión dura un tiempo fijo (por defecto 45 minutos). El contador es visible siempre
  en la esquina superior izquierda; al llegar a cero la sesión se cierra sola por seguridad.

---

## 3. Pantalla principal

> 📷 [Captura 3: ventana principal completa con flechas a: menú lateral, contador de sesión, área de trabajo]

La ventana tiene tres zonas:

| Zona | Qué es |
|---|---|
| **Menú lateral izquierdo** (azul) | La lista de módulos, agrupados en el orden del flujo de trabajo (1 · Configuración base → 5 · Resultados). Solo ves los módulos que tu rol permite. |
| **Contador de sesión** (rojo, arriba a la izquierda) | Tiempo restante de tu sesión. |
| **Área de trabajo** | El módulo seleccionado. |

**Ayudas integradas:**

- Cada módulo tiene un **badge "?" azul** junto al título: deja el mouse encima y verás
  **qué ingresar** en esa ventana y **qué resultados** produce.
- En el módulo **Carreras** está el botón **"? Guía completa: de la carrera al reporte"**,
  que abre esta misma guía en versión resumida dentro de la aplicación (9 pasos con
  botones y criterios de "listo").

> 📷 [Captura 4: badge "?" con su tooltip desplegado]
> 📷 [Captura 5: ventana "Guía completa" abierta]

---

## 4. El flujo de trabajo en 9 pasos

> **Regla de oro:** sigue los pasos en orden. Cada módulo usa los datos del anterior;
> si un valor sale en cero o gigante, el error casi siempre está en un paso previo.
>
> Los módulos **Sueldos Carrera, Aporte Planta Central, Amortización, Datos
> Institucionales, Catálogos, Usuarios y Auditoría** los configura el administrador:
> normalmente no necesitas tocarlos.

---

### Paso 1 — Carreras

**Menú:** 1 · Configuración base → **Carreras**

**Objetivo:** registrar la carrera que vas a proyectar. Todo el sistema calcula POR
carrera: sin este paso no hay dónde guardar nada.

**Qué hacer, botón por botón:**

| Botón | Qué hace | Cuándo usarlo |
|---|---|---|
| **Nuevo** | Limpia el formulario para escribir una carrera nueva. | Siempre al empezar. |
| **Guardar** | Graba la carrera escrita en el formulario. | Tras llenar Código, Nombre, Facultad y Total de ciclos. |
| **Editar selección** | Carga en el formulario la carrera marcada en la lista para corregirla. | Si te equivocaste en algún dato. |
| **Eliminar selección** | Borra (lógicamente) la carrera marcada. | Solo si la carrera fue de prueba. |
| **Refrescar** | Vuelve a leer la lista desde la base de datos. | Si otra persona agregó carreras. |

**Campos del formulario:**

- **Código**: identificador corto (ej. `SIS`). Debe ser único.
- **Nombre**: nombre completo de la carrera.
- **Facultad**: a la que pertenece.
- **Total de ciclos**: semestres de la malla (ej. 8).

> 📷 [Captura 6: módulo Carreras con flechas a Nuevo → campos → Guardar]

✅ **Listo cuando:** la carrera aparece en la lista con su código y ciclos.

---

### Paso 2 — Tasa de Retención y Graduación

**Menú:** 1 · Configuración base → **Tasa de Retención y Graduación**

**Objetivo:** definir las **metas** de permanencia de estudiantes. De aquí sale cuántos
estudiantes avanzan de un ciclo al siguiente — el dato que alimenta a casi todo el sistema.

#### Conceptos clave

- **Meta de retención (%)**: de los estudiantes que entran al ciclo 1, qué porcentaje
  quieres que llegue a la **mitad de la carrera**. Ej.: 65 %.
- **Meta de graduación (%)**: de los que llegan a la segunda mitad, qué porcentaje
  quieres que **se gradúe**. Ej.: 80 % (o 100 % si se exige titulación total).
- **Tasa por ciclo**: el sistema la **deriva automáticamente** de tus metas (se muestra
  en un recuadro informativo). Es el porcentaje que se aplica ciclo a ciclo en los
  cálculos; tú no la escribes.

#### Pestaña "Escenarios" (configuración)

1. Selecciona la **Carrera**.
2. Selecciona el escenario **Histórico** (es el escenario base; regístralo primero).
3. Escribe la **Meta de retención %** y la **Meta de graduación %**.
   Debajo verás la *"Tasa por ciclo (es la que alimenta el cálculo)"* derivada.
4. Completa **Estudiantes** y **Paralelos** de los períodos 1 (abril) y 2 (septiembre).
5. Pulsa **"Guardar"**.
6. Repite para **Optimista** y **Pesimista**: al seleccionarlos, los valores se
   **precargan solos** desde el Histórico (Optimista sube las metas y estudiantes;
   Pesimista los baja). Ajusta si quieres y pulsa **"Guardar"**.

> 📷 [Captura 7: pestaña Escenarios con flechas a Carrera → Escenario → Metas → recuadro de tasa derivada → Guardar]

#### Pestaña "Simulación"

1. Elige la **Configuración** (carrera + escenario) en el desplegable.
2. Escribe el **Grupo de ingreso** (año, ej. 2026).
3. Pulsa **"Ejecutar simulación"**.
4. La tabla de cuadros verdes muestra **cuántos estudiantes quedan en cada ciclo**;
   la cabecera muestra la retención y graduación aplicadas.

Otros botones: **"Refrescar"** (recarga la lista), **"Ver detalle"** (abre el detalle por
ciclo de la simulación marcada), **"Eliminar simulación"** y **"Limpiar por
configuración"** (borra las simulaciones de la configuración elegida).

> 📷 [Captura 8: pestaña Simulación con flechas a Configuración → Grupo de ingreso → Ejecutar simulación → cuadros de resultado]

✅ **Listo cuando:** la simulación muestra los cuadros con los alumnos por ciclo.

---

### Paso 3 — Proyección de Estudiantes

**Menú:** 2 · Proyección académica → **Proyección de Estudiantes**

**Objetivo:** generar la **matriz de cohortes**: cuántos estudiantes hay en cada ciclo,
en cada período, durante toda la proyección. De aquí salen la demanda (quiénes pagan) y
los docentes necesarios.

**Qué hacer:**

1. Selecciona la **carrera** y el **escenario**.
2. Pulsa **"GENERAR PROYECCIÓN"**: crea la matriz período × ciclo a partir de la
   simulación del Paso 2.
3. Revisa la matriz: cada columna es un período (abril/septiembre) y cada fila un ciclo.
   Abajo se muestran las horas de docencia y los **docentes requeridos** por período
   (tiempo completo = 18 h, medio tiempo = 12 h, tiempo parcial cubre el residuo).

Otros botones: **"REFRESCAR"** (recarga los datos, útil si cambiaste las metas del
Paso 2), **"ELIMINAR SELECCIONADA"** (borra una proyección guardada), **"Editar horas
malla"** (ajusta horas de un período concreto si la malla lo requiere) y **"Guardar
cambios"** (graba esos ajustes).

> 📷 [Captura 9: módulo Estudiantes con flechas a carrera/escenario → GENERAR PROYECCIÓN → matriz]

✅ **Listo cuando:** la matriz muestra las cohortes avanzando y los totales por período.

---

### Paso 4 — Recursos y Depreciación

**Menú:** 3 · Costos y recursos → **Recursos y Depreciación**

**Objetivo:** registrar los **activos fijos** (equipos, mobiliario, laboratorios). Definen
la inversión inicial, y su depreciación anual es parte del costo que el arancel debe cubrir.

**Qué hacer, botón por botón:**

| Botón | Qué hace | Por qué |
|---|---|---|
| **Generar activos por defecto** | Crea la lista típica de activos (computadoras, proyectores, mobiliario…) con cantidades y precios de referencia. | Ahorra registrar uno por uno; luego solo ajustas. |
| **Nuevo** | Agrega un activo puntual. | Para equipos específicos de la carrera. |
| **Editar selección** | Modifica el activo marcado (cantidad, precio, vida útil). | Para ajustar los valores por defecto a tu realidad. |
| **Eliminar selección** | Quita un activo. | Si no aplica a la carrera. |
| **Guardar** | Graba el formulario abierto. | Después de crear/editar. |
| **Cargar / Recalcular depreciación** | Reconstruye la tabla de depreciación anual con los activos actuales. | Siempre después de cambiar activos. |
| **Cargar inversiones** | Muestra la matriz de inversiones futuras (reposiciones). | Para revisar cuándo se repone cada activo. |

> 📷 [Captura 10: Recursos y Depreciación con flechas a Generar activos por defecto → lista → Cargar/Recalcular depreciación]

✅ **Listo cuando:** hay activos en la lista y la tabla de depreciación muestra valores por año.

---

### Paso 5 — Mantenimiento e Inversión

**Menú:** 3 · Costos y recursos → **Mantenimiento e Inversión**

**Objetivo:** completar el costo operativo (servicios y mantenimiento), los activos
diferidos, y cerrar la **inversión inicial total**.

Tiene **tres pestañas**:

#### Pestaña "Mantenimiento"

- Pulsa **"Generar servicios por defecto"**: crea agua, luz, internet, teléfono, etc.
  con valores de referencia.
- **"+ Nuevo"** agrega un servicio o rubro de mantenimiento específico.
- **"Editar"** / **"Eliminar"** sobre la fila seleccionada; **"Guardar"** graba el formulario.
- La opción **"Específico del escenario"** permite que un rubro solo exista en un
  escenario (ej. solo en el Optimista).

> 📷 [Captura 11: pestaña Mantenimiento con flechas a Generar servicios por defecto → tabla]

#### Pestaña "Activos Diferidos"

- Registra gastos pre-operativos: licencias, permisos, gastos de constitución.
- Cada rubro se **amortiza** (por defecto 20 % anual, 5 años); la tabla de abajo muestra
  la amortización año a año.

#### Pestaña "Inversión Inicial"

- **Solo lectura**: consolida activos fijos (Paso 4) + activos diferidos + capital de
  trabajo + imprevistos. Revisa que el total tenga sentido.

> 📷 [Captura 12: pestaña Inversión Inicial con el total consolidado señalado]

✅ **Listo cuando:** la pestaña Inversión Inicial muestra el total consolidado sin ceros raros.

---

### Paso 6 — Demanda e Ingresos

**Menú:** 2 · Proyección académica → **Demanda e Ingresos**

**Objetivo:** definir **cuánto se cobra** (arancel) y **cuánto material se consume**;
con eso el sistema calcula los ingresos de la carrera.

Selecciona arriba la **Carrera** y el **Escenario**; luego recorre las pestañas:

#### Pestaña "1. Configuración de Arancel"

- Elige el **modo de arancel**:
  - **Óptimo (recomendado)**: usa el arancel calculado para que la carrera sea viable
    (VAN ≈ 0). El botón **"Usar arancel sugerido"** copia ese valor al formulario.
  - **Manual**: escribes el arancel y la matrícula a mano (ej. para evaluar un precio político).
  - **Costo de la carrera (referencial)**: usa el costo por semestre como referencia.
- **"+ Nueva configuración"** crea una configuración; **"Guardar"** la graba.
  **"Usar % institucional"** aplica el porcentaje de matrícula definido por el administrador.
- Si la carrera ofrece **descuentos por ciclo**: **"Editar descuentos"** → escribe los %
  por ciclo → **"Guardar descuentos"**.

> 📷 [Captura 13: pestaña 1 con flechas a modo de arancel → Usar arancel sugerido → Guardar]

#### Pestaña "2. Demanda Proyectada"

Solo lectura: cuántos estudiantes hay por ciclo y período (viene del Paso 3), más la
fila de **Docentes Requeridos**.

#### Pestaña "3. Presupuestos y Seguro"

Solo lectura: presupuestos institucionales, seguro estudiantil y becas instituciones
(los parámetros los define el administrador en Datos Institucionales).

#### Pestaña "4. Ingresos Proyectados"

Solo lectura: la matriz de ingresos (arancel + matrícula − descuentos − becas) por
ciclo y período. **Aquí verificas que el dinero entra.**

#### Pestaña "5. Materiales en Cantidades"

- Pulsa **"Generar consumos por defecto"**: crea los consumos típicos (papel, marcadores,
  insumos de aseo…) por estudiante/docente/período.
- **"+ Nuevo consumo"** agrega un material puntual: eliges Categoría, Concepto, el
  consumo, la Unidad (por estudiante, por estudiante/mes, fijo por período, por docente)
  y el **Item de Capital de Trabajo** vinculado (de ahí sale el precio).
- **"Editar"** / **"Eliminar"** en cada fila de la tabla.
- Abajo, **"Cantidades calculadas por período"** muestra el resultado.

> 📷 [Captura 14: pestaña 5 con flechas a Generar consumos por defecto → tabla de consumos → cantidades por período]

#### Pestaña "6. Materiales Monetarios"

Solo lectura: las cantidades × precio × inflación = costo de materiales por período,
con el **TOTAL MATERIALES** al pie.

✅ **Listo cuando:** la pestaña "4. Ingresos Proyectados" muestra ingresos distintos de cero.

---

### Paso 7 — Costos y Gastos

**Menú:** 3 · Costos y recursos → **Costos y Gastos**

**Objetivo:** punto de control. **No hay nada que llenar**: el módulo junta
automáticamente sueldos, materiales, mantenimiento, depreciación e inversiones.

**Qué revisar:**

- **Pestaña "1. Inv. Vin. Becas"**: los montos de investigación, vinculación y becas
  calculados con los % institucionales.
- **Pestaña "2. Costos y Gastos"**: la matriz completa de costos por período y el total.
- **Pestaña "3. Costo de la carrera (referencial)"**: cuánto cuesta formar a **un**
  estudiante durante toda la carrera y el **Costo por Semestre** (referencial — es el
  costo, no lo que se cobra).

> 📷 [Captura 15: pestaña Costo de la carrera con las tarjetas de costo señaladas]

✅ **Listo cuando:** el costo por estudiante se ve razonable (ni cero ni cientos de miles).
Si algo se ve mal, el error está en los pasos 2-6.

---

### Paso 8 — Análisis Financiero

**Menú:** 4 · Financiamiento y análisis → **Análisis Financiero**

**Objetivo:** decidir el **arancel final** y verificar la viabilidad. Solo eliges carrera
y escenario; todo se calcula solo.

**Pestañas (en orden de lectura recomendado):**

| Pestaña | Qué muestra | Qué buscar |
|---|---|---|
| **3. TIR / VAN** | Los indicadores de viabilidad y la TMR (tasa mínima de rendimiento). | TIR ≥ TMR y VAN ≥ 0 ⇒ viable. |
| **6. Arancel Óptimo** | El arancel que hace **VAN ≈ 0**: el valor a cobrar para que la carrera se pague sola. | Este es el arancel que se propone. |
| **1. Pérdidas y Ganancias** | Ingresos − costos por período. | Períodos en pérdida al inicio son normales. |
| **2. Flujo de Fondos** | El flujo de caja incluida la inversión inicial. | La base del VAN/TIR. |
| **4. Período de Recuperación** | En cuántos períodos se recupera la inversión. | — |
| **5. Punto de Equilibrio** | Cuántos estudiantes/ingresos se necesitan para no perder. | Se calcula con el último período proyectado. |
| **7. Dashboard Financiero** | Resumen gráfico de todo. | Para presentaciones. |
| **8. CES / INF CES** | Los cuadros regulatorios para justificar el arancel ante el CES. | Se llenan solos; solo el "costo de carreras similares" es editable (dato externo). |
| **9. Balance Proyectado** | Activo, pasivo y patrimonio proyectados. | — |

> 📷 [Captura 16: pestaña TIR/VAN con los indicadores señalados]
> 📷 [Captura 17: pestaña Arancel Óptimo con el valor final señalado]
> 📷 [Captura 18: pestaña CES / INF CES]

✅ **Listo cuando:** el VAN ≈ 0 con el arancel óptimo y la TIR ≥ TMR.

---

### Paso 9 — Reportes

**Menú:** 5 · Resultados → **Reportes**

**Objetivo:** generar el **documento final** en PDF o Excel.

**Qué hacer:**

1. Selecciona la **carrera** y el **escenario**.
2. Espera a que las tarjetas de resumen carguen (verás los valores clave en pantalla,
   organizados por pestañas: Resumen, Estudiantes, CES, Indicadores…).
3. Elige la **dirección destinataria** (o "Completo" para todas las secciones).
4. Pulsa **"Exportar PDF"** o **"Exportar XLSX"** y elige dónde guardar.
   (**"Exportar PDF por dirección"** genera el informe de la dirección elegida.)

El informe empieza con el **"Resumen de Indicadores Clave"**: estudiantes, costo por
estudiante, costo de la carrera, arancel óptimo, matrícula, VAN, TIR y punto de
equilibrio — pensado para lectura rápida y para la planilla de validación.

> 📷 [Captura 19: módulo Reportes con flechas a carrera/escenario → dirección → Exportar PDF]
> 📷 [Captura 20: primera página del PDF con el Resumen de Indicadores Clave]

✅ **Listo cuando:** el archivo se genera y abre sin errores.

---

## 5. Módulos de apoyo

Estos módulos alimentan el flujo pero requieren poca o ninguna intervención:

### Inflación (1 · Configuración base)

Registra la **tasa de inflación anual** que ajusta costos e ingresos futuros. Pulsa el
año, escribe el % y **"Guardar"**. También permite importar series históricas.

### Capital de Trabajo (3 · Costos y recursos)

Solo lectura para el usuario final: muestra el efectivo necesario para operar los
primeros meses (los "meses de capital de trabajo" los define el administrador).
Revisa que el total sea coherente.

> 📷 [Captura 21: Capital de Trabajo con el total señalado]

---

## 6. Módulos del administrador

> Si no eres administrador normalmente **no verás** estos módulos en el menú.

| Módulo | Para qué sirve |
|---|---|
| **Gestión de Usuarios** | Crear usuarios, asignar roles (Administrador, Financiero…) y permisos individuales. Botones: "+ Nuevo Usuario", editar y eliminar por fila. |
| **Auditoría** | Historial de acciones críticas: quién creó/editó/eliminó qué y cuándo. Solo lectura con filtros. |
| **Datos Institucionales** | Parámetros globales que alimentan todos los cálculos: tasas para la TMR, % de investigación/vinculación/becas, % de matrícula, meses de capital de trabajo, financiamiento (préstamo/convenio), impuestos y parámetros de la búsqueda del arancel óptimo. Botón "Editar" → cambiar → "Guardar". |
| **Sueldos Carrera** | Cargos docentes y administrativos con sueldos y horas; genera el costo de personal por período. |
| **Aporte Planta Central** | Prorratea el costo de la administración central de la universidad a la carrera. |
| **Amortización** | Fuentes de financiamiento de la inversión y la tabla de amortización del préstamo (cuota fija). El interés alimenta los costos y el flujo. |
| **Catálogos** | Cargos y materiales disponibles para asignar en los demás módulos. |

> 📷 [Captura 22: Gestión de Usuarios]
> 📷 [Captura 23: Datos Institucionales]

---

## 7. Preguntas frecuentes

**La aplicación no abre o muestra error de conexión.**
Falta o está mal el archivo `appsettings.Local.json` (la conexión a la base de datos).
Pídelo al administrador y colócalo junto al ejecutable.

**"Error al guardar carrera: duplicate key… IX_carrera_codigo".**
Ya existe una carrera (incluso eliminada) con ese código. Desde la versión actual el
sistema la reactiva automáticamente; si ves este error, actualiza la aplicación.

**Los números del Análisis Financiero salen en cero.**
Falta algún paso previo: revisa en orden Retención (Paso 2) → Estudiantes (Paso 3) →
Demanda (Paso 6). El badge "?" de cada módulo te dice qué debe existir.

**El costo por estudiante sale gigante.**
Casi siempre son los **consumos de materiales** (Paso 6, pestaña 5): revisa las unidades
(ej. "por estudiante/mes" multiplica por los meses del período).

**Cambié las metas de retención y nada cambió en Estudiantes.**
Vuelve a **"Ejecutar simulación"** (Paso 2) y luego **"GENERAR PROYECCIÓN"** (Paso 3):
las proyecciones guardadas no se recalculan solas.

**¿Por qué el arancel del CES no coincide con el "Costo por Semestre"?**
Son cosas distintas: el **Costo por Semestre** es lo que *cuesta* (referencial); el
**arancel** es lo que se *cobra*. El CES reporta el arancel óptimo (VAN ≈ 0) y muestra
el costo referencial al lado para comparar.

**Se cerró la sesión sola.**
La sesión tiene duración fija por seguridad. Vuelve a iniciar sesión; el trabajo
guardado no se pierde.

**La ventana se ve cortada en mi laptop.**
La aplicación se adapta a la pantalla y se maximiza en monitores pequeños. Si la ves
cortada, ciérrala y ábrela de nuevo.

---

## 8. Glosario

| Término | Significado |
|---|---|
| **Arancel** | Lo que el estudiante **paga** por semestre (sin matrícula). |
| **Matrícula** | Pago adicional por inscripción (típicamente 10 % del arancel). |
| **Arancel óptimo** | El arancel que hace VAN ≈ 0: cubre exactamente todos los costos e inversiones. |
| **Costo por Semestre** | Lo que *cuesta* formar a un estudiante un semestre (referencial; no es lo que se cobra). |
| **Costo de la carrera** | El costo por estudiante acumulado de todos los semestres de la malla. |
| **Meta de retención** | % de estudiantes que llega a la mitad de la carrera respecto de los que entraron. |
| **Meta de graduación** | % de la segunda mitad que se gradúa. |
| **Tasa por ciclo** | El % ciclo a ciclo que el sistema deriva de las metas (meta^(1/pasos)). |
| **Cohorte / Grupo de ingreso** | El grupo de estudiantes que entra junto en un año. |
| **Paralelo** | Sección o grupo de clase de un mismo ciclo. |
| **VAN** | Valor Actual Neto: cuánto vale hoy el proyecto descontando los flujos futuros. ≥ 0 es viable. |
| **TIR** | Tasa Interna de Retorno: la rentabilidad del proyecto. Debe superar la TMR. |
| **TMR** | Tasa Mínima de Rendimiento exigida (tasa financiera + premio al riesgo). |
| **Punto de equilibrio** | Nivel de ingresos/estudiantes donde no se gana ni se pierde. |
| **Capital de trabajo** | Efectivo para operar los primeros meses antes de que entren ingresos. |
| **Activo diferido** | Gasto pre-operativo (licencias, permisos) que se amortiza en varios años. |
| **Depreciación** | Pérdida de valor anual de los activos fijos, contabilizada como costo. |
| **Escenario** | Variante de la proyección: Histórico (base), Optimista y Pesimista. |
| **CES** | Consejo de Educación Superior (Ecuador), ante quien se justifica el arancel. |

---

*Sistema de Aranceles Universitarios — guía de usuario. Las capturas de pantalla se
insertan en los marcadores 📷 numerados.*
