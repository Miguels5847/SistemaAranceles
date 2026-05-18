-- =============================================================================
-- KAN-24: Categoría personalizada para activos fijos
-- Permite que la opción "Nueva" conserve el nombre escrito por el usuario
-- sin agregarlo como categoría permanente del catálogo.
-- Ejecutar en Supabase SQL Editor.
-- =============================================================================

BEGIN;

ALTER TABLE public.recurso_activo_fijo
ADD COLUMN IF NOT EXISTS categoria_personalizada VARCHAR(80) NOT NULL DEFAULT '';

UPDATE public.recurso_activo_fijo
SET categoria_personalizada = ''
WHERE categoria_personalizada IS NULL;

COMMIT;

-- Verificación
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'recurso_activo_fijo'
  AND column_name = 'categoria_personalizada';
