# KAN-03 - Diseno de Base de Datos

## 1. Objetivo

Definir un modelo relacional mantenible y entendible para los 11 modulos del sistema, alineado a Arquitectura Limpia y a los requerimientos funcionales validados.

## 2. Decisiones de diseno confirmadas

- Convencion en base de datos: snake_case.
- Convencion en C#: PascalCase.
- Seguridad: correo institucional unico por usuario y contrasena con hash BCrypt.
- Precision numerica:
  - Montos monetarios: decimal(18,2).
  - Tasas y porcentajes: decimal(9,4).

## 3. Estrategia de borrado recomendada (hibrida)

Para maximizar mantenibilidad y trazabilidad para usuarios no tecnicos:

- Borrado logico en tablas de negocio y catalogos:
  - Campos base: esta_activo, eliminado_en, eliminado_por_usuario_id.
- Borrado fisico en tablas tecnicas de union o calculo temporal.

Esta estrategia evita perdida de historial y simplifica soporte administrativo.

## 4. Estrategia de auditoria recomendada

Se adopta una sola tabla global auditoria_log con columnas legibles para administradores:

- evento_en
- modulo_nombre
- entidad_nombre
- entidad_id
- accion_nombre (CREAR, ACTUALIZAR, SUSPENDER, INICIAR_SESION, EXPORTAR)
- resumen_texto (texto claro para usuario no tecnico)
- valores_anteriores_json
- valores_nuevos_json
- ejecutado_por_usuario_id

Ventajas:

- Consulta centralizada.
- Reportes mas simples.
- Menor complejidad de mantenimiento.

## 5. Modelo propuesto v1 (29 tablas)

### 5.1 Seguridad y acceso

1. usuario
2. rol
3. permiso
4. rol_permiso
5. usuario_rol
6. sesion_usuario
7. auditoria_log

### 5.2 Catalogos y contexto academico

8. carrera
9. periodo_academico
10. escenario_proyeccion

### 5.3 Modulo Inflacion

11. inflacion_anual
12. inflacion_proyectada

### 5.4 Modulo Tasa de Retencion

13. configuracion_retencion
14. criterio_referencia_retencion
15. simulacion_retencion
16. detalle_simulacion_retencion

### 5.5 Modulo Estudiantes

17. proyeccion_estudiantes
18. detalle_proyeccion_estudiantes
19. configuracion_carga_docente
20. proyeccion_requerimiento_docente

### 5.6 Modulo Sueldos y Planta Central

21. cargo_facultad
22. proyeccion_cargo_facultad
23. cargo_planta_central
24. proyeccion_cargo_planta_central

### 5.7 Modulo Demanda e Ingresos

25. configuracion_arancel
26. presupuesto_institucional
27. item_material_insumo
28. proyeccion_material_insumo

### 5.8 Modulos transversales financieros

29. resumen_proyeccion_financiera

## 6. Criterios de nomenclatura para legibilidad

- Tablas en singular y nombre explicito: usuario, configuracion_retencion, proyeccion_material_insumo.
- Evitar abreviaturas ambiguas.
- Nombres funcionales por dominio, no por tecnologia.

## 7. Campos base estandar por tabla de negocio

- id (PK, entero autoincremental)
- creado_en
- creado_por_usuario_id
- actualizado_en
- actualizado_por_usuario_id
- esta_activo
- eliminado_en (nullable)
- eliminado_por_usuario_id (nullable)

## 8. Indices minimos recomendados

- Unicos:
  - usuario.correo_institucional
  - rol.nombre
  - permiso.codigo
  - carrera.codigo
  - inflacion_anual.anio
- Busqueda:
  - auditoria_log(evento_en, modulo_nombre)
  - configuracion_retencion(carrera_id, escenario_proyeccion_id)
  - proyeccion_estudiantes(carrera_id, escenario_proyeccion_id)

## 9. Orden de migraciones EF Core (KAN-03)

1. Base de seguridad: usuarios, roles, permisos y auditoria.
2. Catalogos: carrera, periodos y escenarios.
3. Inflacion.
4. Retencion y simulacion.
5. Estudiantes.
6. Sueldos y planta central.
7. Demanda e ingresos.
8. Resumen financiero.

## 10. Alcance de este documento

Este archivo define el modelo de datos objetivo para KAN-03.
La implementacion de entidades de dominio corresponde a KAN-04 y el contexto de datos/repositorio a KAN-05.
