-- =============================================================
-- KAN-03 FIX CRÍTICO: Crear sequences para columnas `id` sin autoincrement
--
-- CAUSA: Las migraciones KAN-03 se generaron contra SQLite
-- (.Annotation("Sqlite:Autoincrement", true)). Npgsql ignoró
-- esa anotación → columnas `id` como INTEGER NOT NULL sin sequence.
-- Las migraciones KAN-04 sí generaron IDENTITY columns correctamente.
--
-- LÓGICA: Solo parchea tablas con id INTEGER que NO son IDENTITY
-- y NO tienen column_default → esas son las que fallan en INSERT.
--
-- EJECUTAR en: Supabase SQL Editor
-- ORDEN: Antes de KAN06_09_seed_datos_iniciales.sql
-- =============================================================

DO $$
DECLARE
    t        RECORD;
    seq_name TEXT;
    max_val  BIGINT;
BEGIN
    FOR t IN
        SELECT c.table_name
        FROM information_schema.columns c
        WHERE c.table_schema    = 'public'
          AND c.column_name     = 'id'
          AND c.data_type       = 'integer'
          AND c.column_default  IS NULL
          AND c.is_identity     = 'NO'   -- <-- excluye columnas IDENTITY (KAN-04)
        ORDER BY c.table_name
    LOOP
        seq_name := t.table_name || '_id_seq';

        EXECUTE format(
            'SELECT COALESCE(MAX(id), 0) FROM public.%I',
            t.table_name
        ) INTO max_val;

        EXECUTE format(
            'CREATE SEQUENCE IF NOT EXISTS %I START %s',
            seq_name,
            max_val + 1
        );

        EXECUTE format(
            'ALTER TABLE public.%I ALTER COLUMN id SET DEFAULT nextval(''%s'')',
            t.table_name,
            seq_name
        );

        EXECUTE format(
            'ALTER SEQUENCE %I OWNED BY public.%I.id',
            seq_name,
            t.table_name
        );

        RAISE NOTICE 'Sequence creada → tabla: % (inicio en %)', t.table_name, max_val + 1;
    END LOOP;
END$$;

-- Registrar este hotfix en el historial de migraciones
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260406000001_KAN03_Fix_IdSequences', '8.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;

-- Verificación final
-- Columnas con sequence (recién creadas):
SELECT table_name, column_default
FROM information_schema.columns
WHERE table_schema   = 'public'
  AND column_name    = 'id'
  AND column_default LIKE 'nextval%'
ORDER BY table_name;

-- Columnas IDENTITY (ya estaban bien, KAN-04):
SELECT table_name, identity_generation
FROM information_schema.columns
WHERE table_schema     = 'public'
  AND column_name      = 'id'
  AND is_identity      = 'YES'
ORDER BY table_name;
