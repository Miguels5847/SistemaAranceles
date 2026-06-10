-- KAN-20_fix_cargo_facultad_schema.sql
-- Corrige el esquema en las tablas EF (singulares) cargo_facultad y item_material_insumo.
-- Los scripts anteriores fix_boolean_columns_capital_trabajo.sql y create_capital_trabajo_tables.sql
-- operaban sobre tablas PLURALES (cargos_facultad, items_material_insumo) que son tablas distintas.
-- EF usa tablas SINGULARES. Este script arregla las tablas correctas.
--
-- Ejecutar en Supabase > SQL Editor antes de abrir Capital de Trabajo.

BEGIN;

-- ============================================================
-- 1. cargo_facultad.es_cargo_docente: INTEGER → boolean
-- ============================================================
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name   = 'cargo_facultad'
      AND column_name  = 'es_cargo_docente'
      AND data_type IN ('integer','smallint','bigint')
  ) THEN
    RAISE NOTICE 'Convirtiendo cargo_facultad.es_cargo_docente a boolean...';
    ALTER TABLE public.cargo_facultad
      ALTER COLUMN es_cargo_docente TYPE boolean
        USING (es_cargo_docente::int <> 0);
    RAISE NOTICE 'Listo.';
  ELSE
    RAISE NOTICE 'cargo_facultad.es_cargo_docente ya es boolean o no existe. OK.';
  END IF;
END$$;

-- ============================================================
-- 2. cargo_facultad.cantidad_default: agregar si no existe
-- ============================================================
ALTER TABLE public.cargo_facultad
  ADD COLUMN IF NOT EXISTS cantidad_default numeric(9,2) NOT NULL DEFAULT 1;

-- ============================================================
-- 3. item_material_insumo.es_cantidad_fija: INTEGER → boolean
-- ============================================================
DO $$
BEGIN
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'public'
      AND table_name   = 'item_material_insumo'
      AND column_name  = 'es_cantidad_fija'
      AND data_type IN ('integer','smallint','bigint')
  ) THEN
    RAISE NOTICE 'Convirtiendo item_material_insumo.es_cantidad_fija a boolean...';
    ALTER TABLE public.item_material_insumo
      ALTER COLUMN es_cantidad_fija TYPE boolean
        USING (es_cantidad_fija::int <> 0);
    RAISE NOTICE 'Listo.';
  ELSE
    RAISE NOTICE 'item_material_insumo.es_cantidad_fija ya es boolean o no existe. OK.';
  END IF;
END$$;

COMMIT;

-- ============================================================
-- Verificación post-fix
-- ============================================================
SELECT
  table_name,
  column_name,
  data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name IN ('cargo_facultad', 'item_material_insumo')
  AND column_name IN ('es_cargo_docente', 'es_cantidad_fija', 'cantidad_default')
ORDER BY table_name, column_name;
