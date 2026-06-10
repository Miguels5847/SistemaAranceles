-- fix_boolean_columns_capital_trabajo.sql
-- Convierte columnas tipo entero a boolean en las tablas usadas por la semilla
-- Ejecutar en Supabase (staging) antes de correr KAN-20 seed si hay errores de tipo boolean/integer.

DO $$
BEGIN
  -- cargos_facultad.es_cargo_docente: integer -> boolean
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema='public' AND table_name='cargos_facultad' AND column_name='es_cargo_docente' AND data_type IN ('integer','smallint','bigint')
  ) THEN
    RAISE NOTICE 'Altering cargos_facultad.es_cargo_docente to boolean';
    ALTER TABLE public.cargos_facultad
      ALTER COLUMN es_cargo_docente TYPE boolean USING (CASE WHEN es_cargo_docente IS NULL THEN NULL WHEN es_cargo_docente::text ~ '^[0-9]+$' THEN (es_cargo_docente::int <> 0) ELSE (es_cargo_docente::text IN ('t','true','y','yes')) END);
  END IF;

  -- items_material_insumo.es_cantidad_fija: integer -> boolean (si existe)
  IF EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema='public' AND table_name='items_material_insumo' AND column_name='es_cantidad_fija' AND data_type IN ('integer','smallint','bigint')
  ) THEN
    RAISE NOTICE 'Altering items_material_insumo.es_cantidad_fija to boolean';
    ALTER TABLE public.items_material_insumo
      ALTER COLUMN es_cantidad_fija TYPE boolean USING (CASE WHEN es_cantidad_fija IS NULL THEN NULL WHEN es_cantidad_fija::text ~ '^[0-9]+$' THEN (es_cantidad_fija::int <> 0) ELSE (es_cantidad_fija::text IN ('t','true','y','yes')) END);
  END IF;
END$$;

-- Verificar tipos resultantes
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_schema='public' AND table_name IN ('cargos_facultad','items_material_insumo')
AND column_name IN ('es_cargo_docente','es_cantidad_fija');
