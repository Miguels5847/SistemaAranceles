# KAN-03 - Diccionario de Datos Inicial

## Modulo Seguridad

### usuario

- Finalidad: cuentas de acceso del sistema.
- Campos clave: id, nombre_completo, correo_institucional, hash_contrasena, estado, ultimo_acceso_en.
- Restricciones: correo_institucional unico.

### rol

- Finalidad: perfiles de acceso (Administrador, Planificador, Consultor).
- Campos clave: id, nombre, descripcion.
- Restricciones: nombre unico.

### permiso

- Finalidad: permisos por modulo y accion.
- Campos clave: id, codigo, modulo_nombre, accion_nombre, descripcion.
- Restricciones: codigo unico.

### rol_permiso

- Finalidad: relacion N:M entre roles y permisos.
- Campos clave: rol_id, permiso_id.

### usuario_rol

- Finalidad: relacion N:M entre usuarios y roles.
- Campos clave: usuario_id, rol_id.

### sesion_usuario

- Finalidad: sesiones autenticadas y expiracion por inactividad.
- Campos clave: id, usuario_id, token_sesion, emitido_en, expira_en, revocado_en.

### auditoria_log

- Finalidad: auditoria global legible para administradores.
- Campos clave: id, evento_en, modulo_nombre, entidad_nombre, entidad_id, accion_nombre, resumen_texto, valores_anteriores_json, valores_nuevos_json, ejecutado_por_usuario_id.

## Catalogos

### carrera

- Finalidad: carreras del sistema.
- Campos clave: id, codigo, nombre, facultad_nombre, total_ciclos.
- Restricciones: codigo unico.

### periodo_academico

- Finalidad: periodos por anio (P1, P2).
- Campos clave: id, anio, numero_periodo, etiqueta_periodo, fecha_inicio, fecha_fin.
- Restricciones: (anio, numero_periodo) unico.

### escenario_proyeccion

- Finalidad: escenarios de simulacion por carrera.
- Campos clave: id, carrera_id, nombre, descripcion, es_predeterminado.

## Modulo Inflacion

### inflacion_anual

- Finalidad: inflacion historica anual.
- Campos clave: id, anio, porcentaje_inflacion, fuente_nombre, tipo_fuente.
- Restricciones: anio unico.

### inflacion_proyectada

- Finalidad: inflacion proyectada por anio y escenario.
- Campos clave: id, escenario_proyeccion_id, anio, porcentaje_inflacion, metodo_proyeccion, es_ajuste_manual.

## Modulo Tasa de Retencion

### configuracion_retencion

- Finalidad: configuracion de tasas por carrera y escenario.
- Campos clave: id, carrera_id, escenario_proyeccion_id, total_ciclos, tasa_retencion_porcentaje, tasa_graduacion_porcentaje, estudiantes_periodo_1, estudiantes_periodo_2, paralelos_periodo_1, paralelos_periodo_2.

### criterio_referencia_retencion

- Finalidad: criterios informativos de acreditacion.
- Campos clave: id, configuracion_retencion_id, meta_retencion_porcentaje, meta_graduacion_porcentaje.

### simulacion_retencion

- Finalidad: cabecera de corrida de simulacion.
- Campos clave: id, configuracion_retencion_id, ejecutado_en, notas.

### detalle_simulacion_retencion

- Finalidad: detalle por ciclo y periodo de simulacion.
- Campos clave: id, simulacion_retencion_id, numero_ciclo, numero_periodo, valor_estudiantes, tasa_aplicada_porcentaje, tipo_zona.

## Modulo Estudiantes

### proyeccion_estudiantes

- Finalidad: cabecera de proyeccion de estudiantes.
- Campos clave: id, carrera_id, escenario_proyeccion_id, anio_base, semanas_por_semestre.

### detalle_proyeccion_estudiantes

- Finalidad: estudiantes proyectados por ciclo, anio y periodo.
- Campos clave: id, proyeccion_estudiantes_id, periodo_academico_id, numero_ciclo, cantidad_paralelos, total_estudiantes.

### configuracion_carga_docente

- Finalidad: configuracion de horas y parametros docentes.
- Campos clave: id, proyeccion_estudiantes_id, horas_docencia_estandar, horas_tecnico_estandar, proporcion_phd_porcentaje, proporcion_mgs_porcentaje.

### proyeccion_requerimiento_docente

- Finalidad: docentes requeridos por periodo.
- Campos clave: id, configuracion_carga_docente_id, periodo_academico_id, total_docentes, docentes_phd, docentes_mgs, docentes_parcial, docentes_tecnico.

## Modulo Sueldos y Planta Central

### cargo_facultad

- Finalidad: cargos de facultad.
- Campos clave: id, carrera_id, nombre_cargo, tipo_cargo, sueldo_base_mensual, es_cargo_docente.

### proyeccion_cargo_facultad

- Finalidad: costo proyectado de cargos de facultad por periodo.
- Campos clave: id, cargo_facultad_id, periodo_academico_id, cantidad_personas, factor_ponderacion, factor_inflacion, costo_total_semestre.

### cargo_planta_central

- Finalidad: cargos de planta central.
- Campos clave: id, nombre_cargo, sueldo_mensual_total.

### proyeccion_cargo_planta_central

- Finalidad: costo atribuido a la carrera por periodo.
- Campos clave: id, cargo_planta_central_id, carrera_id, periodo_academico_id, proporcion_asignacion, costo_total_semestre.

## Modulo Demanda e Ingresos

### configuracion_arancel

- Finalidad: arancel y matricula por escenario.
- Campos clave: id, carrera_id, escenario_proyeccion_id, valor_arancel, valor_matricula, tipo_origen.

### presupuesto_institucional

- Finalidad: presupuestos institucionales.
- Campos clave: id, tipo_presupuesto, valor_anual_base, ajustable_por_inflacion.

### item_material_insumo

- Finalidad: catalogo de materiales e insumos.
- Campos clave: id, carrera_id, nombre_item, categoria_nombre, unidad_nombre, cantidad_base, precio_unitario, es_cantidad_fija.

### proyeccion_material_insumo

- Finalidad: costos proyectados de insumos por periodo.
- Campos clave: id, item_material_insumo_id, periodo_academico_id, cantidad_proyectada, factor_inflacion, costo_total_proyectado.

## Resumen Financiero

### resumen_proyeccion_financiera

- Finalidad: consolidado por periodo para consumo de modulos financieros.
- Campos clave: id, carrera_id, escenario_proyeccion_id, periodo_academico_id, ingreso_total, costo_servicios_total, gasto_administrativo_total, gasto_ventas_total, otros_gastos_total, gasto_financiero_total, resultado_neto_total.

## Campos base de trazabilidad

Para tablas de negocio aplicar:

- creado_en, creado_por_usuario_id
- actualizado_en, actualizado_por_usuario_id
- esta_activo, eliminado_en, eliminado_por_usuario_id
