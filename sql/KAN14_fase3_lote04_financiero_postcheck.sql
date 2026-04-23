-- =============================================================
-- KAN14 Fase 3 - Lote 04 (Financiero) - POSTCHECK
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
          ('presupuesto_institucional', 'creado_en'),
          ('presupuesto_institucional', 'actualizado_en'),
          ('presupuesto_institucional', 'eliminado_en'),
          ('proyeccion_estudiantes', 'creado_en'),
          ('proyeccion_estudiantes', 'actualizado_en'),
          ('proyeccion_estudiantes', 'eliminado_en'),
          ('resumen_proyeccion_financiera', 'creado_en'),
          ('resumen_proyeccion_financiera', 'actualizado_en'),
          ('resumen_proyeccion_financiera', 'eliminado_en'),
          ('configuracion_arancel', 'creado_en'),
          ('configuracion_arancel', 'actualizado_en'),
          ('configuracion_arancel', 'eliminado_en')
      )
  )
ORDER BY c.table_name, c.column_name;

-- 2) Conteo de filas por tabla (comparar con precheck)
SELECT 'presupuesto_institucional' AS tabla, COUNT(*) AS filas FROM public.presupuesto_institucional
UNION ALL
SELECT 'proyeccion_estudiantes', COUNT(*) FROM public.proyeccion_estudiantes
UNION ALL
SELECT 'resumen_proyeccion_financiera', COUNT(*) FROM public.resumen_proyeccion_financiera
UNION ALL
SELECT 'configuracion_arancel', COUNT(*) FROM public.configuracion_arancel
ORDER BY tabla;

-- 3) Muestras rapidas de coherencia temporal
SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.presupuesto_institucional
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.proyeccion_estudiantes
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.resumen_proyeccion_financiera
ORDER BY id DESC
LIMIT 10;

SELECT id, creado_en, actualizado_en, eliminado_en
FROM public.configuracion_arancel
ORDER BY id DESC
LIMIT 10;