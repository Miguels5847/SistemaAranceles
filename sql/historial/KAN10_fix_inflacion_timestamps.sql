-- =============================================================
-- KAN-10 HOTFIX: Normalizar columnas de fecha en tablas de inflación
--
-- SINTOMA:
--   Error al cargar inflación anual: Reading as 'System.DateTime'
--   is not supported for fields having DataTypeName 'text'.
--
-- CAUSA PROBABLE:
--   Las columnas de auditoría/fecha fueron creadas como TEXT en
--   PostgreSQL/Supabase, pero EF Core las lee como DateTime.
--
-- EJECUTAR EN:
--   Supabase SQL Editor
--
-- EFECTO:
--   Convierte las columnas de fecha de inflación a timestamptz.
-- =============================================================

DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'inflacion_anual'
          AND column_name = 'creado_en'
          AND data_type = 'text'
    ) THEN
        ALTER TABLE public.inflacion_anual
            ALTER COLUMN creado_en TYPE timestamptz USING creado_en::timestamptz,
            ALTER COLUMN actualizado_en TYPE timestamptz USING CASE WHEN actualizado_en IS NULL THEN NULL ELSE actualizado_en::timestamptz END,
            ALTER COLUMN eliminado_en TYPE timestamptz USING CASE WHEN eliminado_en IS NULL THEN NULL ELSE eliminado_en::timestamptz END;
    END IF;

    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name = 'inflacion_proyectada'
          AND column_name = 'creado_en'
          AND data_type = 'text'
    ) THEN
        ALTER TABLE public.inflacion_proyectada
            ALTER COLUMN creado_en TYPE timestamptz USING creado_en::timestamptz,
            ALTER COLUMN actualizado_en TYPE timestamptz USING CASE WHEN actualizado_en IS NULL THEN NULL ELSE actualizado_en::timestamptz END,
            ALTER COLUMN eliminado_en TYPE timestamptz USING CASE WHEN eliminado_en IS NULL THEN NULL ELSE eliminado_en::timestamptz END;
    END IF;
END$$;

-- Verificación rápida
SELECT table_name, column_name, data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name IN ('inflacion_anual', 'inflacion_proyectada')
  AND column_name IN ('creado_en', 'actualizado_en', 'eliminado_en')
ORDER BY table_name, column_name;