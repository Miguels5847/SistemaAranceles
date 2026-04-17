-- =============================================================
-- KAN14 Fase 3 - Lote 02 (Académico-Operativo) - POSTCHECK
-- Objetivo: validar tipos finales y sanidad basica tras APPLY.
-- =============================================================

SET lock_timeout = '5s';
SET statement_timeout = '120s';

-- 1) Verificacion de tipos finales esperados
SELECT
    c.table_name,
    c.column_name,
    c.data_type,
    c.udt_name
FROM information_schema.columns c
WHERE c.table_schema = 'public'
  AND (
      (c.table_name, c.column_name) IN (
          ('carrera', 'creado_en'),
          ('carrera', 'actualizado_en'),
          ('carrera', 'eliminado_en'),
          ('escenario_proyeccion', 'creado_en'),
          ('escenario_proyeccion', 'actualizado_en'),
          ('escenario_proyeccion', 'eliminado_en'),
          ('periodo_academico', 'creado_en'),
          ('periodo_academico', 'actualizado_en'),
          ('periodo_academico', 'eliminado_en'),
          ('configuracion_carga_docente', 'creado_en'),
          ('configuracion_carga_docente', 'actualizado_en'),
          ('configuracion_carga_docente', 'eliminado_en'),
          ('detalle_proyeccion_estudiantes', 'creado_en'),
          ('detalle_proyeccion_estudiantes', 'actualizado_en'),
          ('detalle_proyeccion_estudiantes', 'eliminado_en'),
          ('detalle_simulacion_retencion', 'creado_en'),
          ('detalle_simulacion_retencion', 'actualizado_en'),
          ('detalle_simulacion_retencion', 'eliminado_en'),
          ('simulacion_retencion', 'creado_en'),
          ('simulacion_retencion', 'actualizado_en'),
          ('simulacion_retencion', 'eliminado_en')
      )
  )
ORDER BY c.table_name, c.column_name;

-- 2) Conteo de filas por tabla (comparar con precheck)
SELECT 'carrera' AS tabla, COUNT(*) AS filas FROM public.carrera
UNION ALL
SELECT 'escenario_proyeccion', COUNT(*) FROM public.escenario_proyeccion
UNION ALL
SELECT 'periodo_academico', COUNT(*) FROM public.periodo_academico
UNION ALL
SELECT 'configuracion_carga_docente', COUNT(*) FROM public.configuracion_carga_docente
UNION ALL
SELECT 'detalle_proyeccion_estudiantes', COUNT(*) FROM public.detalle_proyeccion_estudiantes
UNION ALL
SELECT 'detalle_simulacion_retencion', COUNT(*) FROM public.detalle_simulacion_retencion
UNION ALL
SELECT 'simulacion_retencion', COUNT(*) FROM public.simulacion_retencion
ORDER BY tabla;

-- 3) Muestras rapidas de coherencia temporal
SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.carrera
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.escenario_proyeccion
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.periodo_academico
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.configuracion_carga_docente
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.detalle_proyeccion_estudiantes
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.detalle_simulacion_retencion
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.simulacion_retencion
ORDER BY id DESC
LIMIT 10;