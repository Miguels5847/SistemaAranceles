-- =============================================================
-- KAN14 Fase 3 - Lote 03 (Costos/Operativo) - POSTCHECK
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
          ('cargo_facultad', 'creado_en'),
          ('cargo_facultad', 'actualizado_en'),
          ('cargo_facultad', 'eliminado_en'),
          ('cargo_planta_central', 'creado_en'),
          ('cargo_planta_central', 'actualizado_en'),
          ('cargo_planta_central', 'eliminado_en'),
          ('configuracion_retencion', 'creado_en'),
          ('configuracion_retencion', 'actualizado_en'),
          ('configuracion_retencion', 'eliminado_en'),
          ('criterio_referencia_retencion', 'creado_en'),
          ('criterio_referencia_retencion', 'actualizado_en'),
          ('criterio_referencia_retencion', 'eliminado_en'),
          ('item_material_insumo', 'creado_en'),
          ('item_material_insumo', 'actualizado_en'),
          ('item_material_insumo', 'eliminado_en'),
          ('proyeccion_cargo_facultad', 'creado_en'),
          ('proyeccion_cargo_facultad', 'actualizado_en'),
          ('proyeccion_cargo_facultad', 'eliminado_en'),
          ('proyeccion_cargo_planta_central', 'creado_en'),
          ('proyeccion_cargo_planta_central', 'actualizado_en'),
          ('proyeccion_cargo_planta_central', 'eliminado_en'),
          ('proyeccion_material_insumo', 'creado_en'),
          ('proyeccion_material_insumo', 'actualizado_en'),
          ('proyeccion_material_insumo', 'eliminado_en'),
          ('proyeccion_requerimiento_docente', 'creado_en'),
          ('proyeccion_requerimiento_docente', 'actualizado_en'),
          ('proyeccion_requerimiento_docente', 'eliminado_en')
      )
  )
ORDER BY c.table_name, c.column_name;

-- 2) Conteo de filas por tabla (comparar con precheck)
SELECT 'cargo_facultad' AS tabla, COUNT(*) AS filas FROM public.cargo_facultad
UNION ALL
SELECT 'cargo_planta_central', COUNT(*) FROM public.cargo_planta_central
UNION ALL
SELECT 'configuracion_retencion', COUNT(*) FROM public.configuracion_retencion
UNION ALL
SELECT 'criterio_referencia_retencion', COUNT(*) FROM public.criterio_referencia_retencion
UNION ALL
SELECT 'item_material_insumo', COUNT(*) FROM public.item_material_insumo
UNION ALL
SELECT 'proyeccion_cargo_facultad', COUNT(*) FROM public.proyeccion_cargo_facultad
UNION ALL
SELECT 'proyeccion_cargo_planta_central', COUNT(*) FROM public.proyeccion_cargo_planta_central
UNION ALL
SELECT 'proyeccion_material_insumo', COUNT(*) FROM public.proyeccion_material_insumo
UNION ALL
SELECT 'proyeccion_requerimiento_docente', COUNT(*) FROM public.proyeccion_requerimiento_docente
ORDER BY tabla;

-- 3) Muestras rapidas de coherencia temporal
SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.cargo_facultad
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.cargo_planta_central
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.configuracion_retencion
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.criterio_referencia_retencion
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.item_material_insumo
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.proyeccion_cargo_facultad
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.proyeccion_cargo_planta_central
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.proyeccion_material_insumo
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.proyeccion_requerimiento_docente
ORDER BY id DESC
LIMIT 10;